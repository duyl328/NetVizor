import BaseBridge from "./BaseBridge";
/**
 * Tauri适配器
 */
class TauriBridge extends BaseBridge {
  constructor() {
    super();
    // Tauri初始化代码
    if (typeof window !== 'undefined' && window.__TAURI__) {
      // 设置Tauri事件监听
      window.__TAURI__.event.listen('tauri://event', (event: { payload: unknown }) => {
        const eventData = event.payload as { channel: string; payload: MessagePayload };
        if (eventData.channel) {
          this.dispatchEvent(eventData.channel, eventData.payload);
        }
      });
    }
  }

  async send<TResponse = void, TRequest extends MessagePayload = MessagePayload>(
    channel: string,
    data?: TRequest
  ): Promise<TResponse> {
    if (typeof window === 'undefined' || !window.__TAURI__) {
      throw new BridgeError('Tauri bridge not available', 'BRIDGE_UNAVAILABLE');
    }

    try {
      // 使用Tauri的invoke函数
      return await window.__TAURI__.invoke<TResponse>(channel, data || {});
    } catch (error) {
      if (error instanceof Error) {
        throw new BridgeError(`Failed to invoke Tauri command: ${error.message}`, 'INVOKE_FAILED');
      }
      throw new BridgeError('Unknown error when invoking Tauri command', 'UNKNOWN_ERROR');
    }
  }

  override on<TData extends MessagePayload = MessagePayload>(
    channel: string,
    listener: (data: TData) => void
  ): () => void {
    const unsubscribe = super.on(channel, listener);

    // 向Tauri注册事件监听
    if (typeof window !== 'undefined' && window.__TAURI__) {
      window.__TAURI__.event.listen(channel, (event: { payload: unknown }) => {
        listener(event.payload as TData);
      }).catch(console.error);
    }

    return unsubscribe;
  }

  override off(channel: string): void {
    super.off(channel);
    // Tauri的事件注销需要特殊处理
    if (typeof window !== 'undefined' && window.__TAURI__) {
      window.__TAURI__.event.unlisten(channel).catch(console.error);
    }
  }
}
