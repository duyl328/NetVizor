import { FirewallRule } from '@/types/firewall'

/**
 * 防火墙对象 - 展示所用
 */
export interface FirewallRuleShow {
  id: string
  name: string
  description: string
  enabled: boolean
  /**
   * 入站/出站
   */
  direction: string
  /**
   * 禁止/放行
   */
  action: string
  program: string
  protocol: string
  port: string
  profiles: string[]
  grouping?: string
}

export interface FirewallRuleResponse {
  rules: FirewallRule[]
  totalCount: number
  startIndex: number
  limit: number
  hasMore: boolean
}

/**
 * 防火墙请求规则
 */
export interface FirewallRulesParam {
  start?: number
  limit?: number
  name?: string
  direction?: string
  enabled?: boolean
  protocol?: number
  action?: number
  application?: string
  port?: number
  search?: string
}

/**
 * 创建防火墙规则请求
 */
export interface CreateFirewallRuleRequest {
  name: string
  description?: string
  applicationName?: string
  localAddresses?: string
  remoteAddresses?: string
  protocol?: string
  icmpTypesAndCodes?: string
  localPorts?: string
  remotePorts?: string
  direction: string
  enabled: boolean
  profiles: string
  action: string
  grouping?: string
  interfaceTypes?: string
  edgeTraversal?: boolean
}

/**
 * 更新防火墙规则请求
 */
export interface UpdateFirewallRuleRequest {
  currentName: string
  newName?: string
  description?: string
  enabled?: boolean
  applicationName?: string
  protocol?: string
  localPorts?: string
  remotePorts?: string
  localAddresses?: string
  remoteAddresses?: string
  profiles?: string
  action?: string
  grouping?: string
  edgeTraversal?: boolean
}
