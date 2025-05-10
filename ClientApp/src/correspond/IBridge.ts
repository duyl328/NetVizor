/**
 * Time:2025/5/10 13:31 41
 * Name:IBridge.ts
 * Path:src/correspond
 * ProjectName:ClientApp
 * Author:charlatans
 *
 *  Il n'ya qu'un héroïsme au monde :
 *     c'est de voir le monde tel qu'il est et de l'aimer.
 */
/**
 * 消息类型定义 - 用于类型安全的通信
 */
export type MessagePayload = Record<string, unknown>;

/**
 * 核心接口层 - 定义与后端通信的基本能力
 */
export interface IBridge {
  // 发送消息到后端
  send<TResponse = void, TRequest extends MessagePayload = MessagePayload>(
    channel: string,
    data?: TRequest
  ): Promise<TResponse>;

  // 监听后端消息
  on<TData extends MessagePayload = MessagePayload>(
    channel: string,
    listener: (data: TData) => void
  ): () => void;

  // 只监听一次后端消息
  once<TData extends MessagePayload = MessagePayload>(
    channel: string,
    listener: (data: TData) => void
  ): () => void;

  // 移除指定通道的所有监听器
  off(channel: string): void;

  // 移除特定监听器
  removeListener<TData extends MessagePayload = MessagePayload>(
    channel: string,
    listener: (data: TData) => void
  ): void;
}

/**
 * 定义Window扩展，用于与原生平台交互
 */
interface CSharpAppInterface {
  sendToCSharp(channel: string, dataJson: string): string;
}

interface TauriInterface {
  invoke<T>(cmd: string, args?: unknown): Promise<T>;
  event: {
    listen(event: string, handler: (event: { payload: unknown }) => void): Promise<() => void>;
    once(event: string, handler: (event: { payload: unknown }) => void): Promise<void>;
    unlisten(event: string): Promise<void>;
  };
}

interface WindowWithNative extends Window {
  csharpApp?: CSharpAppInterface;
  receiveCSharpMessage?: (channel: string, dataJson: string) => void;
  __TAURI__?: TauriInterface;
}

declare let window: WindowWithNative;

/**
 * 异常类型定义
 */
export class BridgeError extends Error {
  constructor(message: string, public readonly code?: string) {
    super(message);
    this.name = 'BridgeError';
  }
}
