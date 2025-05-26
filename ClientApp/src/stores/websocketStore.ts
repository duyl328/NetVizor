
// stores/websocket.ts
import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { WebSocketManager } from '@/services/WebSocketManager'
import type {
  SystemInfo,
  NetworkStatus,
  FirewallRule,
  ConnectionState,
  MessageType
} from '@/types/websocket'

export const useWebSocketStore = defineStore('websocket', () => {
  // 状态
  const wsManager = ref<WebSocketManager | null>(null)
  const connectionState = ref<ConnectionState>('disconnected' as ConnectionState)
  const systemInfo = ref<SystemInfo | null>(null)
  const networkStatus = ref<NetworkStatus[]>([])
  const firewallRules = ref<FirewallRule[]>([])
  const lastError = ref<string | null>(null)
  const isLoading = ref(false)

  // 计算属性
  const isConnected = computed(() => connectionState.value === 'connected')
  const connectionCount = computed(() => systemInfo.value?.connectionCount ?? 0)
  const activeNetworkInterfaces = computed(() =>
    networkStatus.value.filter(status => status.isConnected)
  )

  // 初始化WebSocket连接
  const initialize = async (url: string): Promise<void> => {
    if (wsManager.value) {
      wsManager.value.disconnect()
    }

    wsManager.value = new WebSocketManager({
      url,
      reconnectInterval: 5000,
      maxReconnectAttempts: 10,
      heartbeatInterval: 30000,
      timeout: 10000
    })

    setupEventListeners()

    try {
      await wsManager.value.connect()
    } catch (error) {
      lastError.value = error instanceof Error ? error.message : 'Connection failed'
      throw error
    }
  }

  // 设置事件监听器
  const setupEventListeners = (): void => {
    if (!wsManager.value) return

    wsManager.value.on('stateChange', (state: ConnectionState) => {
      connectionState.value = state
    })

    wsManager.value.on('error', (error: Error) => {
      lastError.value = error.message
    })

    // 监听数据更新事件
    wsManager.value.on('network_status_response', (data: NetworkStatus[]) => {
      networkStatus.value = data
    })

    wsManager.value.on('firewall_rules_response', (data: FirewallRule[]) => {
      firewallRules.value = data
    })

    wsManager.value.on('system_info_response', (data: SystemInfo) => {
      systemInfo.value = data
      networkStatus.value = data.networkInterfaces
      firewallRules.value = data.firewallRules
    })

    // 监听实时更新事件
    wsManager.value.on('network_status_changed', (data: NetworkStatus[]) => {
      networkStatus.value = data
    })

    wsManager.value.on('firewall_rule_changed', (data: FirewallRule[]) => {
      firewallRules.value = data
    })

    wsManager.value.on('system_alert', (data: { message: string; level: string }) => {
      console.warn('System Alert:', data)
    })
  }

  // 数据获取方法
  const fetchSystemInfo = async (): Promise<SystemInfo> => {
    if (!wsManager.value || !isConnected.value) {
      throw new Error('WebSocket not connected')
    }

    isLoading.value = true
    try {
      const data = await wsManager.value.sendRequest<void, SystemInfo>(
        'get_system_info' as MessageType
      )
      systemInfo.value = data
      return data
    } finally {
      isLoading.value = false
    }
  }

  const fetchNetworkStatus = async (): Promise<NetworkStatus[]> => {
    if (!wsManager.value || !isConnected.value) {
      throw new Error('WebSocket not connected')
    }

    isLoading.value = true
    try {
      const data = await wsManager.value.sendRequest<void, NetworkStatus[]>(
        'get_network_status' as MessageType
      )
      networkStatus.value = data
      return data
    } finally {
      isLoading.value = false
    }
  }

  const fetchFirewallRules = async (): Promise<FirewallRule[]> => {
    if (!wsManager.value || !isConnected.value) {
      throw new Error('WebSocket not connected')
    }

    isLoading.value = true
    try {
      const data = await wsManager.value.sendRequest<void, FirewallRule[]>(
        'get_firewall_rules' as MessageType
      )
      firewallRules.value = data
      return data
    } finally {
      isLoading.value = false
    }
  }

  // 防火墙操作方法
  const addFirewallRule = async (rule: Omit<FirewallRule, 'id'>): Promise<void> => {
    if (!wsManager.value || !isConnected.value) {
      throw new Error('WebSocket not connected')
    }

    await wsManager.value.sendRequest<Omit<FirewallRule, 'id'>, void>(
      'add_firewall_rule' as MessageType,
      rule
    )
  }

  const updateFirewallRule = async (rule: FirewallRule): Promise<void> => {
    if (!wsManager.value || !isConnected.value) {
      throw new Error('WebSocket not connected')
    }

    await wsManager.value.sendRequest<FirewallRule, void>(
      'update_firewall_rule' as MessageType,
      rule
    )
  }

  const deleteFirewallRule = async (ruleId: string): Promise<void> => {
    if (!wsManager.value || !isConnected.value) {
      throw new Error('WebSocket not connected')
    }

    await wsManager.value.sendRequest<{ id: string }, void>(
      'delete_firewall_rule' as MessageType,
      { id: ruleId }
    )
  }

  const toggleFirewallRule = async (ruleId: string): Promise<void> => {
    if (!wsManager.value || !isConnected.value) {
      throw new Error('WebSocket not connected')
    }

    await wsManager.value.sendRequest<{ id: string }, void>(
      'toggle_firewall_rule' as MessageType,
      { id: ruleId }
    )
  }

  // 断开连接
  const disconnect = (): void => {
    if (wsManager.value) {
      wsManager.value.disconnect()
      wsManager.value = null
    }
    connectionState.value = 'disconnected' as ConnectionState
  }

  // 清除错误
  const clearError = (): void => {
    lastError.value = null
  }

  return {
    // 状态
    connectionState,
    systemInfo,
    networkStatus,
    firewallRules,
    lastError,
    isLoading,

    // 计算属性
    isConnected,
    connectionCount,
    activeNetworkInterfaces,

    // 方法
    initialize,
    fetchSystemInfo,
    fetchNetworkStatus,
    fetchFirewallRules,
    addFirewallRule,
    updateFirewallRule,
    deleteFirewallRule,
    toggleFirewallRule,
    disconnect,
    clearError
  }
})
