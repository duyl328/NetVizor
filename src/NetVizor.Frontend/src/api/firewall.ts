/**
 * 防火墙API服务
 */
import { httpClient } from '@/utils/http'
import type { ApiResponse } from '@/types/http'
import type {
  FirewallRule,
  CreateRuleRequest,
  UpdateRuleRequest,
  RuleFilter,
  FirewallStatus,
  FirewallStatistics,
  FirewallRulesResponse,
  DisplayRule,
} from '@/types/firewall'

import { RuleDirection, RuleAction, ProtocolType } from '@/types/firewall'

class FirewallApiService {
  private readonly baseUrl = '/firewall'

  /**
   * 获取防火墙规则列表（支持分页和筛选）
   */
  async getRules(filter?: RuleFilter): Promise<FirewallRulesResponse> {
    const params: Record<string, string> = {}

    if (filter) {
      if (filter.name) params.name = filter.name
      if (filter.direction) params.direction = filter.direction.toString()
      if (typeof filter.enabled === 'boolean') params.enabled = filter.enabled.toString()
      if (filter.protocol) params.protocol = filter.protocol.toString()
      if (filter.action) params.action = filter.action.toString()
      if (filter.application) params.application = filter.application
      if (filter.port) params.port = filter.port
      if (typeof filter.start === 'number') params.start = filter.start.toString()
      if (typeof filter.limit === 'number') params.limit = filter.limit.toString()
    }

    const response = await httpClient.get<FirewallRulesResponse>(`${this.baseUrl}/rules`, params)
    return response.data!
  }

  /**
   * 创建防火墙规则
   */
  async createRule(rule: CreateRuleRequest): Promise<void> {
    await httpClient.post(`${this.baseUrl}/rules`, rule)
  }

  /**
   * 更新防火墙规则
   */
  async updateRule(rule: UpdateRuleRequest): Promise<void> {
    await httpClient.put(`${this.baseUrl}/rules`, rule)
  }

  /**
   * 删除防火墙规则
   */
  async deleteRule(ruleName: string): Promise<void> {
    await httpClient.delete(`${this.baseUrl}/rules`, { name: ruleName })
  }

  /**
   * 获取防火墙状态
   */
  async getStatus(): Promise<FirewallStatus> {
    const response = await httpClient.get<FirewallStatus>(`${this.baseUrl}/status`)
    return response.data!
  }

  /**
   * 获取防火墙统计信息
   */
  async getStatistics(): Promise<FirewallStatistics> {
    const response = await httpClient.get<FirewallStatistics>(`${this.baseUrl}/statistics`)
    return response.data!
  }

  /**
   * 转换后端规则数据为前端显示格式
   */
  convertToDisplayRule(rule: FirewallRule): DisplayRule {
    // 将Profile枚举转换为字符串数组
    const profiles: string[] = []
    if ((rule.profiles & 1) === 1) profiles.push('域') // Domain
    if ((rule.profiles & 2) === 2) profiles.push('专用') // Private
    if ((rule.profiles & 4) === 4) profiles.push('公用') // Public
    let direction
    if (rule.direction === null || rule.direction === undefined) {
      direction = RuleDirection.None
    } else {
      direction = rule.direction === RuleDirection.Inbound ? 'inbound' : 'outbound'
    }
    return {
      id: rule.name, // 使用规则名称作为ID
      name: rule.name,
      description: rule.description,
      enabled: rule.enabled,
      direction: direction,
      action: rule.action === RuleAction.Allow ? 'allow' : 'block',
      program: rule.applicationName || rule.serviceName || '任意',
      protocol: this.getProtocolDisplayName(rule.protocol),
      port: rule.localPorts || rule.remotePorts || '任意',
      profiles,
    }
  }

  /**
   * 转换前端显示规则为后端创建请求格式
   */
  convertToCreateRequest(displayRule: DisplayRule): CreateRuleRequest {
    // 将配置文件字符串数组转换为枚举值
    let profiles = 0
    displayRule.profiles.forEach((profile) => {
      switch (profile) {
        case '域':
          profiles |= 1
          break // Domain
        case '专用':
          profiles |= 2
          break // Private
        case '公用':
          profiles |= 4
          break // Public
      }
    })

    return {
      name: displayRule.name,
      description: displayRule.description,
      direction:
        displayRule.direction === 'inbound' ? RuleDirection.Inbound : RuleDirection.Outbound,
      action: displayRule.action === 'allow' ? RuleAction.Allow : RuleAction.Block,
      enabled: displayRule.enabled,
      applicationName: displayRule.program !== '任意' ? displayRule.program : '',
      protocol: this.getProtocolTypeFromName(displayRule.protocol),
      localPorts: displayRule.port !== '任意' ? displayRule.port : '',
      profiles: profiles || 7, // 默认All (Domain | Private | Public)
      localAddresses: '*',
      remoteAddresses: '*',
    }
  }

  /**
   * 获取协议显示名称
   */
  private getProtocolDisplayName(protocol: ProtocolType): string {
    switch (protocol) {
      case ProtocolType.TCP:
        return 'TCP'
      case ProtocolType.UDP:
        return 'UDP'
      case ProtocolType.ICMPV4:
        return 'ICMP'
      case ProtocolType.ICMPv6:
        return 'ICMPv6'
      case ProtocolType.Any:
        return '任意'
      default:
        return '其他'
    }
  }

  /**
   * 从显示名称获取协议类型枚举
   */
  private getProtocolTypeFromName(protocolName: string): ProtocolType {
    switch (protocolName.toUpperCase()) {
      case 'TCP':
        return ProtocolType.TCP
      case 'UDP':
        return ProtocolType.UDP
      case 'ICMP':
        return ProtocolType.ICMPV4
      case 'ICMPV6':
        return ProtocolType.ICMPv6
      case '任意':
        return ProtocolType.Any
      default:
        return ProtocolType.Any
    }
  }

  /**
   * 批量操作规则（启用/禁用/删除）
   */
  async batchUpdateRules(
    ruleNames: string[],
    operation: 'enable' | 'disable' | 'delete',
  ): Promise<void> {
    const promises = ruleNames.map(async (name) => {
      switch (operation) {
        case 'enable':
          return this.updateRule({ currentName: name, enabled: true })
        case 'disable':
          return this.updateRule({ currentName: name, enabled: false })
        case 'delete':
          return this.deleteRule(name)
      }
    })

    await Promise.all(promises)
  }
}

export const firewallApi = new FirewallApiService()
