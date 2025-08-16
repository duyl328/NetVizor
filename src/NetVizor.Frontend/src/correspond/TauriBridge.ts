import { CSharpBridge } from './CSharpBridge'
import type { LogParam } from './IBridge'

export class TauriBridge extends CSharpBridge {
  async invokeMethod(methodName: string, ...args: any[]): Promise<any> {
    try {
      // 如果运行在 Tauri 环境中
      if (window.__TAURI__) {
        const { invoke } = window.__TAURI__.tauri
        return await invoke(methodName, ...args)
      }
      
      // 否则使用父类的默认实现
      return super.invokeMethod(methodName, ...args)
    } catch (error) {
      console.error(`TauriBridge.invokeMethod error:`, error)
      throw error
    }
  }

  log(param: LogParam): void {
    // 在 Tauri 环境中，可以使用更高级的日志记录
    super.log(param)
  }
}

// 扩展 Window 接口
declare global {
  interface Window {
    __TAURI__?: {
      tauri: {
        invoke: (cmd: string, ...args: any[]) => Promise<any>
      }
    }
  }
}