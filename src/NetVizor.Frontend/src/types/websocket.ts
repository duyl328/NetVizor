// types/websocket.ts

// 基础消息格式
export interface WebSocketMessage<T = unknown> {
  command: string
  data?: T
  timestamp?: string
}

// 响应消息格式
export interface WebSocketResponse<T = unknown> {
  type: string
  data?: T
  success?: boolean
  error?: string
  timestamp?: string
}

// 订阅相关
export interface SubscriptionMessage {
  type: 'subscribe' | 'unsubscribe';
  topic: string;
}

// 网络状态数据结构示例
export interface NetworkStatus {
  connectionCount: number;
  bandwidth: number;
  latency: number;
  timestamp: number;
}

// 数据库查询请求
export interface DatabaseQueryRequest {
  table: string;
  query: Record<string, unknown>;
  limit?: number;
}

// 配置更新请求
export interface ConfigUpdateRequest {
  section: string;
  config: Record<string, unknown>;
}

// 命令执行请求
export interface CommandRequest {
  functionName: string;
  parameters: Record<string, unknown>;
}

// WebSocket连接状态
export enum WebSocketState {
  DISCONNECTED = 'disconnected',
  CONNECTING = 'connecting',
  CONNECTED = 'connected',
  ERROR = 'error'
}

// 事件监听器类型
export type EventListener<T = unknown> = (data: T) => void;

// 订阅回调类型
export type SubscriptionCallback<T = unknown> = (data: T, topic: string) => void;

// 请求选项
export interface RequestOptions {
  timeout?: number;
  retries?: number;
}

// WebSocket管理器配置
export interface WebSocketConfig {
  reconnectInterval?: number;
  maxReconnectAttempts?: number;
  heartbeatInterval?: number;
  requestTimeout?: number;
}

// 订阅请求格式
export interface SubscriptionRequest {
  type: string
  interval?: number
  params?: Record<string, unknown>
}
// 命令处理器类型
export type CommandHandler = (data: WebSocketResponse) => void

// WebSocket 配置选项
export interface WebSocketOptions {
  url: string
  heartbeatInterval?: number
  maxReconnectDelay?: number
  baseReconnectDelay?: number
  autoReconnect?: boolean
}
// 命令处理函数类型
export type CommandHandler = (data: unknown) => void | Promise<void>;
