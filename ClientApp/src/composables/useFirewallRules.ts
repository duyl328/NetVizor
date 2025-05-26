/**
 * Time:2025/5/25 19:16 09
 * Name:useFirewallRules.ts
 * Path:src/composables
 * ProjectName:ClientApp
 * Author:charlatans
 *
 *  Il n'ya qu'un héroïsme au monde :
 *     c'est de voir le monde tel qu'il est et de l'aimer.
 */
// composables/useFirewallRules.ts - 防火墙规则管理
import { ref, onMounted, onUnmounted } from 'vue'
import { useWebSocket } from './useWebSocket'
import type { FirewallRuleMessage } from '@/types/websocket'

interface FirewallRule {
  name: string
  action: string
  protocol: string
  port: string
  enabled: boolean
}

export function useFirewallRules() {
  const { sendCommand, onMessage, isConnected } = useWebSocket()

  const firewallRules = ref<FirewallRule[]>([])
  const loading = ref(false)
  const cleanupFunctions: (() => void)[] = []

  // 获取防火墙规则
  const fetchFirewallRules = async () => {
    if (!isConnected.value) return

    loading.value = true
    try {
      await sendCommand('getFirewallRules')
    } catch (error) {
      console.error('获取防火墙规则失败:', error)
    } finally {
      loading.value = false
    }
  }

  // 添加防火墙规则
  const addFirewallRule = async (rule: Omit<FirewallRule, 'enabled'>) => {
    if (!isConnected.value) return false

    const ruleData: FirewallRuleMessage = {
      Type: 'firewallRule',
      RuleName: rule.name,
      Action: rule.action,
      Protocol: rule.protocol,
      Port: rule.port,
      Enabled: true,
      Timestamp: new Date().toISOString()
    }

    return await sendCommand('addFirewallRule', ruleData)
  }

  // 处理防火墙规则响应
  const handleFirewallRulesResponse = (message: any) => {
    if (message.Success && message.Data && Array.isArray(message.Data)) {
      firewallRules.value = message.Data.map((item: FirewallRuleMessage) => ({
        name: item.RuleName,
        action: item.Action,
        protocol: item.Protocol,
        port: item.Port,
        enabled: item.Enabled
      }))
    }
  }

  onMounted(() => {
    // 监听防火墙规则响应
    cleanupFunctions.push(
      onMessage('firewallRules', handleFirewallRulesResponse)
    )

    // 监听防火墙规则更新
    cleanupFunctions.push(
      onMessage('firewallRulesUpdate', handleFirewallRulesResponse)
    )

    // 监听添加规则响应
    cleanupFunctions.push(
      onMessage('addFirewallRuleResponse', (message) => {
        if (message.Success) {
          console.log('防火墙规则添加成功')
          // 规则会通过广播更新自动刷新
        } else {
          console.error('防火墙规则添加失败:', message.Message)
        }
      })
    )

    // 连接成功后获取规则
    cleanupFunctions.push(
      onMessage('welcome', () => {
        fetchFirewallRules()
      })
    )
  })

  onUnmounted(() => {
    cleanupFunctions.forEach(cleanup => cleanup())
  })

  return {
    firewallRules,
    loading,
    fetchFirewallRules,
    addFirewallRule
  }
}
