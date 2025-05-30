import { CommandHandler, SubscriptionRequest, WebSocketMessage, WebSocketResponse } from './types'
import CSharpBridgeV2 from '@/correspond/CSharpBridgeV2'
import { ref } from 'vue'
import { WebSocketState } from '@/types/websocket'

export class WebSocketManager {
  private socket: WebSocket | null = null
  private handlers: Map<string, CommandHandler[]> = new Map()
  private subscriptions: Map<string, { interval: number; timer?: NodeJS.Timeout }> = new Map()
  private messageQueue: WebSocketMessage[] = []
  private state = WebSocketState.DISCONNECTED
  private isReconnecting = false;
  private heartbeatTimer: number | null = null;

  public isInitialized: boolean = false
  private heartbeatInterval = 30000
  // 可配置参数
  maxDelay = 3000 // 最大重连间隔 30s
  baseDelay = 500 // 初始间隔 1s
  reconnectAttempts = 0
  isManuallyClosed = false
  // 定时检测
  timedDetection = 0

  // 获取状态的响应式引用
  get connectionState() {
    return this.state
  }
  get reconnecting() {
    return this.isReconnecting;
  }
  // 初始化WebSocket连接
  public initialize(url: string) {
    console.log('initialize', url)
    if (url.trim() === '') return
    if (this.socket) return
    this.state = WebSocketState.CONNECTING
    this.socket = new WebSocket(url)
    this.isInitialized = true

    this.registerHandler('welcome', (data: WebSocketResponse) => {
      if (data.success) console.log('WebSocket 连接建立成功 !! ')
      else console.error('收到WbeSocket 欢迎信息，但是解析失败!!1')
    })

    this.socket.onopen = () => {
      this.state = WebSocketState.CONNECTED
      this.isReconnecting = true
      this.reconnectAttempts = 0
      console.log('WebSocket connected')
      // 发送队列中的消息
      this.flushMessageQueue()
      // 重新激活订阅
      this.reactivateSubscriptions()
      // 启动心跳
      this.startHeartbeat();
    }

    this.socket.onmessage = (event) => {
      try {
        const message: WebSocketMessage = JSON.parse(event.data)
        this.handleMessage(message)
      } catch (error) {
        console.error('Error parsing WebSocket message:', error)
      }
    }

    this.socket.onclose = () => {
      this.socket = null
      this.state = WebSocketState.DISCONNECTED
      this.isReconnecting = false
      console.log('WebSocket disconnected')
      // 尝试重连
      this.retryConnect()
    }

    this.socket.onerror = (error) => {
      console.error('WebSocket error:', error)
      this.state = WebSocketState.ERROR
    }
  }

  retryConnect() {
    this.reconnectAttempts++
    const delay = Math.min(this.baseDelay * 2 ** (this.reconnectAttempts - 1), this.maxDelay)

    console.log(`[WebSocket] Attempting reconnect in ${delay / 1000}s`)
    const bridge = CSharpBridgeV2?.getBridge()
    // 监听获取 WebSocket 链接
    const isConnected1 = this.isReconnecting
    console.log(isConnected1)
    if (isConnected1) return
    setTimeout(() => {
      if (!this.isManuallyClosed) {
        bridge.send('GetWebSocketPath', {}, (data) => {
          this.initialize(data)
          console.log(data, '======')
          this.retryConnect()
        })
      }
    }, delay)
  }

  // 心跳机制
  private startHeartbeat(): void {
    this.heartbeatTimer = setInterval(() => {
      if (this.ws?.readyState === WebSocket.OPEN) {
        this.send({ command: 'ping', data: null });
      }
    }, this.heartbeatInterval);
  }

  // 注册命令处理器
  public registerHandler(command: string, handler: CommandHandler) {
    if (!this.handlers.has(command)) {
      this.handlers.set(command, [])
    }
    this.handlers.get(command)?.push(handler)
  }

  // 注销命令处理器
  public unregisterHandler(command: string, handler: CommandHandler) {
    const handlers = this.handlers.get(command)
    if (handlers) {
      const index = handlers.indexOf(handler)
      if (index !== -1) {
        handlers.splice(index, 1)
      }
    }
  }

  // 发送消息
  public send<T>(command: string, data?: T) {
    const message: WebSocketMessage<T> = {
      command,
      data,
      timestamp: new Date().toISOString(),
    }

    // 如果未连接或连接中，将消息加入队列
    if (!this.isReconnecting || !this.socket) {
      this.messageQueue.push(message)
      return
    }

    try {
      this.socket.send(JSON.stringify(message))
    } catch (error) {
      console.error('Error sending WebSocket message:', error)
      this.messageQueue.push(message)
    }
  }

  // 订阅数据
  public subscribe(request: SubscriptionRequest) {
    this.send('SUBSCRIBE', request)

    // 存储订阅信息
    this.subscriptions.set(request.type, {
      interval: request.interval || 1000, // 默认1秒
    })
  }

  // 取消订阅
  public unsubscribe(type: string) {
    this.send('UNSUBSCRIBE', { type })
    this.subscriptions.delete(type)
  }

  // 重新激活所有订阅
  private reactivateSubscriptions() {
    this.subscriptions.forEach((_, type) => {
      this.subscribe({ type })
    })
  }

  // 处理接收到的消息
  private handleMessage(message: WebSocketResponse) {
    const handlers = this.handlers.get(message.type)
    if (handlers) {
      handlers.forEach((handler) => {
        try {
          handler(message)
        } catch (error) {
          console.error(`Error handling command '${message.type}':`, error)
        }
      })
    }
  }

  // 发送队列中的消息
  private flushMessageQueue() {
    while (this.messageQueue.length > 0 && this.socket?.readyState === WebSocket.OPEN) {
      const message = this.messageQueue.shift()
      if (message) {
        this.send(message.command, message.data)
      }
    }
  }

  // 关闭连接
  public disconnect() {
    if (this.socket) {
      this.socket.close()
      this.socket = null
    }
    this.isReconnecting = false
    this.state = WebSocketState.DISCONNECTED

    this.isInitialized.value = false
  }
}
