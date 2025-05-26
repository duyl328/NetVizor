
// composables/useWebSocket.ts
import { onMounted, onUnmounted, watch } from 'vue'
import { useWebSocketStore } from '@/stores/websocket'
import type { ConnectionState } from '@/types/websocket'

export interface UseWebSocketOptions {
  url: string
  autoConnect?: boolean
  autoRefresh?: boolean
  refreshInterval?: number
  onConnected?: () => void
  onDisconnected?: () => void
  onError?: (error: string) => void
  onStateChange?: (state: ConnectionState) => void
}

export const useWebSocket = (options: UseWebSocketOptions) => {
  const store = useWebSocketStore()
  let refreshTimer: NodeJS.Timeout | null = null

  const {
    url,
    autoConnect = true,
    autoRefresh = true,
    refreshInterval = 5000,
    onConnected,
    onDisconnected,
    onError,
    onStateChange
  } = options

  // 启动自动刷新
  const startAutoRefresh = (): void => {
    if (!autoRefresh || refreshTimer) return

    refreshTimer = setInterval(async () => {
      if (store.isConnected) {
        try {
          await store.fetchSystemInfo()
        } catch (error) {
          console.error('Auto refresh failed:', error)
        }
      }
    }, refreshInterval)
  }

  // 停止自动刷新
  const stopAutoRefresh = (): void => {
    if (refreshTimer) {
      clearInterval(refreshTimer)
      refreshTimer = null
    }
  }

  // 手动连接
  const connect = async (): Promise<void> => {
    try {
      await store.initialize(url)
      if (autoRefresh) {
        startAutoRefresh()
      }
    } catch (error) {
      console.error('Connection failed:', error)
      throw error
    }
  }

  // 手动断开连接
  const disconnect = (): void => {
    stopAutoRefresh()
    store.disconnect()
  }

  // 监听连接状态变化
  watch(
    () => store.connectionState,
    (newState, oldState) => {
      onStateChange?.(newState)

      if (newState === 'connected' && oldState !== 'connected') {
        onConnected?.()
        if (autoRefresh) {
          startAutoRefresh()
        }
      } else if (newState === 'disconnected' && oldState === 'connected') {
        stopAutoRefresh()
        onDisconnected?.()
      }
    },
    { immediate: true }
  )

  // 监听错误
  watch(
    () => store.lastError,
    (error) => {
      if (error) {
        onError?.(error)
      }
    }
  )

  onMounted(() => {
    if (autoConnect) {
      connect().catch(console.error)
    }
  })

  onUnmounted(() => {
    disconnect()
  })

  return {
    // 状态
    connectionState: store.connectionState,
    isConnected: store.isConnected,
    systemInfo: store.systemInfo,
    networkStatus: store.networkStatus,
    firewallRules: store.firewallRules,
    lastError: store.lastError,
    isLoading: store.isLoading,
    connectionCount: store.connectionCount,
    activeNetworkInterfaces: store.activeNetworkInterfaces,

    // 方法
    connect,
    disconnect,
    fetchSystemInfo: store.fetchSystemInfo,
    fetchNetworkStatus: store.fetchNetworkStatus,
    fetchFirewallRules: store.fetchFirewallRules,
    addFirewallRule: store.addFirewallRule,
    updateFirewallRule: store.updateFirewallRule,
    deleteFirewallRule: store.deleteFirewallRule,
    toggleFirewallRule: store.toggleFirewallRule,
    clearError: store.clearError,
    startAutoRefresh,
    stopAutoRefresh
  }
}
