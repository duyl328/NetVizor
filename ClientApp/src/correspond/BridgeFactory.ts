import CSharpBridge from "./CSharpBridge";
import TauriBridge from "./TauriBridge";
/**
 * 桥梁工厂 - 根据运行环境创建适当的桥梁实例
 */
export class BridgeFactory {
  static create(): IBridge {
    if (typeof window !== 'undefined') {
      // 根据环境判断使用哪个桥接实现
      if (window.csharpApp) {
        return new CSharpBridge();
      } else if (window.__TAURI__) {
        return new TauriBridge();
      }
    }

    // 默认返回一个Web模拟实现
    return new WebMockBridge();
  }
}
