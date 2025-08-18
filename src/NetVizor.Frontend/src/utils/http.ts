import axios, {
  type AxiosInstance,
  type AxiosRequestConfig,
  type AxiosResponse,
  AxiosError,
  type InternalAxiosRequestConfig,
} from 'axios'
// 或者混合导入（如果同时需要导入值和类型）
import type { ApiResponse, HttpConfig, RequestConfig } from '@/types/http.ts'
import { disLog, logB, logN, logS } from '@/utils/logHelper/logUtils'
import { environmentDetector, shouldUseMockData } from '@/utils/environmentDetector'
import { dataSourceAdapter } from '@/utils/dataSourceAdapter'

class HttpClient {
  private axiosInstance: AxiosInstance
  private pending: Map<string, AbortController> = new Map()
  private isDemoMode: boolean = shouldUseMockData()

  constructor() {
    this.axiosInstance = axios.create()
    this.setupInterceptors()

    // 打印环境信息
    console.log('[HttpClient] 初始化HTTP客户端')
    console.log('[HttpClient] 环境信息:', environmentDetector.getEnvironmentInfo())
  }

  /**
   * 更新配置
   */
  updateConfig(config: HttpConfig): void {
    this.axiosInstance.defaults.baseURL = config.baseURL
    this.axiosInstance.defaults.timeout = config.timeout || 10000
    this.axiosInstance.defaults.headers.common = {
      ...this.axiosInstance.defaults.headers.common,
      ...config.headers,
    }
  }

  /**
   * 生成请求标识符
   */
  private getRequestKey(config: AxiosRequestConfig): string {
    return `${config.method?.toUpperCase()}_${config.url}_${JSON.stringify(
      config.params,
    )}_${JSON.stringify(config.data)}`
  }

  /**
   * 添加请求到待处理列表
   */
  private addPending(config: InternalAxiosRequestConfig): void {
    const requestKey = this.getRequestKey(config)
    config.signal = new AbortController().signal

    if (this.pending.has(requestKey)) {
      this.pending.get(requestKey)?.abort()
    }

    const controller = new AbortController()
    config.signal = controller.signal
    this.pending.set(requestKey, controller)
  }

  /**
   * 移除请求从待处理列表
   */
  private removePending(config: AxiosRequestConfig): void {
    const requestKey = this.getRequestKey(config)
    if (this.pending.has(requestKey)) {
      this.pending.delete(requestKey)
    }
  }

  /**
   * 设置拦截器
   */
  private setupInterceptors(): void {
    // 请求拦截器
    this.axiosInstance.interceptors.request.use(
      (config: InternalAxiosRequestConfig) => {
        console.group(
          '请求链接: ' +
            config.url +
            '  ' +
            config.method?.toUpperCase() +
            '  params: ' +
            JSON.stringify(config.params) +
            ',  data: ' +
            JSON.stringify(config.data),
        )
        // 防止重复请求
        this.addPending(config)

        // 添加时间戳防止缓存
        if (config.method?.toLowerCase() === 'get') {
          config.params = {
            ...config.params,
            _t: Date.now(),
          }
        }

        // 添加token
        const token = localStorage.getItem('token')
        if (token) {
          config.headers.Authorization = `Bearer ${token}`
        }

        logB.success('请求发送:', config)
        return config
      },
      (error: AxiosError) => {
        logB.error('请求错误:', error)
        return Promise.reject(error)
      },
    )

    // 响应拦截器
    this.axiosInstance.interceptors.response.use(
      (response: AxiosResponse<ApiResponse>) => {
        this.removePending(response.config)

        logB.success('响应接收:', response)

        // 统一处理响应数据
        const { data } = response

        console.groupEnd()
        if (data.success || data.code === 200) {
          return response
        } else {
          // 业务错误处理
          const errorMessage = data.message || '请求失败'
          return Promise.reject(new Error(errorMessage))
        }
      },
      (error: AxiosError<ApiResponse>) => {
        this.removePending(error.config || {})

        logB.error('响应错误:', error)

        // 统一错误处理
        this.handleError(error)
        console.groupEnd()
        return Promise.reject(error)
      },
    )
  }

  /**
   * 统一错误处理
   */
  private handleError(error: AxiosError<ApiResponse>): void {
    let message = '网络错误'

    if (error.response) {
      const { status, data } = error.response

      switch (status) {
        case 400:
          message = data?.message || '请求参数错误'
          break
        case 401:
          message = '未授权，请重新登录'
          // 清除token并跳转到登录页
          localStorage.removeItem('token')
          // router.push('/login');
          break
        case 403:
          message = '拒绝访问'
          break
        case 404:
          message = '请求的资源不存在'
          break
        case 500:
          message = '服务器内部错误'
          break
        default:
          message = data?.message || `错误码: ${status}`
      }
    } else if (error.code === 'ECONNABORTED') {
      message = '请求超时'
    } else if (error.message.includes('Network Error')) {
      message = '网络连接异常'
    }
    logB.error(message)
  }

  /**
   * 处理演示模式的API请求
   */
  private async handleDemoRequest<T = unknown>(url: string, method: string, data?: unknown, params?: unknown): Promise<ApiResponse<T>> {
    console.log(`[HttpClient] 演示模式：模拟API请求 ${method} ${url}`, data)

    // 模拟网络延迟
    await new Promise(resolve => setTimeout(resolve, Math.random() * 500 + 100))

    try {
      // 根据URL路径返回相应的模拟数据
      let responseData: unknown

      if (url.includes('/processes') || url.includes('/process')) {
        responseData = await dataSourceAdapter.getProcessList()
      } else if (url.includes('/connections') || url.includes('/connection')) {
        responseData = await dataSourceAdapter.getConnectionList()
      } else if (url.includes('/firewall/status')) {
        responseData = await dataSourceAdapter.getFirewallStatus()
      } else if (url.includes('/firewall/rules') || url.includes('/firewall/rule')) {
        // 防火墙规则需要返回特定格式的数据
        const rules = await dataSourceAdapter.getFirewallRules()
        responseData = {
          rules: rules,
          totalCount: rules.length,
          currentPage: 0,
          pageSize: rules.length
        }
      } else if (url.includes('/stats') || url.includes('/statistics/available-ranges')) {
        // 时间范围可用性
        responseData = [
          { type: 'hour', available: true },
          { type: 'day', available: true },
          { type: 'week', available: true },
          { type: 'month', available: false }
        ]
      } else if (url.includes('/statistics/interfaces')) {
        // 网络接口列表
        responseData = [
          {
            id: 'eth0',
            name: 'Ethernet',
            displayName: '以太网',
            isActive: true,
            macAddress: '00:1B:44:11:3A:B7'
          },
          {
            id: 'wifi0',
            name: 'WiFi',
            displayName: 'WiFi适配器',
            isActive: true,
            macAddress: '00:1C:42:00:00:08'
          }
        ]
      } else if (url.includes('/traffic/trends')) {
        // 流量趋势数据
        const now = Date.now()
        const points = []
        for (let i = 0; i < 60; i++) {
          points.push({
            timestamp: Math.floor((now - (59-i) * 60000) / 1000).toString(),
            uploadSpeed: Math.floor(Math.random() * 1000000) + 100000,
            downloadSpeed: Math.floor(Math.random() * 2000000) + 500000
          })
        }
        responseData = {
          interface: 'all',
          timeRange: '1hour',
          points: points
        }
      } else if (url.includes('/traffic/top-apps')) {
        // Top应用流量数据
        const topApps = [
          { processName: 'chrome.exe', displayName: 'Google Chrome', icon: '', totalBytes: 2500000000 },
          { processName: 'firefox.exe', displayName: 'Mozilla Firefox', icon: '', totalBytes: 1800000000 },
          { processName: 'steam.exe', displayName: 'Steam', icon: '', totalBytes: 1200000000 },
          { processName: 'discord.exe', displayName: 'Discord', icon: '', totalBytes: 800000000 },
          { processName: 'spotify.exe', displayName: 'Spotify', icon: '', totalBytes: 600000000 },
          { processName: 'skype.exe', displayName: 'Skype', icon: '', totalBytes: 400000000 },
          { processName: 'zoom.exe', displayName: 'Zoom', icon: '', totalBytes: 350000000 },
          { processName: 'teams.exe', displayName: 'Microsoft Teams', icon: '', totalBytes: 300000000 },
          { processName: 'telegram.exe', displayName: 'Telegram', icon: '', totalBytes: 250000000 },
          { processName: 'qbittorrent.exe', displayName: 'qBittorrent', icon: '', totalBytes: 200000000 }
        ]
        responseData = topApps
      } else if (url.includes('/apps/top-traffic')) {
        // 软件流量排行数据
        const topTrafficApps = [
          {
            rank: 1,
            appId: 'chrome_001',
            processName: 'chrome.exe',
            displayName: 'Google Chrome',
            processPath: 'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe',
            icon: '',
            version: '120.0.6099.109',
            company: 'Google LLC',
            totalBytes: 2500000000,
            uploadBytes: 250000000,
            connectionCount: 45
          },
          {
            rank: 2,
            appId: 'firefox_001',
            processName: 'firefox.exe',
            displayName: 'Mozilla Firefox',
            processPath: 'C:\\Program Files\\Mozilla Firefox\\firefox.exe',
            icon: '',
            version: '121.0',
            company: 'Mozilla Corporation',
            totalBytes: 1800000000,
            uploadBytes: 180000000,
            connectionCount: 28
          },
          {
            rank: 3,
            appId: 'steam_001',
            processName: 'steam.exe',
            displayName: 'Steam',
            processPath: 'C:\\Program Files (x86)\\Steam\\steam.exe',
            icon: '',
            version: '3.4.15.7',
            company: 'Valve Corporation',
            totalBytes: 1200000000,
            uploadBytes: 120000000,
            connectionCount: 12
          },
          {
            rank: 4,
            appId: 'discord_001',
            processName: 'discord.exe',
            displayName: 'Discord',
            processPath: 'C:\\Users\\User\\AppData\\Local\\Discord\\app-1.0.9016\\Discord.exe',
            icon: '',
            version: '1.0.9016',
            company: 'Discord Inc.',
            totalBytes: 800000000,
            uploadBytes: 80000000,
            connectionCount: 8
          },
          {
            rank: 5,
            appId: 'spotify_001',
            processName: 'spotify.exe',
            displayName: 'Spotify',
            processPath: 'C:\\Users\\User\\AppData\\Roaming\\Spotify\\Spotify.exe',
            icon: '',
            version: '1.2.25.1011',
            company: 'Spotify AB',
            totalBytes: 600000000,
            uploadBytes: 60000000,
            connectionCount: 15
          }
        ]

        responseData = {
          total: topTrafficApps.length,
          page: 1,
          pageSize: 100,
          items: topTrafficApps
        }
      } else if (url.includes('/apps/network-analysis')) {
        // 网络分析详情数据
        const requestParams = (method === 'GET' ? params : data) as any
        const appId = requestParams?.appId
        
        if (!appId) {
          // 模拟后端的参数校验错误
          return {
            success: false,
            code: 400,
            message: '缺少必需参数 appId',
            data: null as T,
            timestamp: new Date().toISOString()
          }
        }
        
        // 根据appId生成不同的模拟数据
        const appNames = {
          'chrome_001': { name: 'Google Chrome', company: 'Google LLC', version: '120.0.6099.109' },
          'firefox_001': { name: 'Mozilla Firefox', company: 'Mozilla Corporation', version: '121.0' },
          'steam_001': { name: 'Steam', company: 'Valve Corporation', version: '3.4.15.7' },
          'discord_001': { name: 'Discord', company: 'Discord Inc.', version: '1.0.9016' },
          'spotify_001': { name: 'Spotify', company: 'Spotify AB', version: '1.2.25.1011' },
        } as any
        
        const appInfo = appNames[appId] || { name: 'Unknown Application', company: 'Unknown Company', version: '1.0.0' }
        
        // 生成模拟的网络分析数据
        const now = Date.now()
        const startTime = now - (requestParams?.timeRange === '1hour' ? 3600000 : 86400000)
        
        // 生成连接数据
        const connections = []
        for (let i = 0; i < 20; i++) {
          connections.push({
            localIP: '192.168.1.100',
            localPort: 50000 + i,
            remoteIP: `52.${Math.floor(Math.random() * 255)}.${Math.floor(Math.random() * 255)}.${Math.floor(Math.random() * 255)}`,
            remotePort: [80, 443, 8080, 3000, 5000][Math.floor(Math.random() * 5)],
            protocol: Math.random() > 0.7 ? 'UDP' : 'TCP',
            totalUpload: Math.floor(Math.random() * 10000000),
            totalDownload: Math.floor(Math.random() * 50000000),
            totalTraffic: 0,
            connectionCount: Math.floor(Math.random() * 10) + 1,
            firstSeen: new Date(startTime + Math.random() * (now - startTime)).toISOString(),
            lastSeen: new Date(now - Math.random() * 3600000).toISOString()
          })
        }
        
        // 计算总流量
        connections.forEach(conn => {
          conn.totalTraffic = conn.totalUpload + conn.totalDownload
        })
        
        // 协议统计
        const protocolStats = [
          { protocol: 'TCP', connectionCount: 15, totalTraffic: 150000000, percentage: 75 },
          { protocol: 'UDP', connectionCount: 5, totalTraffic: 50000000, percentage: 25 }
        ]
        
        // 时间趋势
        const timeTrends = []
        for (let i = 0; i < 24; i++) {
          const timestamp = Math.floor((startTime + i * 3600000) / 1000)
          timeTrends.push({
            timestamp: timestamp,
            timeStr: new Date(timestamp * 1000).toLocaleTimeString(),
            upload: Math.floor(Math.random() * 1000000),
            download: Math.floor(Math.random() * 5000000),
            connections: Math.floor(Math.random() * 10) + 1
          })
        }
        
        // 端口分析
        const portAnalysis = [
          { port: 80, serviceName: 'HTTP', connectionCount: 8, totalTraffic: 80000000, protocols: ['TCP'] },
          { port: 443, serviceName: 'HTTPS', connectionCount: 10, totalTraffic: 100000000, protocols: ['TCP'] },
          { port: 8080, serviceName: 'HTTP-Alt', connectionCount: 2, totalTraffic: 20000000, protocols: ['TCP'] }
        ]
        
        responseData = {
          appInfo: {
            appId: appId,
            name: appInfo.name,
            company: appInfo.company,
            version: appInfo.version,
            path: `C:\\Program Files\\${appInfo.company}\\${appInfo.name}\\app.exe`,
            icon: '',
            hash: 'mock_hash_' + appId
          },
          summary: {
            timeRange: requestParams?.timeRange || '1hour',
            startTime: new Date(startTime).toISOString(),
            endTime: new Date(now).toISOString(),
            totalUpload: connections.reduce((sum, conn) => sum + conn.totalUpload, 0),
            totalDownload: connections.reduce((sum, conn) => sum + conn.totalDownload, 0),
            totalTraffic: connections.reduce((sum, conn) => sum + conn.totalTraffic, 0),
            totalConnections: connections.length,
            uniqueRemoteIPs: new Set(connections.map(conn => conn.remoteIP)).size,
            uniqueRemotePorts: new Set(connections.map(conn => conn.remotePort)).size
          },
          topConnections: connections,
          protocolStats: protocolStats,
          timeTrends: timeTrends,
          portAnalysis: portAnalysis
        }
      } else if (url.includes('/statistics')) {
        responseData = {
          timestamp: Date.now(),
          totalConnections: Math.floor(Math.random() * 100) + 50,
          activeConnections: Math.floor(Math.random() * 50) + 20,
          totalBandwidth: Math.floor(Math.random() * 1000000) + 100000,
          currentBandwidth: Math.floor(Math.random() * 100000) + 10000
        }
      } else if (url.includes('/unsubscribe')) {
        // 取消订阅请求
        responseData = {
          message: '演示模式：取消订阅成功',
          success: true,
          timestamp: Date.now()
        }
      } else {
        // 根据HTTP方法提供不同的响应
        if (method === 'POST') {
          responseData = {
            message: '演示模式：创建成功',
            id: Date.now(),
            timestamp: Date.now()
          }
        } else if (method === 'PUT' || method === 'PATCH') {
          responseData = {
            message: '演示模式：更新成功',
            timestamp: Date.now()
          }
        } else if (method === 'DELETE') {
          responseData = {
            message: '演示模式：删除成功',
            timestamp: Date.now()
          }
        } else {
          responseData = {
            message: '演示模式：操作成功',
            timestamp: Date.now()
          }
        }
      }

      return {
        success: true,
        code: 200,
        message: '请求成功',
        data: responseData as T,
        timestamp: new Date().toISOString()
      }
    } catch (error) {
      console.error('[HttpClient] 演示模式请求处理错误:', error)
      return {
        success: false,
        code: 500,
        message: '演示模式：请求处理失败',
        data: null as T,
        timestamp: new Date().toISOString()
      }
    }
  }

  /**
   * 通用请求方法
   */
  async request<T = unknown>(config: RequestConfig): Promise<ApiResponse<T>> {
    // 演示模式下使用模拟数据
    if (this.isDemoMode) {
      return this.handleDemoRequest<T>(
        config.url || '',
        config.method?.toUpperCase() || 'GET',
        config.data,
        config.params
      )
    }

    try {
      const response = await this.axiosInstance.request<ApiResponse<T>>(config)
      return response.data
    } catch (error) {
      throw error
    }
  }

  /**
   * GET请求
   */
  async get<T = unknown>(
    url: string,
    params?: unknown,
    config?: AxiosRequestConfig,
  ): Promise<ApiResponse<T>> {
    return this.request<T>({
      url,
      method: 'GET',
      params,
      ...config,
    })
  }

  /**
   * POST请求
   */
  async post<T = unknown>(
    url: string,
    data?: unknown,
    config?: AxiosRequestConfig,
  ): Promise<ApiResponse<T>> {
    return this.request<T>({
      url,
      method: 'POST',
      data,
      ...config,
    })
  }

  /**
   * PUT请求
   */
  async put<T = unknown>(
    url: string,
    data?: unknown,
    config?: AxiosRequestConfig,
  ): Promise<ApiResponse<T>> {
    return this.request<T>({
      url,
      method: 'PUT',
      data,
      ...config,
    })
  }

  /**
   * DELETE请求
   */
  async delete<T = unknown>(
    url: string,
    params?: unknown,
    config?: AxiosRequestConfig,
  ): Promise<ApiResponse<T>> {
    return this.request<T>({
      url,
      method: 'DELETE',
      params,
      ...config,
    })
  }

  /**
   * PATCH请求
   */
  async patch<T = unknown>(
    url: string,
    data?: unknown,
    config?: AxiosRequestConfig,
  ): Promise<ApiResponse<T>> {
    return this.request<T>({
      url,
      method: 'PATCH',
      data,
      ...config,
    })
  }

  /**
   * 取消所有请求
   */
  cancelAllRequests(): void {
    this.pending.forEach((controller) => {
      controller.abort()
    })
    this.pending.clear()
  }

  /**
   * 获取Axios实例
   */
  getAxiosInstance(): AxiosInstance {
    return this.axiosInstance
  }

  /**
   * 获取演示模式状态
   */
  isDemoModeEnabled(): boolean {
    return this.isDemoMode
  }

  /**
   * 获取HTTP客户端信息
   */
  getClientInfo() {
    return {
      isDemoMode: this.isDemoMode,
      environmentInfo: environmentDetector.getEnvironmentInfo(),
      pendingRequests: this.pending.size,
      baseURL: this.axiosInstance.defaults.baseURL,
      timeout: this.axiosInstance.defaults.timeout
    }
  }

  /**
   * 强制切换演示模式（用于测试）
   */
  setDemoMode(enabled: boolean): void {
    console.log(`[HttpClient] 切换演示模式: ${enabled}`)
    this.isDemoMode = enabled
  }
}

export const httpClient = new HttpClient()
