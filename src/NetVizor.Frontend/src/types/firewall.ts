// 所有 DateTime 在前端统一采用 string

// FirewallProfile 枚举（使用 bit flags）
export enum FirewallProfile {
  Domain = 1,
  Private = 2,
  Public = 4,
  All = Domain | Private | Public,
}

// RuleDirection 枚举
export enum RuleDirection {
  Inbound = 1,
  Outbound = 2,
}

// RuleAction 枚举
export enum RuleAction {
  Allow = 1,
  Block = 0,
}

// ProtocolType 枚举
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

// 防火墙状态信息
export interface FirewallStatus {
  isEnabled: boolean
  profileStatuses: Record<FirewallProfile, ProfileStatus>
  totalRules: number
  enabledRules: number
  inboundRules: number
  outboundRules: number
  lastModified: string // ISO 格式字符串或 Date 类型
}

// 防火墙规则详细信息
export interface FirewallRule {
  name: string
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

  // 高级属性
  edgeTraversalAllowed: boolean
  looseSourceMapping: boolean
  localOnlyMapping: boolean
  remoteMachineAuthorizationList: string
  remoteUserAuthorizationList: string
  embeddedContext: string
  flags: number
  secureFlags: boolean
}

// 创建规则请求
export interface CreateRuleRequest {
  name: string
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
  action: RuleAction
  grouping: string
  interfaceTypes: string
  interfaces: string[]
  edgeTraversal: boolean
  edgeTraversalAllowed: boolean
  remoteMachineAuthorizationList: string
  remoteUserAuthorizationList: string
}

// 更新规则请求
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

// 规则过滤器
export interface RuleFilter {
  namePattern?: string
  direction?: RuleDirection
  enabled?: boolean
  profile?: FirewallProfile
  protocol?: ProtocolType
  action?: RuleAction
  applicationName?: string
  grouping?: string
  port?: string
}

// 防火墙统计信息
export interface FirewallStatistics {
  totalRules: number
  enabledRules: number
  disabledRules: number
  rulesByDirection: Record<RuleDirection, number>
  rulesByProtocol: Record<ProtocolType, number>
  rulesByProfile: Record<FirewallProfile, number>
  rulesByAction: Record<RuleAction, number>
  topApplications: string[]
  topPorts: string[]
}
