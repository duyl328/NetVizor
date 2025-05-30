import { App } from 'vue';
import { useWebSocketStore } from '@/stores/websocketStore';

export default {
  install: (app: App) => {
    const store = useWebSocketStore();

    // 提供全局访问
    app.config.globalProperties.$websocket = {
      send: store.send,
      subscribe: store.subscribe,
      unsubscribe: store.unsubscribe,
      registerHandler: store.registerHandler,
      unregisterHandler: store.unregisterHandler,
      isConnected: () => store.isConnected
    };

    // 注入到组合式API
    app.provide('websocket', {
      send: store.send,
      subscribe: store.subscribe,
      unsubscribe: store.unsubscribe,
      registerHandler: store.registerHandler,
      unregisterHandler: store.unregisterHandler,
      isConnected: store.isConnected
    });
  }
};
