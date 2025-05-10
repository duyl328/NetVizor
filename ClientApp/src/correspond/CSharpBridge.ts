import BaseBridge from "./BaseBridge"
/**
 * C#套壳浏览器的适配器
 */
class CSharpBridge extends BaseBridge {
  constructor() {
    super();
    // 假设C#注入了window.csharpApp对象
    if (typeof window !== 'undefined' && window.csharpApp) {
      // 监听C#发来的消息
      window.receiveCSharpMessage = (channel: string, dataJson: string) => {
        try {
          const data = JSON.parse(dataJson) as MessagePayload;
          this.dispatchEvent(channel, data);
        } catch (error) {
          console.error('Failed to parse message from C#:', error);
        }
      };
    }
  }

  async send<TResponse = void, TRequest extends MessagePayload = MessagePayload>(
    channel: string,
    data?: TRequest
  ): Promise<TResponse> {
    if (typeof window === 'undefined' || !window.csharpApp) {
      throw new BridgeError('CSharp bridge not available', 'BRIDGE_UNAVAILABLE');
    }

    try {
      // 假设C#提供了sendToCSharp方法
      const responseJson = window.csharpApp.sendToCSharp(
        channel,
        JSON.stringify(data || {})
      );
      return JSON.parse(responseJson) as TResponse;
    } catch (error) {
      if (error instanceof Error) {
        throw new BridgeError(`Failed to send message to C#: ${error.message}`, 'SEND_FAILED');
      }
      throw new BridgeError('Unknown error when sending message to C#', 'UNKNOWN_ERROR');
    }
  }
}
