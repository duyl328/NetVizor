
// services/WebSocketManager.ts
import { EventEmitter } from 'events'
import type {
  WebSocketMessage,
  WebSocketConfig,
  ConnectionState,
  MessageType
} from '@/types/websocket'

export class WebSocketManager extends EventEmitter {
  private ws: WebSocket | null = null
  private config: WebSocketConfig
  private reconnectTimer: NodeJS.Timeout | null = null
  private heartbeatTimer: NodeJS.Timeout | null = null
  /**
   * 重新连接尝试
   * @private
   */
  private reconnectAttempts = 0
  private state: ConnectionState = ConnectionState.DISCONNECTED
  /**
   * 待处理请求
   * @private
   */
  private pendingRequests = new Map<string, {
    resolve: (value: unknown) => void
    reject: (reason: unknown) => void
    timeout: NodeJS.Timeout
  }>()

  constructor(config: WebSocketConfig) {
    super()
    this.config = config
  }

  public connect(): Promise<void> {
    return new Promise((resolve, reject) => {
      if (this.state === ConnectionState.CONNECTED) {
        resolve()
        return
      }

      this.setState(ConnectionState.CONNECTING)

      try {
        this.ws = new WebSocket(this.config.url)
        this.setupEventListeners(resolve, reject)
      } catch (error) {
        this.setState(ConnectionState.ERROR)
        reject(error)
      }
    })
  }

  public disconnect(): void {
    this.clearTimers()
    this.setState(ConnectionState.DISCONNECTED)

    if (this.ws) {
      this.ws.close()
      this.ws = null
    }

    this.rejectPendingRequests('Connection closed')
  }

  public send<T>(type: MessageType, data?: T): void {
    if (!this.isConnected()) {
      throw new Error('WebSocket is not connected')
    }

    const message: WebSocketMessage<T> = {
      type,
      data,
      timestamp: Date.now()
    }

    this.ws!.send(JSON.stringify(message))
  }

  public sendRequest<T, R>(type: MessageType, data?: T): Promise<R> {
    return new Promise((resolve, reject) => {
      if (!this.isConnected()) {
        reject(new Error('WebSocket is not connected'))
        return
      }

      const requestId = this.generateRequestId()
      const message: WebSocketMessage<T> = {
        type,
        data,
        timestamp: Date.now(),
        requestId
      }

      const timeout = setTimeout(() => {
        this.pendingRequests.delete(requestId)
        reject(new Error('Request timeout'))
      }, this.config.timeout)

      this.pendingRequests.set(requestId, { resolve, reject, timeout })
      this.ws!.send(JSON.stringify(message))
    })
  }

  public getState(): ConnectionState {
    return this.state
  }

  public isConnected(): boolean {
    return this.state === ConnectionState.CONNECTED && this.ws?.readyState === WebSocket.OPEN
  }

  private setupEventListeners(
    connectResolve: () => void,
    connectReject: (error: unknown) => void
  ): void {
    if (!this.ws) return

    this.ws.onopen = () => {
      this.setState(ConnectionState.CONNECTED)
      this.reconnectAttempts = 0
      this.startHeartbeat()
      connectResolve()
      this.emit('connected')
    }

    this.ws.onclose = (event) => {
      this.setState(ConnectionState.DISCONNECTED)
      this.stopHeartbeat()
      this.emit('disconnected', event)

      if (!event.wasClean && this.reconnectAttempts < this.config.maxReconnectAttempts) {
        this.scheduleReconnect()
      }
    }

    this.ws.onerror = (error) => {
      this.setState(ConnectionState.ERROR)
      this.emit('error', error)
      connectReject(error)
    }

    this.ws.onmessage = (event) => {
      try {
        const message: WebSocketMessage = JSON.parse(event.data)
        this.handleMessage(message)
      } catch (error) {
        console.error('Failed to parse WebSocket message:', error)
      }
    }
  }

  private handleMessage(message: WebSocketMessage): void {
    // 处理响应消息
    if (message.requestId && this.pendingRequests.has(message.requestId)) {
      const request = this.pendingRequests.get(message.requestId)!
      clearTimeout(request.timeout)
      this.pendingRequests.delete(message.requestId)

      if (message.type === MessageType.ERROR) {
        request.reject(new Error(message.data))
      } else {
        request.resolve(message.data)
      }
      return
    }

    // 触发消息事件
    this.emit('message', message)
    this.emit(message.type, message.data)
  }

  private scheduleReconnect(): void {
    if (this.reconnectTimer) return

    this.setState(ConnectionState.RECONNECTING)
    this.reconnectAttempts++

    const delay = Math.min(1000 * Math.pow(2, this.reconnectAttempts - 1), 30000)

    this.reconnectTimer = setTimeout(() => {
      this.reconnectTimer = null
      this.connect().catch(() => {
        // 重连失败，会自动触发下一次重连
      })
    }, delay)
  }

  private startHeartbeat(): void {
    this.heartbeatTimer = setInterval(() => {
      if (this.isConnected()) {
        this.send(MessageType.GET_SYSTEM_INFO)
      }
    }, this.config.heartbeatInterval)
  }

  private stopHeartbeat(): void {
    if (this.heartbeatTimer) {
      clearInterval(this.heartbeatTimer)
      this.heartbeatTimer = null
    }
  }

  private clearTimers(): void {
    if (this.reconnectTimer) {
      clearTimeout(this.reconnectTimer)
      this.reconnectTimer = null
    }
    this.stopHeartbeat()
  }

  private setState(state: ConnectionState): void {
    if (this.state !== state) {
      this.state = state
      this.emit('stateChange', state)
    }
  }

  private generateRequestId(): string {
    return `req_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`
  }

  private rejectPendingRequests(reason: string): void {
    this.pendingRequests.forEach(({ reject, timeout }) => {
      clearTimeout(timeout)
      reject(new Error(reason))
    })
    this.pendingRequests.clear()
  }
}
