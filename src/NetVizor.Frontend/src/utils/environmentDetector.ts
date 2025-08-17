/**
 * 环境检测工具类
 * 用于检测当前运行环境并决定数据源类型
 */

export enum DataSourceType {
  REAL_DATA = 'REAL_DATA',     // 真实数据 (WebView2生产环境)
  MOCK_DATA = 'MOCK_DATA',     // 模拟数据 (演示模式)
  DEV_API = 'DEV_API'          // 开发API (开发环境)
}

export enum EnvironmentType {
  PRODUCTION = 'PRODUCTION',   // 生产环境 (WebView2)
  DEMO = 'DEMO',              // 演示环境 (GitHub Pages)
  DEVELOPMENT = 'DEVELOPMENT'  // 开发环境
}

class EnvironmentDetector {
  private static instance: EnvironmentDetector | null = null

  private _isWebView2: boolean | null = null
  private _isDemoMode: boolean | null = null
  private _dataSourceType: DataSourceType | null = null
  private _environmentType: EnvironmentType | null = null

  static getInstance(): EnvironmentDetector {
    if (!EnvironmentDetector.instance) {
      EnvironmentDetector.instance = new EnvironmentDetector()
    }
    return EnvironmentDetector.instance
  }

  /**
   * 检测是否运行在WebView2环境中
   */
  isWebView2(): boolean {
    if (this._isWebView2 === null) {
      this._isWebView2 = !!(window.chrome && window.chrome.webview)
      console.log('[EnvironmentDetector] WebView2 detected:', this._isWebView2)
    }
    return this._isWebView2
  }

  /**
   * 检测是否开启了演示模式
   */
  isDemoMode(): boolean {
    if (this._isDemoMode === null) {
      this._isDemoMode = import.meta.env.VITE_DEMO_MODE === 'true'
      console.log('[EnvironmentDetector] Demo mode enabled:', this._isDemoMode)
    }
    return this._isDemoMode
  }

  /**
   * 获取当前环境类型
   */
  getEnvironmentType(): EnvironmentType {
    if (this._environmentType === null) {
      if (this.isWebView2()) {
        this._environmentType = EnvironmentType.PRODUCTION
      } else if (this.isDemoMode()) {
        this._environmentType = EnvironmentType.DEMO
      } else {
        this._environmentType = EnvironmentType.DEVELOPMENT
      }
      console.log('[EnvironmentDetector] Environment type:', this._environmentType)
    }
    return this._environmentType
  }

  /**
   * 获取数据源类型
   */
  getDataSourceType(): DataSourceType {
    if (this._dataSourceType === null) {
      if (this.isWebView2()) {
        // WebView2环境：使用真实数据（桥接通信）
        this._dataSourceType = DataSourceType.REAL_DATA
      } else if (this.isDemoMode()) {
        // 演示模式：使用模拟数据
        this._dataSourceType = DataSourceType.MOCK_DATA
      } else {
        // 开发环境：尝试连接真实API
        this._dataSourceType = DataSourceType.DEV_API
      }
      console.log('[EnvironmentDetector] Data source type:', this._dataSourceType)
    }
    return this._dataSourceType
  }

  /**
   * 是否应该使用模拟数据
   */
  shouldUseMockData(): boolean {
    return this.getDataSourceType() === DataSourceType.MOCK_DATA
  }

  /**
   * 是否应该使用桥接通信
   */
  shouldUseBridge(): boolean {
    return this.getDataSourceType() === DataSourceType.REAL_DATA
  }

  /**
   * 是否应该使用HTTP API
   */
  shouldUseAPI(): boolean {
    return this.getDataSourceType() === DataSourceType.DEV_API
  }

  /**
   * 获取API基础URL
   */
  getApiBaseUrl(): string {
    return import.meta.env.VITE_APP_API_URL || 'http://localhost:8268/api'
  }

  /**
   * 获取环境信息摘要
   */
  getEnvironmentInfo() {
    return {
      isWebView2: this.isWebView2(),
      isDemoMode: this.isDemoMode(),
      environmentType: this.getEnvironmentType(),
      dataSourceType: this.getDataSourceType(),
      apiBaseUrl: this.getApiBaseUrl(),
      shouldUseMockData: this.shouldUseMockData(),
      shouldUseBridge: this.shouldUseBridge(),
      shouldUseAPI: this.shouldUseAPI()
    }
  }

  /**
   * 重置缓存（主要用于测试）
   */
  reset(): void {
    this._isWebView2 = null
    this._isDemoMode = null
    this._dataSourceType = null
    this._environmentType = null
  }
}

// 导出单例实例
export const environmentDetector = EnvironmentDetector.getInstance()

// 导出便捷方法
export const isWebView2 = () => environmentDetector.isWebView2()
export const isDemoMode = () => environmentDetector.isDemoMode()
export const shouldUseMockData = () => environmentDetector.shouldUseMockData()
export const shouldUseBridge = () => environmentDetector.shouldUseBridge()
export const shouldUseAPI = () => environmentDetector.shouldUseAPI()
export const getDataSourceType = () => environmentDetector.getDataSourceType()
export const getEnvironmentType = () => environmentDetector.getEnvironmentType()
export const getEnvironmentInfo = () => environmentDetector.getEnvironmentInfo()

export default environmentDetector
