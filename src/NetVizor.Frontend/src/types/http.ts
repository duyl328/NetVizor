/**
 * HTTP相关类型定义
 */
import type { AxiosRequestConfig } from 'axios'

// HTTP配置接口
export interface HttpConfig {
  baseURL: string
  timeout?: number
  headers?: Record<string, string>
}

// 请求配置接口
export interface RequestConfig extends AxiosRequestConfig {
  skipErrorMessage?: boolean
  skipLoading?: boolean
}

// API响应接口
export interface ApiResponse<T = unknown> {
  success?: boolean
  code?: number
  data?: T
  message?: string
}