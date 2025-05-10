/**
 * Time:2025/5/10 13:42 55
 * Name:BaseBridge.ts
 * Path:src/correspond
 * ProjectName:ClientApp
 * Author:charlatans
 *
 *  Il n'ya qu'un héroïsme au monde :
 *     c'est de voir le monde tel qu'il est et de l'aimer.
 */

import type MessagePayload from "./IBridge"
import { IBridge } from '@/correspond/IBridge'

/**
 * 平台适配层基类
 */
abstract class BaseBridge implements IBridge {
  protected listeners: Map<string, Set<(data: unknown) => void>> = new Map();

  abstract send<TResponse = void, TRequest extends MessagePayload = MessagePayload>(
    channel: string,
    data?: TRequest
  ): Promise<TResponse>;

  on<TData extends MessagePayload = MessagePayload>(
    channel: string,
    listener: (data: TData) => void
  ): () => void {
    if (!this.listeners.has(channel)) {
      this.listeners.set(channel, new Set());
    }

    // TypeScript 不允许将具体类型的函数分配给更通用的类型，因此这里需要类型转换
    const typedListener = listener as (data: unknown) => void;
    this.listeners.get(channel)!.add(typedListener);

    return () => this.removeListener(channel, listener);
  }

  once<TData extends MessagePayload = MessagePayload>(
    channel: string,
    listener: (data: TData) => void
  ): () => void {
    // 创建一个一次性监听器
    const onceListener = ((data: TData) => {
      listener(data);
      this.removeListener(channel, onceListener);
    });

    return this.on(channel, onceListener);
  }

  off(channel: string): void {
    this.listeners.delete(channel);
  }

  removeListener<TData extends MessagePayload = MessagePayload>(
    channel: string,
    listener: (data: TData) => void
  ): void {
    const channelListeners = this.listeners.get(channel);
    if (channelListeners) {
      // 需要类型转换以匹配存储的监听器类型
      const typedListener = listener as (data: unknown) => void;
      channelListeners.delete(typedListener);
      if (channelListeners.size === 0) {
        this.listeners.delete(channel);
      }
    }
  }

  protected dispatchEvent<TData extends MessagePayload = MessagePayload>(
    channel: string,
    data: TData
  ): void {
    const listeners = this.listeners.get(channel);
    if (listeners) {
      listeners.forEach(listener => {
        try {
          listener(data);
        } catch (error) {
          console.error(`Error in listener for channel ${channel}:`, error);
        }
      });
    }
  }
}
