import type { IBridge, LogParam, MessagePayload } from './IBridge'

export class CSharpBridge implements IBridge {
  private eventListeners = new Map<string, Set<(...args: any[]) => void>>()

  log(param: LogParam): void {
    console.log(`[${param.level.toUpperCase()}] ${param.message}`, param.data)
  }

  async invokeMethod(methodName: string, ...args: any[]): Promise<any> {
    // 默认实现
    console.warn(`CSharpBridge.invokeMethod not implemented: ${methodName}`, args)
    return Promise.resolve(null)
  }

  addEventListener(eventName: string, callback: (...args: any[]) => void): void {
    if (!this.eventListeners.has(eventName)) {
      this.eventListeners.set(eventName, new Set())
    }
    this.eventListeners.get(eventName)!.add(callback)
  }

  removeEventListener(eventName: string, callback: (...args: any[]) => void): void {
    const listeners = this.eventListeners.get(eventName)
    if (listeners) {
      listeners.delete(callback)
    }
  }

  protected emit(eventName: string, ...args: any[]): void {
    const listeners = this.eventListeners.get(eventName)
    if (listeners) {
      listeners.forEach(callback => callback(...args))
    }
  }
}