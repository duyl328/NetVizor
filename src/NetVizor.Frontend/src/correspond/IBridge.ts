// 桥接接口定义
export interface IBridge {
  log(param: LogParam): void
  invokeMethod(methodName: string, ...args: any[]): Promise<any>
  addEventListener(eventName: string, callback: (...args: any[]) => void): void
  removeEventListener(eventName: string, callback: (...args: any[]) => void): void
}

export interface LogParam {
  level: 'info' | 'warn' | 'error' | 'debug'
  message: string
  data?: any
}

export interface MessagePayload<T = any> {
  type: string
  data: T
  timestamp?: number
}