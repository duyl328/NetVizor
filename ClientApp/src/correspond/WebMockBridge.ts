import BaseBridge from "./BaseBridge";
/**
 * Web环境的模拟实现 - 用于开发调试
 */
class WebMockBridge extends BaseBridge {
  async send<TResponse = void, TRequest extends MessagePayload = MessagePayload>(
    channel: string,
    data?: TRequest
  ): Promise<TResponse> {
    console.log(`[WebMock] Send to channel ${channel}:`, data);
    // 模拟异步响应
    return new Promise<TResponse>(resolve => {
      setTimeout(() => {
        // 默认的模拟响应
        const mockResponse = { success: true, mock: true } as unknown as TResponse;
        resolve(mockResponse);
      }, 100);
    });
  }

  // 用于测试 - 模拟后端发送消息
  mockReceive<TData extends MessagePayload = MessagePayload>(channel: string, data: TData): void {
    this.dispatchEvent(channel, data);
  }
}
