// types/websocket.ts
export interface WebSocketMessage<T = unknown> {
  type: string
  data: T
  timestamp: number
  requestId?: string
}

export interface NetworkStatus {
  interfaceName: string
  isConnected: boolean
  ipAddress: string
  macAddress: string
  speed: number
  bytesReceived: number
  bytesSent: number
}

export interface FirewallRule {
  id: string
  name: string
  direction: 'inbound' | 'outbound'
  action: 'allow' | 'block'
  protocol: 'tcp' | 'udp' | 'icmp' | 'any'
  localPort?: string
  remotePort?: string
  localAddress?: string
  remoteAddress?: string
  enabled: boolean
}

export interface SystemInfo {
  cpuUsage: number
  memoryUsage: number
  networkInterfaces: NetworkStatus[]
  firewallRules: FirewallRule[]
  connectionCount: number
}

export enum MessageType {
  // 数据获取相关
  GET_NETWORK_STATUS = 'get_network_status',
  GET_FIREWALL_RULES = 'get_firewall_rules',
  GET_SYSTEM_INFO = 'get_system_info',

  // 数据响应相关
  NETWORK_STATUS_RESPONSE = 'network_status_response',
  FIREWALL_RULES_RESPONSE = 'firewall_rules_response',
  SYSTEM_INFO_RESPONSE = 'system_info_response',

  // 操作相关
  ADD_FIREWALL_RULE = 'add_firewall_rule',
  UPDATE_FIREWALL_RULE = 'update_firewall_rule',
  DELETE_FIREWALL_RULE = 'delete_firewall_rule',
  TOGGLE_FIREWALL_RULE = 'toggle_firewall_rule',

  // 通知相关
  NETWORK_STATUS_CHANGED = 'network_status_changed',
  FIREWALL_RULE_CHANGED = 'firewall_rule_changed',
  SYSTEM_ALERT = 'system_alert',
  ERROR = 'error',
  SUCCESS = 'success'
}

export interface WebSocketConfig {
  url: string
  /**
   * 重新连接间隔
   */
  reconnectInterval: number
  /**
   * 最大重新连接尝试次数
   */
  maxReconnectAttempts: number
  /**
   * 心跳间隔
   */
  heartbeatInterval: number
  timeout: number
}

export enum ConnectionState {
  CONNECTING = 'connecting',
  CONNECTED = 'connected',
  DISCONNECTED = 'disconnected',
  RECONNECTING = 'reconnecting',
  ERROR = 'error'
}
