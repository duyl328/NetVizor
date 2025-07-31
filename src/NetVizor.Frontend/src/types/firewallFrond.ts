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
