/**
 * Time:2025/5/25 20:42 29
 * Name:useWebSocketEnhanced.ts
 * Path:src/composables
 * ProjectName:ClientApp
 * Author:charlatans
 *
 *  Il n'ya qu'un héroïsme au monde :
 *     c'est de voir le monde tel qu'il est et de l'aimer.
 */

// composables/useWebSocketEnhanced.ts - 增强的WebSocket管理
import { ref, onUnmounted, watch } from 'vue';
import { useWebSocketStore } from '@/stores/websocketStore';
import { WebSocketRetryManager } from '@/utils/websocketHelpers';
import type { NetworkStatus, ConnectionInfo, FirewallRule, WebSocketMessage, WebSocketCommand } from '@/types/websocket';

export class EnhancedWebSocketManager {
  private ws: WebSocket | null = null;
  private retryManager = new WebSocketRetryManager();
  private pingTimer: number | null = null;
  private dataRefreshTimer: number | null = null;
  private messageHandlers = new Map<string, Function[]>();
  private pendingCommands = new Map<string, { resolve: Function; reject: Function; timeout: number }>();
  private store = useWebSocketStore();

  constructor(private url: string = 'ws://localhost:8080') {
    this.setupEventHandlers();
  }

  async connect(): Promise<void> {
    if (this.ws?.readyState === WebSocket.OPEN) return;

    this.store.updateConnectionState(false, true);

    return new Promise((resolve, reject) => {
      try {
        this.ws = new WebSocket(this.url);

        this.ws.onopen = () => {
          console.log('WebSocket connected successfully');
          this.store.updateConnectionState(true, false, 'excellent');
          this.retryManager.reset();
          this.startPing();
          this.startDataRefresh();
          resolve();
        };

        this.ws.onmessage = (event) => {
          this.handleMessage(event.data);
        };

        this.ws.onclose = (event) => {
          console.log('WebSocket closed:', event.code, event.reason);
          this.store.updateConnectionState(false, false);
          this.stopPing();
          this.stopDataRefresh();

          if (event.code !== 1000 && this.retryManager.shouldRetry()) {
            this.scheduleReconnect();
          }
        };

        this.ws.onerror = (error) => {
          console.error('WebSocket error:', error);
          this.store.setError('WebSocket连接错误');
          this.store.updateConnectionState(false, false);
          reject(error);
        };

      } catch (error) {
        this.store.updateConnectionState(false, false);
        this.store.setError('无法创建WebSocket连接');
        reject(error);
      }
    });
  }

  disconnect() {
    this.stopPing();
    this.stopDataRefresh();
    this.retryManager.reset();

    // 清理待处理的命令
    this.pendingCommands.forEach(({ reject, timeout }) => {
      clearTimeout(timeout);
      reject(new Error('Connection closed'));
    });
    this.pendingCommands.clear();

    if (this.ws) {
      this.ws.close(1000, 'User initiated disconnect');
      this.ws = null;
    }

    this.store.updateConnectionState(false, false, 'disconnected');
  }

  async sendCommand<T = unknown>(action: WebSocketCommand['action'], data?: unknown): Promise<T> {
    if (!this.ws || this.ws.readyState !== WebSocket.OPEN) {
      throw new Error('WebSocket not connected');
    }

    const commandId = this.generateId();
    const command: WebSocketCommand = { action, data, id: commandId };
    const message: WebSocketMessage = {
      type: 'command',
      data: command,
      timestamp: Date.now(),
      id: commandId
    };

    return new Promise((resolve, reject) => {
      // 设置超时
      const timeout = setTimeout(() => {
        this.pendingCommands.delete(commandId);
        reject(new Error(`Command timeout: ${action}`));
      }, 15000);

      // 存储待处理命令
      this.pendingCommands.set(commandId, { resolve, reject, timeout });

      // 发送命令
      try {
        this.ws!.send(JSON.stringify(message));
      } catch (error) {
        this.pendingCommands.delete(commandId);
        clearTimeout(timeout);
        reject(error);
      }
    });
  }

  private handleMessage(data: string) {
    try {
      const message: WebSocketMessage = JSON.parse(data);

      switch (message.type) {
        case 'network_status':
          this.store.updateNetworkStatus(message.data);
          this.emit('network_status', message.data);
          break;

        case 'connection_info':
          this.store.updateConnections(message.data);
          this.emit('connection_info', message.data);
          break;

        case 'firewall_rules':
          this.store.updateFirewallRules(message.data);
          this.emit('firewall_rules', message.data);
          break;

        case 'response':
          this.handleCommandResponse(message);
          break;

        case 'error':
          this.store.setError(message.data.message || '服务器错误');
          this.emit('error', message.data);
          break;

        default:
          this.emit('message', message);
      }
    } catch (error) {
      console.error('Failed to parse WebSocket message:', error);
    }
  }

  private handleCommandResponse(message: WebSocketMessage) {
    if (!message.id) return;

    const pending = this.pendingCommands.get(message.id);
    if (pending) {
      clearTimeout(pending.timeout);
      this.pendingCommands.delete(message.id);

      if (message.data.success) {
        pending.resolve(message.data.data);
      } else {
        pending.reject(new Error(message.data.error || 'Command failed'));
      }
    }
  }

  private scheduleReconnect() {
    const delay = this.retryManager.getNextDelay();
    console.log(`Reconnecting in ${delay}ms (attempt ${this.retryManager.getCurrentAttempt()})`);

    setTimeout(() => {
      this.connect().catch(console.error);
    }, delay);
  }

  private startPing() {
    this.pingTimer = setInterval(() => {
      if (this.ws?.readyState === WebSocket.OPEN) {
        this.sendPing().catch(() => {
          this.store.updateConnectionState(true, false, 'poor');
        });
      }
    }, 30000);
  }

  private stopPing() {
    if (this.pingTimer) {
      clearInterval(this.pingTimer);
      this.pingTimer = null;
    }
  }

  private startDataRefresh() {
    // 定期刷新数据
    this.dataRefreshTimer = setInterval(() => {
      this.refreshAllData().catch(console.error);
    }, 5000); // 每5秒刷新一次
  }

  private stopDataRefresh() {
    if (this.dataRefreshTimer) {
      clearInterval(this.dataRefreshTimer);
      this.dataRefreshTimer = null;
    }
  }

  private async sendPing() {
    const message: WebSocketMessage = {
      type: 'command',
      data: { action: 'ping' },
      timestamp: Date.now()
    };

    if (this.ws?.readyState === WebSocket.OPEN) {
      this.ws.send(JSON.stringify(message));
    }
  }

  private async refreshAllData() {
    if (!this.ws || this.ws.readyState !== WebSocket.OPEN) return;

    try {
      await Promise.all([
        this.sendCommand('get_network_status'),
        this.sendCommand('get_connections'),
        this.sendCommand('get_firewall_rules')
      ]);
    } catch (error) {
      console.error('Failed to refresh data:', error);
    }
  }

  private setupEventHandlers() {
    // 监听store状态变化
    watch(() => this.store.isConnected, (connected) => {
      if (connected) {
        this.refreshAllData();
      }
    });
  }

  // 事件系统
  on(event: string, handler: Function) {
    if (!this.messageHandlers.has(event)) {
      this.messageHandlers.set(event, []);
    }
    this.messageHandlers.get(event)!.push(handler);
  }

  off(event: string, handler: Function) {
    const handlers = this.messageHandlers.get(event);
    if (handlers) {
      const index = handlers.indexOf(handler);
      if (index > -1) {
        handlers.splice(index, 1);
      }
    }
  }

  private emit(event: string, data?: unknown) {
    const handlers = this.messageHandlers.get(event);
    if (handlers) {
      handlers.forEach(handler => handler(data));
    }
  }

  private generateId(): string {
    return Date.now().toString(36) + Math.random().toString(36).substr(2);
  }

  // API方法
  async getNetworkStatus(): Promise<NetworkStatus[]> {
    return this.sendCommand('get_network_status');
  }

  async getConnections(): Promise<ConnectionInfo[]> {
    return this.sendCommand('get_connections');
  }

  async getFirewallRules(): Promise<FirewallRule[]> {
    return this.sendCommand('get_firewall_rules');
  }

  async addFirewallRule(rule: Omit<FirewallRule, 'id'>): Promise<void> {
    return this.sendCommand('add_firewall_rule', rule);
  }

  async removeFirewallRule(ruleId: string): Promise<void> {
    return this.sendCommand('remove_firewall_rule', { id: ruleId });
  }

  async toggleFirewallRule(ruleId: string): Promise<void> {
    return this.sendCommand('toggle_firewall_rule', { id: ruleId });
  }
}

// 全局WebSocket管理器实例
let wsManager: EnhancedWebSocketManager | null = null;

export function useWebSocketEnhanced(url?: string) {
  if (!wsManager) {
    wsManager = new EnhancedWebSocketManager(url);
  }

  const store = useWebSocketStore();

  // 自动连接
  if (!store.isConnected && !store.isConnecting) {
    wsManager.connect().catch(console.error);
  }

  onUnmounted(() => {
    // 组件卸载时不断开连接，因为是单例
  });

  return {
    // 从store获取状态
    ...store,

    // WebSocketManager方法
    connect: () => wsManager!.connect(),
    disconnect: () => wsManager!.disconnect(),
    sendCommand: (action: WebSocketCommand['action'], data?: unknown) => wsManager!.sendCommand(action, data),

    // API方法
    getNetworkStatus: () => wsManager!.getNetworkStatus(),
    getConnections: () => wsManager!.getConnections(),
    getFirewallRules: () => wsManager!.getFirewallRules(),
    addFirewallRule: (rule: Omit<FirewallRule, 'id'>) => wsManager!.addFirewallRule(rule),
    removeFirewallRule: (ruleId: string) => wsManager!.removeFirewallRule(ruleId),
    toggleFirewallRule: (ruleId: string) => wsManager!.toggleFirewallRule(ruleId),

    // 事件监听
    on: (event: string, handler: Function) => wsManager!.on(event, handler),
    off: (event: string, handler: Function) => wsManager!.off(event, handler),
  };
}

// plugins/websocket.ts - Vue插件
import type { App } from 'vue';
import { EnhancedWebSocketManager } from '@/composables/useWebSocketEnhanced';

declare module '@vue/runtime-core' {
  interface ComponentCustomProperties {
    $ws: EnhancedWebSocketManager;
  }
}

export default {
  install(app: App, options: { url?: string } = {}) {
    const wsManager = new EnhancedWebSocketManager(options.url);

    app.config.globalProperties.$ws = wsManager;
    app.provide('websocket', wsManager);

    // 应用关闭时断开连接
    window.addEventListener('beforeunload', () => {
      wsManager.disconnect();
    });
  }
};
