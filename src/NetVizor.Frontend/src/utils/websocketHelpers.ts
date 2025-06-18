/**
 * Time:2025/5/25 20:41 47
 * Name:websocketHelpers.ts
 * Path:src/utils
 * ProjectName:ClientApp
 * Author:charlatans
 *
 *  Il n'ya qu'un héroïsme au monde :
 *     c'est de voir le monde tel qu'il est et de l'aimer.
 */


// utils/websocketHelpers.ts - WebSocket辅助工具
export class WebSocketRetryManager {
  private retryAttempts = 0;
  private maxRetries: number;
  private baseDelay: number;
  private maxDelay: number;

  constructor(maxRetries = 5, baseDelay = 1000, maxDelay = 30000) {
    this.maxRetries = maxRetries;
    this.baseDelay = baseDelay;
    this.maxDelay = maxDelay;
  }

  shouldRetry(): boolean {
    return this.retryAttempts < this.maxRetries;
  }

  getNextDelay(): number {
    const delay = Math.min(this.baseDelay * Math.pow(2, this.retryAttempts), this.maxDelay);
    this.retryAttempts++;
    return delay;
  }

  reset() {
    this.retryAttempts = 0;
  }

  getCurrentAttempt(): number {
    return this.retryAttempts;
  }
}

export class WebSocketMessageQueue {
  private queue: Array<{ message: unknown; resolve: Function; reject: Function }> = [];
  private isProcessing = false;

  async enqueue(message: unknown): Promise<void> {
    return new Promise((resolve, reject) => {
      this.queue.push({ message, resolve, reject });
      this.processQueue();
    });
  }

  private async processQueue() {
    if (this.isProcessing || this.queue.length === 0) return;

    this.isProcessing = true;

    while (this.queue.length > 0) {
      const item = this.queue.shift();
      if (item) {
        try {
          // 这里应该调用实际的发送方法
          await this.sendMessage(item.message);
          item.resolve();
        } catch (error) {
          item.reject(error);
        }
      }
    }

    this.isProcessing = false;
  }

  private async sendMessage(message: unknown): Promise<void> {
    // 由WebSocketManager实现
    throw new Error('sendMessage method should be implemented by WebSocketManager');
  }

  clear() {
    this.queue.forEach(item => item.reject(new Error('Queue cleared')));
    this.queue = [];
  }
}
