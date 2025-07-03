import {
  CommandHandler,
  SubscriptionRequest,
  WebSocketMessage,
  WebSocketResponse,
  WebSocketState,
} from '@/types/websocket'
import CSharpBridgeV2 from '@/correspond/CSharpBridgeV2'
import { ref, Ref } from 'vue'
import { logB } from '@/utils/logHelper/logUtils'
import { useUuidStore } from '@/stores/uuidStore'
import pako from "pako";

export class WebSocketManager {
  private socket: WebSocket | null = null
  private handlers: Map<string, CommandHandler[]> = new Map()
  private subscriptions: Map<string, { interval: number; timer?: NodeJS.Timeout }> = new Map()
  private messageQueue: WebSocketMessage[] = []

  // 响应式状态 - 修复：使用 shallowRef 或强制创建正确的 ref
  public state: Ref<WebSocketState>
  public isReconnecting: Ref<boolean>
  public isConnected: Ref<boolean>
  public isInit: Ref<boolean>
  public isOpen: Ref<boolean>

  constructor() {
    // 初始化响应式状态
    this.state = ref(WebSocketState.DISCONNECTED)
    this.isReconnecting = ref(false)
    this.isConnected = ref(false)
    this.isInit = ref(false)
    this.isOpen = ref(false)
  }

  private heartbeatTimer: NodeJS.Timeout | null = null
  private reconnectTimer: NodeJS.Timeout | null = null
  private heartbeatInterval = 30000

  // 可配置参数
  maxDelay = 10000 // 最大重连间隔 30s
  baseDelay = 500 // 初始间隔 0.5s
  reconnectAttempts = 0
  isManuallyClosed = false

  private url: string = ''

  // 初始化WebSocket连接
  public initialize(url: string) {
    logB.info('进入 WebSocket 初始化，url:', url)
    if (url.trim() === '') return
    if (this.socket?.readyState === WebSocket.OPEN) {
      logB.warning('WebSocket 已经连接，跳过初始化')
      return
    }

    this.url = url
    this.state.value = WebSocketState.CONNECTING
    this.isManuallyClosed = false

    const uuidStore = useUuidStore()
    logB.info('准备建立连接, uuid:', uuidStore.uuid)

    const fullUrl = url + '?uuid=' + uuidStore.uuid
    logB.info('拼接链接:', fullUrl)

    try {
      this.socket = new WebSocket(fullUrl)
      this.isInit.value = true
      this.setupSocketHandlers()
    } catch (error) {
      console.error('创建WebSocket失败:', error)
      this.state.value = WebSocketState.ERROR
      this.retryConnect()
    }
  }

  private setupSocketHandlers() {
    if (!this.socket) return

    // 注册欢迎消息处理器
    this.registerHandler('welcome', (data: WebSocketResponse) => {
      if (data.success) logB.success('WebSocket 连接建立成功 !! ')
      else console.error('收到WebSocket 欢迎信息，但是解析失败!!')
    })
    this.registerHandler('pong', (data: WebSocketResponse) => {
      console.log('接收到服务器心跳')
    })

    this.socket.onopen = () => {
      this.state.value = WebSocketState.CONNECTED
      this.isConnected.value = true
      this.isReconnecting.value = false
      this.reconnectAttempts = 0
      logB.success('WebSocket connected')
      this.isOpen.value = true

      // 发送队列中的消息
      this.flushMessageQueue()
      // 重新激活订阅
      this.reactivateSubscriptions()
      // 启动心跳
      this.startHeartbeat()
    }

    this.socket.onmessage = async (event) => {
      try {
        const arrayBuffer: ArrayBuffer = event.data instanceof Blob
          ? await event.data.arrayBuffer()
          : event.data;

        const uint8Array = new Uint8Array(arrayBuffer);
        const decompressed = pako.inflate(uint8Array, { to: "string" });

        const json = JSON.parse(decompressed);
        this.handleMessage(json)
      } catch (error) {
        console.error('Error parsing WebSocket message:', error)
      }
    }

    this.socket.onclose = (event) => {
      this.isOpen.value = false
      this.socket = null
      this.state.value = WebSocketState.DISCONNECTED
      this.isConnected.value = false
      this.stopHeartbeat()

      console.log('WebSocket disconnected', event.code, event.reason)

      // 如果不是主动关闭，尝试重连
      if (!this.isManuallyClosed) {
        this.retryConnect()
      }
    }

    this.socket.onerror = (error) => {
      console.error('WebSocket error:', error)
      this.state.value = WebSocketState.ERROR
    }
  }

  private retryConnect() {
    // 如果已经在重连中，不重复执行
    if (this.isReconnecting.value) {
      logB.info('已经在重连中，跳过')
      return
    }

    this.isReconnecting.value = true
    this.reconnectAttempts++
    const delay = Math.min(this.baseDelay * 2 ** (this.reconnectAttempts - 1), this.maxDelay)

    console.log(
      `[WebSocket] Attempting reconnect in ${delay / 1000}s (attempt ${this.reconnectAttempts})`,
    )

    // 清除之前的重连定时器
    if (this.reconnectTimer) {
      clearTimeout(this.reconnectTimer)
    }

    this.reconnectTimer = setTimeout(() => {
      if (!this.isManuallyClosed) {
        const bridge = CSharpBridgeV2?.getBridge()
        if (bridge) {
          bridge.send('GetWebSocketPath', {}, (data: string) => {
            if (data && data.trim()) {
              this.initialize(data)
            } else {
              console.error('获取WebSocket路径失败')
              // 继续重试
              this.retryConnect()
            }
          })
        } else if (this.url) {
          // 如果没有bridge，使用原来的URL重连
          this.initialize(this.url)
        } else {
          console.error('无法获取WebSocket连接地址')
        }
        this.isReconnecting.value = false
      }
    }, delay)
  }

  // 心跳机制
  private startHeartbeat(): void {
    this.stopHeartbeat() // 先清理之前的
    this.heartbeatTimer = setInterval(() => {
      if (this.socket?.readyState === WebSocket.OPEN) {
        this.send('ping', null)
      }
    }, this.heartbeatInterval)
  }

  private stopHeartbeat(): void {
    if (this.heartbeatTimer) {
      clearInterval(this.heartbeatTimer)
      this.heartbeatTimer = null
    }
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

    // 如果未连接，将消息加入队列
    if (!this.isConnected.value || !this.socket || this.socket.readyState !== WebSocket.OPEN) {
      this.messageQueue.push(message as WebSocketMessage)
      logB.warning('WebSocket未连接，消息已加入队列:', command)
      return
    }

    try {
      this.socket.send(JSON.stringify(message))
    } catch (error) {
      console.error('Error sending WebSocket message:', error)
      this.messageQueue.push(message as WebSocketMessage)
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

    // 清除定时器
    const sub = this.subscriptions.get(type)
    if (sub?.timer) {
      clearInterval(sub.timer)
    }
    this.subscriptions.delete(type)
  }

  // 重新激活所有订阅
  private reactivateSubscriptions() {
    this.subscriptions.forEach((sub, type) => {
      this.subscribe({ type, interval: sub.interval })
    })
  }

  // 处理接收到的消息
  private handleMessage(message: WebSocketResponse) {
    const handlers = this.handlers.get(message.type)
    if (handlers && handlers.length > 0) {
      handlers.forEach((handler) => {
        try {
          handler(message)
        } catch (error) {
          console.error(`Error handling command '${message.type}':`, error)
        }
      })
    } else {
      logB.warning(`No handler registered for message type: ${message.type}`)
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
    this.isManuallyClosed = true

    // 清理定时器
    this.stopHeartbeat()
    if (this.reconnectTimer) {
      clearTimeout(this.reconnectTimer)
      this.reconnectTimer = null
    }

    // 关闭socket
    if (this.socket) {
      this.socket.close()
      this.socket = null
    }

    // 重置状态
    this.isReconnecting.value = false
    this.isConnected.value = false
    this.state.value = WebSocketState.DISCONNECTED
    this.isInit.value = false
    this.isOpen.value = false
    this.reconnectAttempts = 0

    // 清空消息队列
    this.messageQueue = []
  }
}
