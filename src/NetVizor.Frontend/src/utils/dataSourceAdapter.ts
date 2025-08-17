/**
 * 数据源适配器
 * 根据环境自动切换真实数据源和模拟数据源
 */

import { environmentDetector, DataSourceType } from './environmentDetector'
import { mockDataService } from './mockDataService'
import type { ProcessType, ConnectionInfo } from '@/types/process'
import type { FirewallRule, FirewallStatus } from '@/types/firewall'
import type { WebSocketResponse } from '@/types/websocket'

class DataSourceAdapter {
  private static instance: DataSourceAdapter | null = null

  // 模拟实时数据的定时器
  private realtimeTimers: Map<string, NodeJS.Timeout> = new Map()

  static getInstance(): DataSourceAdapter {
    if (!DataSourceAdapter.instance) {
      DataSourceAdapter.instance = new DataSourceAdapter()
    }
    return DataSourceAdapter.instance
  }

  // ================= 进程和连接数据获取 =================

  /**
   * 获取进程列表
   */
  async getProcessList(): Promise<ProcessType[]> {
    const dataSourceType = environmentDetector.getDataSourceType()

    switch (dataSourceType) {
      case DataSourceType.MOCK_DATA:
        console.log('[DataAdapter] 使用模拟进程数据')
        return await this.getMockProcessList()

      case DataSourceType.REAL_DATA:
        console.log('[DataAdapter] 使用WebView2桥接获取进程数据')
        return await this.getRealProcessList()

      case DataSourceType.DEV_API:
        console.log('[DataAdapter] 使用开发API获取进程数据')
        return await this.getApiProcessList()

      default:
        console.warn('[DataAdapter] 未知数据源类型，使用模拟数据')
        return await this.getMockProcessList()
    }
  }

  /**
   * 获取连接列表
   */
  async getConnectionList(): Promise<ConnectionInfo[]> {
    const dataSourceType = environmentDetector.getDataSourceType()

    switch (dataSourceType) {
      case DataSourceType.MOCK_DATA:
        return await this.getMockConnectionList()

      case DataSourceType.REAL_DATA:
        return await this.getRealConnectionList()

      case DataSourceType.DEV_API:
        return await this.getApiConnectionList()

      default:
        return await this.getMockConnectionList()
    }
  }

  // ================= 防火墙数据获取 =================

  /**
   * 获取防火墙状态
   */
  async getFirewallStatus(): Promise<FirewallStatus> {
    const dataSourceType = environmentDetector.getDataSourceType()

    switch (dataSourceType) {
      case DataSourceType.MOCK_DATA:
        return await this.getMockFirewallStatus()

      case DataSourceType.REAL_DATA:
        return await this.getRealFirewallStatus()

      case DataSourceType.DEV_API:
        return await this.getApiFirewallStatus()

      default:
        return await this.getMockFirewallStatus()
    }
  }

  /**
   * 获取防火墙规则列表
   */
  async getFirewallRules(): Promise<FirewallRule[]> {
    const dataSourceType = environmentDetector.getDataSourceType()

    switch (dataSourceType) {
      case DataSourceType.MOCK_DATA:
        return await this.getMockFirewallRules()

      case DataSourceType.REAL_DATA:
        return await this.getRealFirewallRules()

      case DataSourceType.DEV_API:
        return await this.getApiFirewallRules()

      default:
        return await this.getMockFirewallRules()
    }
  }

  // ================= 实时数据订阅 =================

  /**
   * 获取应用程序列表
   */
  async getApplicationList(): Promise<any[]> {
    const dataSourceType = environmentDetector.getDataSourceType()

    switch (dataSourceType) {
      case DataSourceType.MOCK_DATA:
        console.log('[DataAdapter] 使用模拟应用程序数据')
        return await this.getMockApplicationList()

      case DataSourceType.REAL_DATA:
        console.log('[DataAdapter] 使用WebView2桥接获取应用程序数据')
        return await this.getRealApplicationList()

      case DataSourceType.DEV_API:
        console.log('[DataAdapter] 使用开发API获取应用程序数据')
        return await this.getApiApplicationList()

      default:
        console.warn('[DataAdapter] 未知数据源类型，使用模拟应用程序数据')
        return await this.getMockApplicationList()
    }
  }

  /**
   * 订阅实时数据更新
   */
  subscribeRealtimeData(
    callback: (data: WebSocketResponse) => void,
    interval = 2000
  ): string {
    const subscriptionId = `sub_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`
    const dataSourceType = environmentDetector.getDataSourceType()

    switch (dataSourceType) {
      case DataSourceType.MOCK_DATA:
        this.subscribeMockRealtimeData(subscriptionId, callback, interval)
        break

      case DataSourceType.REAL_DATA:
        this.subscribeRealRealtimeData(subscriptionId, callback)
        break

      case DataSourceType.DEV_API:
        this.subscribeApiRealtimeData(subscriptionId, callback)
        break
    }

    console.log(`[DataAdapter] 已订阅实时数据更新: ${subscriptionId}`)
    return subscriptionId
  }

  /**
   * 取消订阅实时数据
   */
  unsubscribeRealtimeData(subscriptionId: string): void {
    const timer = this.realtimeTimers.get(subscriptionId)
    if (timer) {
      clearInterval(timer)
      this.realtimeTimers.delete(subscriptionId)
      console.log(`[DataAdapter] 已取消订阅: ${subscriptionId}`)
    }
  }

  // ================= 模拟数据实现 =================

  private async getMockProcessList(): Promise<ProcessType[]> {
    // 演示模式立即返回数据，不模拟延迟
    const processes = mockDataService.generateProcessList(8) // 减少到8个进程，更符合实际
    console.log(`[${new Date().toLocaleTimeString()}] [DataAdapter] 生成演示进程数据:`, processes.length, '个进程')
    return processes
  }

  private async getMockConnectionList(): Promise<ConnectionInfo[]> {
    await this.delay(50, 150)
    const processes = await this.getMockProcessList()
    const connections: ConnectionInfo[] = []

    processes.forEach(process => {
      connections.push(...process.connections)
    })

    return connections
  }

  private async getMockApplicationList(): Promise<any[]> {
    const applications = mockDataService.generateApplicationList(15)
    console.log(`[${new Date().toLocaleTimeString()}] [DataAdapter] 生成演示应用数据:`, applications.length, '个应用')
    return applications
  }

  private async getMockFirewallStatus(): Promise<FirewallStatus> {
    await this.delay(50, 100)
    return mockDataService.generateFirewallStatus()
  }

  private async getMockFirewallRules(): Promise<FirewallRule[]> {
    await this.delay(100, 200)
    return mockDataService.generateFirewallRules(35)
  }

  private subscribeMockRealtimeData(
    subscriptionId: string,
    callback: (data: WebSocketResponse) => void,
    interval: number
  ): void {
    const timer = setInterval(() => {
      const update = mockDataService.generateRealtimeUpdate()
      callback(update)
    }, interval)

    this.realtimeTimers.set(subscriptionId, timer)
  }

  // ================= 真实数据实现 (WebView2桥接) =================

  private async getRealProcessList(): Promise<ProcessType[]> {
    // TODO: 实现WebView2桥接调用
    console.log('[DataAdapter] 调用WebView2桥接获取进程数据')
    // 临时返回空数组，待后端桥接实现
    return []
  }

  private async getRealConnectionList(): Promise<ConnectionInfo[]> {
    // TODO: 实现WebView2桥接调用
    console.log('[DataAdapter] 调用WebView2桥接获取连接数据')
    return []
  }

  private async getRealApplicationList(): Promise<any[]> {
    // TODO: 实现WebView2桥接调用
    console.log('[DataAdapter] 调用WebView2桥接获取应用程序数据')
    return []
  }

  private async getRealFirewallStatus(): Promise<FirewallStatus> {
    // TODO: 实现WebView2桥接调用
    console.log('[DataAdapter] 调用WebView2桥接获取防火墙状态')
    return mockDataService.generateFirewallStatus() // 临时使用模拟数据
  }

  private async getRealFirewallRules(): Promise<FirewallRule[]> {
    // TODO: 实现WebView2桥接调用
    console.log('[DataAdapter] 调用WebView2桥接获取防火墙规则')
    return []
  }

  private subscribeRealRealtimeData(
    subscriptionId: string,
    callback: (data: WebSocketResponse) => void
  ): void {
    // TODO: 实现WebView2桥接实时数据订阅
    console.log('[DataAdapter] 订阅WebView2桥接实时数据')
  }

  // ================= API数据实现 (开发环境) =================

  private async getApiProcessList(): Promise<ProcessType[]> {
    try {
      const apiUrl = environmentDetector.getApiBaseUrl()
      console.log(`[DataAdapter] 调用API获取进程数据: ${apiUrl}/processes`)

      // TODO: 实现真实API调用
      // const response = await fetch(`${apiUrl}/processes`)
      // return await response.json()

      // 临时使用模拟数据
      return await this.getMockProcessList()
    } catch (error) {
      console.warn('[DataAdapter] API调用失败，使用模拟数据', error)
      return await this.getMockProcessList()
    }
  }

  private async getApiConnectionList(): Promise<ConnectionInfo[]> {
    try {
      const apiUrl = environmentDetector.getApiBaseUrl()
      console.log(`[DataAdapter] 调用API获取连接数据: ${apiUrl}/connections`)
      return await this.getMockConnectionList()
    } catch (error) {
      console.warn('[DataAdapter] API调用失败，使用模拟数据', error)
      return await this.getMockConnectionList()
    }
  }

  private async getApiApplicationList(): Promise<any[]> {
    try {
      const apiUrl = environmentDetector.getApiBaseUrl()
      console.log(`[DataAdapter] 调用API获取应用程序数据: ${apiUrl}/applications`)
      return await this.getMockApplicationList()
    } catch (error) {
      console.warn('[DataAdapter] API调用失败，使用模拟应用程序数据', error)
      return await this.getMockApplicationList()
    }
  }

  private async getApiFirewallStatus(): Promise<FirewallStatus> {
    try {
      const apiUrl = environmentDetector.getApiBaseUrl()
      console.log(`[DataAdapter] 调用API获取防火墙状态: ${apiUrl}/firewall/status`)
      return await this.getMockFirewallStatus()
    } catch (error) {
      console.warn('[DataAdapter] API调用失败，使用模拟数据', error)
      return await this.getMockFirewallStatus()
    }
  }

  private async getApiFirewallRules(): Promise<FirewallRule[]> {
    try {
      const apiUrl = environmentDetector.getApiBaseUrl()
      console.log(`[DataAdapter] 调用API获取防火墙规则: ${apiUrl}/firewall/rules`)
      return await this.getMockFirewallRules()
    } catch (error) {
      console.warn('[DataAdapter] API调用失败，使用模拟数据', error)
      return await this.getMockFirewallRules()
    }
  }

  private subscribeApiRealtimeData(
    subscriptionId: string,
    callback: (data: WebSocketResponse) => void
  ): void {
    // TODO: 实现WebSocket API订阅
    console.log('[DataAdapter] 订阅API WebSocket实时数据')

    // 临时使用模拟数据
    this.subscribeMockRealtimeData(subscriptionId, callback, 3000)
  }

  // ================= 工具方法 =================

  /**
   * 模拟网络延迟
   */
  private delay(min: number, max: number): Promise<void> {
    const delay = Math.floor(Math.random() * (max - min + 1)) + min
    return new Promise(resolve => setTimeout(resolve, delay))
  }

  /**
   * 清理所有订阅
   */
  cleanup(): void {
    this.realtimeTimers.forEach((timer, id) => {
      clearInterval(timer)
      console.log(`[DataAdapter] 清理订阅: ${id}`)
    })
    this.realtimeTimers.clear()
  }

  /**
   * 获取数据源信息
   */
  getDataSourceInfo() {
    return {
      dataSourceType: environmentDetector.getDataSourceType(),
      environmentType: environmentDetector.getEnvironmentType(),
      activeSubscriptions: this.realtimeTimers.size,
      subscriptionIds: Array.from(this.realtimeTimers.keys())
    }
  }
}

// 导出单例实例
export const dataSourceAdapter = DataSourceAdapter.getInstance()

// 在页面卸载时清理资源
if (typeof window !== 'undefined') {
  window.addEventListener('beforeunload', () => {
    dataSourceAdapter.cleanup()
  })
}

export default dataSourceAdapter
