import './assets/main.css'
import { createApp } from 'vue'
import { createPinia } from 'pinia'

import App from './App.vue'
import router from './router'

import CSharpBridgeV2 from '@/correspond/CSharpBridgeV2'
import websocketPlugin from '@/plugins/websocketPlugin'
import { useWebSocketStore } from '@/stores/websocketStore'

const app = createApp(App)

app.use(createPinia())
app.use(router)

app.mount('#app')
app.use(websocketPlugin)

// 注册 C# 函数
window.externalFunctions = {}

// 监听获取 WebSocket 链接
const webSocketStore = useWebSocketStore()

// 监听WebSocket地址
const bridge = CSharpBridgeV2?.getBridge()

const intervalId = setInterval(() => {
  if (webSocketStore.isInitialized) {
    clearInterval(intervalId)
    return
  }

  bridge.send('GetWebSocketPath', {}, (data) => {
    webSocketStore.initialize(data)
    console.log(data, '======')
  })
}, 500)
