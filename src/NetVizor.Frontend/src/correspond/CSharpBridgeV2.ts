import type { MessagePayload } from '@/correspond/IBridge'
import { CSharpBridge } from '@/correspond/CSharpBridge'
import { TauriBridge } from '@/correspond/TauriBridge'
import { environmentDetector } from '@/utils/environmentDetector'

interface BridgeMessagePayload<T = any> {
  channel: string
  payload: T
}

class CSharpBridgeV2 {
  private static CSBridge: CSharpBridgeV2 | null = null

  static getBridge(): CSharpBridgeV2 | null {
    if (CSharpBridgeV2.CSBridge === null) {
      if (window.chrome && window.chrome.webview) {
        // window.chrome.webview.postMessage("hello");
        CSharpBridgeV2.CSBridge = new CSharpBridgeV2()
        ;(window as any).externalFunctions = (window as any).externalFunctions || {}
        ;(window as any).externalFunctions.__BRIDGE_LISTEN__ = CSharpBridgeV2.CSBridge.bridgeInvoke
        ;(window as any).externalFunctions.__BRIDGE_LISTEN__FUNCTIONS__ = new Map()
      } else {
        // 只在非演示模式下显示此警告
        if (!environmentDetector.shouldUseMockData()) {
          console.warn('[CSharpBridgeV2] WebView2环境未检测到，运行在浏览器模式')
        }
      }
    }
    return CSharpBridgeV2.CSBridge
  }

  bridgeInvoke(channel: string, json: string) {
    const bridgeListenFunctions = (window as any).externalFunctions?.__BRIDGE_LISTEN__FUNCTIONS__
    const listeners = bridgeListenFunctions.get(channel)
    if (listeners) {
      listeners.forEach((listener: any) => {
        try {
          listener.callback(json)
          if (listener.once && CSharpBridgeV2.CSBridge) {
            CSharpBridgeV2.CSBridge.removeListen(channel, listener)
          }
        } catch (error) {
          console.error(`Error in listener for channel ${channel}:`, error)
        }
      })
    }
  }

  // 发送消息到后端
  send<T, V>(channel: string, data: T | null = null, callback: ((data: V) => void) | null = null) {
    const obj: BridgeMessagePayload<T> = {
      channel,
      payload: data,
    }
    if (callback !== null && callback !== undefined) {
      this.listen(channel, callback, true)
    }
    if (window.chrome?.webview) {
      window.chrome.webview.postMessage(obj)
    }
  }

  /**
   * 监听后端消息
   */
  listen<V>(channel: string, callback: (data: V) => void, once: boolean = false) {
    const bridgeListenFunctions = (window as any).externalFunctions?.__BRIDGE_LISTEN__FUNCTIONS__
    if (!bridgeListenFunctions?.has(channel)) {
      bridgeListenFunctions.set(channel, new Set())
    }

    bridgeListenFunctions.get(channel)!.add({
      callback: callback,
      once: once,
    })
  }

  // 移除指定通道的所有监听器
  off(channel: string) {
    ;(window as any).externalFunctions?.__BRIDGE_LISTEN__FUNCTIONS__?.delete(channel)
  }

  // 移除特定监听器
  removeListen(channel: string, listener: unknown) {
    const bridgeListenFunctions = (window as any).externalFunctions?.__BRIDGE_LISTEN__FUNCTIONS__
    const channelListeners = bridgeListenFunctions.get(channel)
    if (channelListeners) {
      // 需要类型转换以匹配存储的监听器类型
      channelListeners.delete(listener)
      if (channelListeners.size === 0) {
        bridgeListenFunctions.delete(channel)
      }
    }
  }

}

export default CSharpBridgeV2

// 扩展 Window 接口
declare global {
  interface Window {
    chrome?: {
      webview?: {
        postMessage: (data: any) => void
      }
    }
  }
}
