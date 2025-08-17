import { defineStore } from 'pinia'
import { WebSocketManager } from '@/utils/websocket'
import { ref, computed, shallowRef } from 'vue'
import { WebSocketState } from '@/types/websocket'
import { environmentDetector, shouldUseMockData } from '@/utils/environmentDetector'
import { dataSourceAdapter } from '@/utils/dataSourceAdapter'

export const useWebSocketStore = defineStore('websocket', () => {
  const manager = shallowRef<WebSocketManager | null>(null)

  // 演示模式相关状态
  const isDemoMode = ref<boolean>(shouldUseMockData())
  const mockSubscriptions = ref<Map<string, string>>(new Map())

  // 计算属性，自动响应WebSocketManager内部状态变化
  const isInitialized = ref<boolean>(false)
  const isOpen = ref<boolean>(false)
  const isConnected = computed(() => {
    if (isDemoMode.value) {
      return true // 演示模式下总是显示已连接
    }
    return manager.value?.isConnected.value ?? false
  })
  const isReconnecting = computed(() => {
    if (isDemoMode.value) {
      return false // 演示模式下不显示重连状态
    }
    return manager.value?.isReconnecting.value ?? false
  })
  const connectionState = computed(() => {
    if (isDemoMode.value) {
      return WebSocketState.CONNECTED
    }
    return manager.value?.state.value ?? WebSocketState.DISCONNECTED
  })

  // 初始化WebSocket
  const initialize = (url: string) => {
    console.log('[WebSocketStore] 初始化WebSocket连接')
    console.log('[WebSocketStore] 环境信息:', environmentDetector.getEnvironmentInfo())

    if (isDemoMode.value) {
      console.log('[WebSocketStore] 演示模式：跳过真实WebSocket连接')
      isInitialized.value = true
      isOpen.value = true
      return
    }

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
    if (isDemoMode.value) {
      console.log(`[WebSocketStore] 演示模式：模拟发送消息 ${command}`, data)
      // 在演示模式下，可以模拟一些响应
      return
    }

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
    if (isDemoMode.value) {
      console.log(`[WebSocketStore] 演示模式：注册消息处理器 ${command}`)

      // 对于实时数据订阅，启动模拟数据推送
      if (command.includes('subscribe') || command.includes('realtime')) {
        const subscriptionId = dataSourceAdapter.subscribeRealtimeData(handler, 2000)
        mockSubscriptions.value.set(command, subscriptionId)
        console.log(`[WebSocketStore] 启动模拟数据推送: ${command} -> ${subscriptionId}`)
      }
      return
    }

    if (!manager.value) {
      console.warn('WebSocket未初始化，处理器将在初始化后注册')
      return
    }
    manager.value.registerHandler(command, handler)
  }

  // 注销处理器
  const unregisterHandler = (command: string, handler: (data: unknown) => void) => {
    if (isDemoMode.value) {
      console.log(`[WebSocketStore] 演示模式：注销消息处理器 ${command}`)

      // 停止模拟数据推送
      const subscriptionId = mockSubscriptions.value.get(command)
      if (subscriptionId) {
        dataSourceAdapter.unsubscribeRealtimeData(subscriptionId)
        mockSubscriptions.value.delete(command)
        console.log(`[WebSocketStore] 停止模拟数据推送: ${command}`)
      }
      return
    }

    if (!manager.value) {
      return
    }
    manager.value.unregisterHandler(command, handler)
  }

  // 断开连接
  const disconnect = () => {
    if (isDemoMode.value) {
      console.log('[WebSocketStore] 演示模式：断开连接（清理模拟订阅）')

      // 清理所有模拟订阅
      mockSubscriptions.value.forEach((subscriptionId, command) => {
        dataSourceAdapter.unsubscribeRealtimeData(subscriptionId)
        console.log(`[WebSocketStore] 清理模拟订阅: ${command}`)
      })
      mockSubscriptions.value.clear()

      isInitialized.value = false
      isOpen.value = false
      return
    }

    if (manager.value) {
      manager.value.disconnect()
      manager.value = null
      isInitialized.value = false
      isOpen.value = false
    }
  }

  // 新增方法：获取演示模式状态和信息
  const getDemoModeInfo = () => {
    return {
      isDemoMode: isDemoMode.value,
      environmentInfo: environmentDetector.getEnvironmentInfo(),
      dataSourceInfo: dataSourceAdapter.getDataSourceInfo(),
      activeSubscriptions: mockSubscriptions.value.size,
      subscriptions: Array.from(mockSubscriptions.value.keys())
    }
  }

  // 添加便捷方法用于数据订阅
  const subscribe = (topic: string, callback: (data: unknown) => void) => {
    const command = `subscribe_${topic}`
    registerHandler(command, callback)
    return command
  }

  const unsubscribe = (command: string, callback: (data: unknown) => void) => {
    unregisterHandler(command, callback)
  }

  return {
    // 响应式状态
    manager,
    isInitialized,
    isOpen,
    isConnected,
    isReconnecting,
    connectionState,
    isDemoMode,

    // 方法
    initialize,
    send,
    registerHandler,
    unregisterHandler,
    disconnect,

    // 新增方法
    getDemoModeInfo,
    subscribe,
    unsubscribe,
  }
})
