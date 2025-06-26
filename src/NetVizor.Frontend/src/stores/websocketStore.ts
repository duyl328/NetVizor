import { defineStore } from 'pinia'
import { WebSocketManager } from '@/utils/websocket'
import { ref, computed,shallowRef } from 'vue'
import { WebSocketState } from '@/types/websocket'

export const useWebSocketStore = defineStore('websocket', () => {
  const manager = shallowRef<WebSocketManager | null>(null)

  // 计算属性，自动响应WebSocketManager内部状态变化
  const isInitialized = ref<boolean>(false)
  const isOpen = ref<boolean>(false)
  const isConnected = computed(() => manager.value?.isConnected.value ?? false)
  const isReconnecting = computed(() => manager.value?.isReconnecting.value ?? false)
  const connectionState = computed(() => manager.value?.state.value ?? WebSocketState.DISCONNECTED)

  // 初始化WebSocket
  const initialize = (url: string) => {
    if (url.trim() === '') {
      console.warn('WebSocket URL is empty')
      return
    }

    if (!manager.value) {
      manager.value = new WebSocketManager()
      console.log(manager.value.state,'this is store');
    }

    manager.value.initialize(url)

    isInitialized.value = manager.value.isInit
    isOpen.value = manager.value.isOpen
  }

  // 发送消息
  const send = <T>(command: string, data?: T) => {
    if (!manager.value) {
      throw new Error('WebSocket未初始化')
    }
    if (!isConnected.value) {
      console.warn('WebSocket连接未建立，消息将被加入队列')
    }
    manager.value.send(command, data)
  }

  // 注册处理器
  const registerHandler = (command: string, handler: (data: unknown) => void) => {
    if (!manager.value) {
      console.warn('WebSocket未初始化，处理器将在初始化后注册')
      return
    }
    manager.value.registerHandler(command, handler)
  }

  // 注销处理器
  const unregisterHandler = (command: string, handler: (data: unknown) => void) => {
    if (!manager.value) {
      return
    }
    manager.value.unregisterHandler(command, handler)
  }

  // 断开连接
  const disconnect = () => {
    if (manager.value) {
      manager.value.disconnect()
      manager.value = null
      isInitialized.value = false
      isOpen.value = false
    }
  }

  return {
    // 响应式状态
    manager,
    isInitialized,
    isOpen,
    isConnected,
    isReconnecting,
    connectionState,

    // 方法
    initialize,
    send,
    registerHandler,
    unregisterHandler,
    disconnect,
  }
})
