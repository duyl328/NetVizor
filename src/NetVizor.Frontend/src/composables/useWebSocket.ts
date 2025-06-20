// composables/useWebSocket.ts
import { ref, reactive, onUnmounted, nextTick } from 'vue';
import type {
  WebSocketMessage,
  WebSocketResponse,
  WebSocketState,
  EventListener,
  SubscriptionCallback,
  RequestOptions,
  WebSocketConfig,
  SubscriptionMessage,
  NetworkStatus,
  DatabaseQueryRequest,
  ConfigUpdateRequest,
  CommandRequest
} from '@/types/websocket';

class WebSocketManager {
  private ws: WebSocket | null = null;
  private url: string = '';
  private config: WebSocketConfig;
  private reconnectAttempts: number = 0;
  private reconnectTimer: number | null = null;
  private heartbeatTimer: number | null = null;

  // 状态管理
  private state = ref<WebSocketState>(WebSocketState.DISCONNECTED);
  private isReconnecting = ref(false);

  // 事件监听器
  private eventListeners = new Map<string, Set<EventListener>>();

  // 订阅管理
  private subscriptions = new Map<string, Set<SubscriptionCallback>>();
  private activeSubscriptions = new Set<string>();

  // 请求-响应管理
  private pendingRequests = new Map<string, {
    resolve: (value: WebSocketResponse) => void;
    reject: (error: Error) => void;
    timeout: number;
  }>();

  // 消息队列（连接断开时暂存消息）
  private messageQueue: WebSocketMessage[] = [];

  constructor(config: WebSocketConfig = {}) {
    this.config = {
      reconnectInterval: 3000,
      maxReconnectAttempts: 5,
      heartbeatInterval: 30000,
      requestTimeout: 10000,
      ...config
    };
  }

  // 获取状态的响应式引用
  get connectionState() {
    return this.state;
  }

  get reconnecting() {
    return this.isReconnecting;
  }

  // 连接WebSocket
  async connect(url: string): Promise<void> {
    if (this.ws?.readyState === WebSocket.OPEN) {
      console.warn('WebSocket already connected');
      return;
    }

    this.url = url;
    this.state.value = WebSocketState.CONNECTING;

    return new Promise((resolve, reject) => {
      try {
        this.ws = new WebSocket(url);

        this.ws.onopen = () => {
          console.log('WebSocket connected');
          this.state.value = WebSocketState.CONNECTED;
          this.reconnectAttempts = 0;
          this.isReconnecting.value = false;

          // 处理队列中的消息
          this.processMessageQueue();

          // 重新订阅之前的主题
          this.resubscribeTopics();

          // 启动心跳
          this.startHeartbeat();

          resolve();
        };

        this.ws.onmessage = (event) => {
          this.handleMessage(event.data);
        };

        this.ws.onclose = (event) => {
          console.log('WebSocket closed', event.code, event.reason);
          this.state.value = WebSocketState.DISCONNECTED;
          this.stopHeartbeat();

          if (!event.wasClean && this.reconnectAttempts < this.config.maxReconnectAttempts!) {
            this.attemptReconnect();
          }
        };

        this.ws.onerror = (error) => {
          console.error('WebSocket error:', error);
          this.state.value = WebSocketState.ERROR;
          reject(new Error('WebSocket connection failed'));
        };
      } catch (error) {
        this.state.value = WebSocketState.ERROR;
        reject(error);
      }
    });
  }

  // 断开连接
  disconnect(): void {
    if (this.reconnectTimer) {
      clearTimeout(this.reconnectTimer);
      this.reconnectTimer = null;
    }

    this.stopHeartbeat();

    if (this.ws) {
      this.ws.close(1000, 'Manual disconnect');
      this.ws = null;
    }

    this.state.value = WebSocketState.DISCONNECTED;
    this.isReconnecting.value = false;
  }

  // 发送消息
  private send(message: WebSocketMessage): void {
    if (this.ws?.readyState === WebSocket.OPEN) {
      this.ws.send(JSON.stringify(message));
    } else {
      // 连接断开时将消息加入队列
      this.messageQueue.push(message);
      console.warn('WebSocket not connected, message queued');
    }
  }

  // 发送请求并等待响应
  async request<TRequest = unknown, TResponse = unknown>(
    command: string,
    data: TRequest,
    options: RequestOptions = {}
  ): Promise<TResponse> {
    const requestId = this.generateRequestId();
    const timeout = options.timeout || this.config.requestTimeout!;

    const message: WebSocketMessage<TRequest> = {
      command,
      data,
      requestId,
      timestamp: Date.now()
    };

    return new Promise((resolve, reject) => {
      // 设置超时
      const timeoutId = setTimeout(() => {
        this.pendingRequests.delete(requestId);
        reject(new Error(`Request timeout: ${command}`));
      }, timeout);

      // 存储请求回调
      this.pendingRequests.set(requestId, {
        resolve: (response: WebSocketResponse<TResponse>) => {
          clearTimeout(timeoutId);
          if (response.success) {
            resolve(response.data);
          } else {
            reject(new Error(response.error || 'Request failed'));
          }
        },
        reject: (error: Error) => {
          clearTimeout(timeoutId);
          reject(error);
        },
        timeout: timeoutId
      });

      // 发送请求
      this.send(message);
    });
  }

  // 订阅主题
  subscribe<T = unknown>(topic: string, callback: SubscriptionCallback<T>): () => void {
    // 添加回调到订阅列表
    if (!this.subscriptions.has(topic)) {
      this.subscriptions.set(topic, new Set());
    }
    this.subscriptions.get(topic)!.add(callback as SubscriptionCallback);

    // 如果是新订阅，发送订阅消息到服务器
    if (!this.activeSubscriptions.has(topic)) {
      this.activeSubscriptions.add(topic);
      const subscribeMessage: WebSocketMessage<SubscriptionMessage> = {
        command: 'subscription',
        data: { type: 'subscribe', topic }
      };
      this.send(subscribeMessage);
    }

    // 返回取消订阅函数
    return () => {
      const callbacks = this.subscriptions.get(topic);
      if (callbacks) {
        callbacks.delete(callback as SubscriptionCallback);
        if (callbacks.size === 0) {
          this.subscriptions.delete(topic);
          this.activeSubscriptions.delete(topic);

          // 发送取消订阅消息
          const unsubscribeMessage: WebSocketMessage<SubscriptionMessage> = {
            command: 'subscription',
            data: { type: 'unsubscribe', topic }
          };
          this.send(unsubscribeMessage);
        }
      }
    };
  }

  // 添加事件监听器
  on<T = unknown>(event: string, listener: EventListener<T>): () => void {
    if (!this.eventListeners.has(event)) {
      this.eventListeners.set(event, new Set());
    }
    this.eventListeners.get(event)!.add(listener as EventListener);

    // 返回移除监听器函数
    return () => {
      const listeners = this.eventListeners.get(event);
      if (listeners) {
        listeners.delete(listener as EventListener);
        if (listeners.size === 0) {
          this.eventListeners.delete(event);
        }
      }
    };
  }

  // 处理接收到的消息
  private handleMessage(data: string): void {
    try {
      const message: WebSocketResponse = JSON.parse(data);

      // 处理请求响应
      if (message.requestId && this.pendingRequests.has(message.requestId)) {
        const request = this.pendingRequests.get(message.requestId)!;
        this.pendingRequests.delete(message.requestId);
        request.resolve(message);
        return;
      }

      // 处理订阅消息
      if (message.command.startsWith('subscription:')) {
        const topic = message.command.replace('subscription:', '');
        const callbacks = this.subscriptions.get(topic);
        if (callbacks) {
          callbacks.forEach(callback => callback(message.data, topic));
        }
        return;
      }

      // 处理普通事件
      const listeners = this.eventListeners.get(message.command);
      if (listeners) {
        listeners.forEach(listener => listener(message.data));
      }
    } catch (error) {
      console.error('Failed to parse WebSocket message:', error);
    }
  }

  // 重连逻辑
  private attemptReconnect(): void {
    if (this.isReconnecting.value) return;

    this.isReconnecting.value = true;
    this.reconnectAttempts++;

    console.log(`Attempting to reconnect... (${this.reconnectAttempts}/${this.config.maxReconnectAttempts})`);

    this.reconnectTimer = setTimeout(async () => {
      try {
        await this.connect(this.url);
      } catch (error) {
        console.error('Reconnection failed:', error);
        if (this.reconnectAttempts < this.config.maxReconnectAttempts!) {
          this.attemptReconnect();
        } else {
          this.isReconnecting.value = false;
          console.error('Max reconnection attempts reached');
        }
      }
    }, this.config.reconnectInterval);
  }

  // 处理消息队列
  private processMessageQueue(): void {
    while (this.messageQueue.length > 0) {
      const message = this.messageQueue.shift()!;
      this.send(message);
    }
  }

  // 重新订阅主题
  private resubscribeTopics(): void {
    this.activeSubscriptions.forEach(topic => {
      const subscribeMessage: WebSocketMessage<SubscriptionMessage> = {
        command: 'subscription',
        data: { type: 'subscribe', topic }
      };
      this.send(subscribeMessage);
    });
  }

  // 心跳机制
  private startHeartbeat(): void {
    this.heartbeatTimer = setInterval(() => {
      if (this.ws?.readyState === WebSocket.OPEN) {
        this.send({ command: 'ping', data: null });
      }
    }, this.config.heartbeatInterval);
  }

  private stopHeartbeat(): void {
    if (this.heartbeatTimer) {
      clearInterval(this.heartbeatTimer);
      this.heartbeatTimer = null;
    }
  }

  // 生成请求ID
  private generateRequestId(): string {
    return `req_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
  }

  // 清理资源
  destroy(): void {
    this.disconnect();
    this.eventListeners.clear();
    this.subscriptions.clear();
    this.activeSubscriptions.clear();
    this.pendingRequests.clear();
    this.messageQueue.length = 0;
  }
}

// 单例模式的WebSocket管理器
let wsManager: WebSocketManager | null = null;

export function useWebSocket(config?: WebSocketConfig) {
  if (!wsManager) {
    wsManager = new WebSocketManager(config);
  }

  // 业务方法封装
  const api = {
    // 数据库查询
    async queryDatabase<T = unknown>(request: DatabaseQueryRequest): Promise<T> {
      return wsManager!.request<DatabaseQueryRequest, T>('database:query', request);
    },

    // 配置更新
    async updateConfig(request: ConfigUpdateRequest): Promise<void> {
      return wsManager!.request<ConfigUpdateRequest, void>('config:update', request);
    },

    // 执行命令
    async executeCommand<T = unknown>(request: CommandRequest): Promise<T> {
      return wsManager!.request<CommandRequest, T>('command:execute', request);
    },

    // 订阅网络状态
    subscribeNetworkStatus(callback: SubscriptionCallback<NetworkStatus>): () => void {
      return wsManager!.subscribe<NetworkStatus>('network:status', callback);
    },

    // 订阅连接信息
    subscribeConnectionInfo(callback: SubscriptionCallback): () => void {
      return wsManager!.subscribe('network:connections', callback);
    },

    // ============================ 真实业务 ===============================
    subscribeNetInfo(callback: SubscriptionCallback): () => void {
      return wsManager!.subscribe('network:NetInfo', callback);
    }
  };

  return {
    // 管理器实例
    manager: wsManager,
    // 状态
    state: wsManager.connectionState,
    reconnecting: wsManager.reconnecting,
    // 核心方法
    connect: wsManager.connect.bind(wsManager),
    disconnect: wsManager.disconnect.bind(wsManager),
    request: wsManager.request.bind(wsManager),
    subscribe: wsManager.subscribe.bind(wsManager),
    on: wsManager.on.bind(wsManager),
    // 业务API
    ...api
  };
}

// 在组件中使用的Composable
export function useWebSocketInComponent(config?: WebSocketConfig) {
  const ws = useWebSocket(config);

  // 组件卸载时清理
  onUnmounted(() => {
    // 注意：这里不要调用destroy，因为是单例模式
    // 其他组件可能还在使用
  });

  return ws;
}
