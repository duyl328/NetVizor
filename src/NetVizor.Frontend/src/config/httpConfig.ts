/**
 * Time:2025/6/15 20:13 49
 * Name:httpConfig.ts
 * Path:src/config
 * ProjectName:argus-front
 * Author:charlatans
 *
 *  Il n'ya qu'un héroïsme au monde :
 *     c'est de voir le monde tel qu'il est et de l'aimer.
 */
import type { HttpConfig } from '@/types/http.ts'

// 2. 配置管理器 (config/httpConfig.ts)
class HttpConfigManager {
  private config: HttpConfig = {
    baseURL: import.meta.env.VITE_APP_API_URL || 'http://localhost:8726',
    timeout: 10000,
    headers: {
      'Content-Type': 'application/json'
    }
  };

  /**
   * 设置配置
   */
  setConfig(config: Partial<HttpConfig>): void {
    this.config = { ...this.config, ...config };
  }

  /**
   * 获取配置
   */
  getConfig(): HttpConfig {
    return { ...this.config };
  }

  /**
   * 设置基础URL
   */
  setBaseURL(baseURL: string): void {
    this.config.baseURL = baseURL;
  }

  /**
   * 设置端口
   */
  setPort(host: string, port: number): void {
    const protocol = this.config.baseURL.startsWith('https') ? 'https' : 'http';
    this.config.baseURL = `${protocol}://${host}:${port}`;
  }

  /**
   * 设置超时时间
   */
  setTimeout(timeout: number): void {
    this.config.timeout = timeout;
  }

  /**
   * 设置默认请求头
   */
  setHeaders(headers: Record<string, string>): void {
    this.config.headers = { ...this.config.headers, ...headers };
  }
}

export const httpConfig = new HttpConfigManager();
