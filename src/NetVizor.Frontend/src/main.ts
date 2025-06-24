// 引入主题变量（放在其他样式之前）
import './assets/theme-variables.css'
import './assets/main.css'
import { createApp } from 'vue'
import { createPinia } from 'pinia'

import App from './App.vue'
import router from './router'

import CSharpBridgeV2 from '@/correspond/CSharpBridgeV2'
import websocketPlugin from '@/plugins/websocketPlugin'
import { useWebSocketStore } from '@/stores/websocketStore'
import { createNaiveUI } from './naive'
import { useThemeStore } from '@/stores/theme'

const app = createApp(App)
app.use(createPinia())
app.use(router)
app.use(websocketPlugin)
// 使用 主题 支持
app.use(createNaiveUI())

// 这行代码会触发 store 的初始化，从而立即应用主题
useThemeStore()

app.mount('#app')

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
  if (bridge){
    bridge.send('GetWebSocketPath', {}, (data) => {
      webSocketStore.initialize(data)
      console.log(data, '======')
    })
  }
}, 500)
