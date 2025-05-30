import { defineStore } from 'pinia'
import { WebSocketManager } from '@/utils/websocket'
import { ref } from 'vue'

export const useWebSocketStore = defineStore('websocket', () => {
  const manager = ref<WebSocketManager | null>(null)
  const isInitialized = ref(false)

  // 初始化WebSocket
  const initialize = (url: string) => {
    if (url.trim() === '') return
    if (!manager.value) {
      manager.value = new WebSocketManager()
      manager.value.initialize(url)
      // 同步状态
      isInitialized.value = manager.value.isInitialized
    }
  }

  const getIsConnected = (): boolean => {
    return manager.value?.isReconnecting
  }

  // 发送消息
  const send = <T>(command: string, data?: T) => {
    if (!getIsConnected()) throw new Error('连接未建立')
    manager.value?.send(command, data)
  }

  // 订阅数据
  const subscribe = (type: string, interval?: number) => {
    manager.value?.subscribe({ type, interval })
  }

  // 取消订阅
  const unsubscribe = (type: string) => {
    manager.value?.unsubscribe(type)
  }

  // 注册处理器
  const registerHandler = (command: string, handler: (data: unknown) => void) => {
    manager.value?.registerHandler(command, handler)
  }

  // 注销处理器
  const unregisterHandler = (command: string, handler: (data: unknown) => void) => {
    manager.value?.unregisterHandler(command, handler)
  }

  // 断开连接
  const disconnect = () => {
    manager.value?.disconnect()
  }

  return {
    manager,
    getIsConnected,
    isInitialized,
    initialize,
    send,
    subscribe,
    unsubscribe,
    registerHandler,
    unregisterHandler,
    disconnect,
  }
})
