import IBridge from "./IBridge";
/**
 * 业务层类型定义
 */
export interface UserInfo {
  id: string;
  name: string;
  email?: string;
  role: string;
  permissions: string[];
}

export interface Notification {
  id: string;
  title: string;
  message: string;
  type: 'info' | 'warning' | 'error' | 'success';
  timestamp: number;
}

export type LogLevel = 'debug' | 'info' | 'warn' | 'error';

export interface LogPayload {
  level: LogLevel;
  message: string;
  context?: Record<string, unknown>;
}

/**
 * 业务功能层 - 提供更高级的通信功能
 */
export class AppBridge {
  private bridge: IBridge;

  constructor() {
    this.bridge = BridgeFactory.create();
  }

  // 获取用户信息
  async getUserInfo(): Promise<UserInfo> {
    return this.bridge.send<UserInfo>('user.getInfo');
  }

  // 监听系统通知
  onNotification(listener: (notification: Notification) => void): () => void {
    return this.bridge.on<Notification>('system.notification', listener);
  }

  // 发送日志
  async sendLog(payload: LogPayload): Promise<void> {
    return this.bridge.send<void, LogPayload>('system.log', payload);
  }

  // 获取系统配置
  async getConfig<T extends Record<string, unknown>>(key: string): Promise<T> {
    return this.bridge.send<T, { key: string }>('system.getConfig', { key });
  }

  // 保存系统配置
  async saveConfig<T extends Record<string, unknown>>(key: string, value: T): Promise<void> {
    return this.bridge.send<void, { key: string; value: T }>('system.saveConfig', { key, value });
  }
}

// 创建单例实例
export const appBridge = new AppBridge();
