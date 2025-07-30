/**
 * 防火墙相关类型定义
 */

// 防火墙配置文件类型
export enum FirewallProfile {
  Domain = 1,
  Private = 2,
  Public = 4,
  All = Domain | Private | Public,
}

// 防火墙规则方向
export enum RuleDirection {
  Inbound = 1,
  Outbound = 2,
  None = -1,
}

// 防火墙规则动作
export enum RuleAction {
  Allow = 1,
  Block = 0,
}

// 协议类型
export enum ProtocolType {
  Any = 256,
  ICMPV4 = 1,
  IGMP = 2,
  TCP = 6,
  UDP = 17,
  IPv6 = 41,
  IPv6Route = 43,
  IPv6Frag = 44,
  GRE = 47,
  ICMPv6 = 58,
  IPv6NoNxt = 59,
  IPv6Opts = 60,
  VRRP = 112,
  PGM = 113,
  L2TP = 115,
}

// 防火墙规则详细信息
export interface FirewallRule {
  name: string
  uniqueId?: string // 添加唯一标识符字段，用于解决重复规则显示问题
  description: string
  applicationName: string
  serviceName: string
  protocol: ProtocolType
  localPorts: string
  remotePorts: string
  localAddresses: string
  remoteAddresses: string
  icmpTypesAndCodes: string
  direction: RuleDirection
  enabled: boolean
  profiles: FirewallProfile
  edgeTraversal: boolean
  action: RuleAction
  grouping: string
  interfaceTypes: string
  interfaces: string[]
  creationDate?: string
  modificationDate?: string
  edgeTraversalAllowed: boolean
  looseSourceMapping: boolean
  localOnlyMapping: boolean
  remoteMachineAuthorizationList: string
  remoteUserAuthorizationList: string
  embeddedContext: string
  flags: number
  secureFlags: boolean
}

// 创建防火墙规则的请求
export interface CreateRuleRequest {
  name: string
  description?: string
  applicationName?: string
  serviceName?: string
  protocol?: ProtocolType
  localPorts?: string
  remotePorts?: string
  localAddresses?: string
  remoteAddresses?: string
  icmpTypesAndCodes?: string
  direction: RuleDirection
  enabled?: boolean
  profiles?: FirewallProfile
  action?: RuleAction
  grouping?: string
  interfaceTypes?: string
  interfaces?: string[]
  edgeTraversal?: boolean
  edgeTraversalAllowed?: boolean
  remoteMachineAuthorizationList?: string
  remoteUserAuthorizationList?: string
}

// 更新防火墙规则的请求
export interface UpdateRuleRequest {
  currentName: string
  newName?: string
  description?: string
  enabled?: boolean
  applicationName?: string
  serviceName?: string
  protocol?: ProtocolType
  localPorts?: string
  remotePorts?: string
  localAddresses?: string
  remoteAddresses?: string
  profiles?: FirewallProfile
  action?: RuleAction
  grouping?: string
  edgeTraversal?: boolean
}

// 防火墙规则查询过滤器
export interface RuleFilter {
  name?: string
  direction?: RuleDirection | string
  enabled?: boolean
  profile?: FirewallProfile
  protocol?: ProtocolType | string
  action?: RuleAction | string
  application?: string
  grouping?: string
  port?: string
  start?: number
  limit?: number
}

// 防火墙状态信息
export interface FirewallStatus {
  isEnabled: boolean
  profileStatuses: Record<string, ProfileStatus>
  totalRules: number
  enabledRules: number
  inboundRules: number
  outboundRules: number
  lastModified: string
}

// 配置文件状态
export interface ProfileStatus {
  profile: FirewallProfile
  isEnabled: boolean
  blockAllInboundTraffic: boolean
  notifyOnListen: boolean
  unicastResponsesDisabled: boolean
  defaultInboundAction: RuleAction
  defaultOutboundAction: RuleAction
}

// 防火墙统计信息
export interface FirewallStatistics {
  totalRules: number
  enabledRules: number
  disabledRules: number
  rulesByDirection: Record<string, number>
  rulesByProtocol: Record<string, number>
  rulesByProfile: Record<string, number>
  rulesByAction: Record<string, number>
  topApplications: string[]
  topPorts: string[]
}

// 防火墙规则查询响应
export interface FirewallRulesResponse {
  rules: FirewallRule[]
  totalCount: number
  startIndex: number
  limit: number
  hasMore: boolean
}

// 前端显示用的规则接口（兼容现有组件）
export interface DisplayRule {
  id: string
  name: string
  description: string
  enabled: boolean
  direction: 'inbound' | 'outbound'
  action: 'allow' | 'block'
  program: string
  protocol: string
  port: string
  profiles: string[]
  priority?: number
}
