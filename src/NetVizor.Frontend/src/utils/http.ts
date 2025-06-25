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

class HttpClient {
  private axiosInstance: AxiosInstance
  private pending: Map<string, AbortController> = new Map()

  constructor() {
    this.axiosInstance = axios.create()
    this.setupInterceptors()
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
   * 通用请求方法
   */
  async request<T = unknown>(config: RequestConfig): Promise<ApiResponse<T>> {
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
}

export const httpClient = new HttpClient()
