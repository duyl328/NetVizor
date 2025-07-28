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
import { useUuidStore } from '@/stores/uuidStore'
import { logB } from '@/utils/logHelper/logUtils'
import { httpConfig } from '@/config/httpConfig'

// Vue Virtual Scroller
import 'vue-virtual-scroller/dist/vue-virtual-scroller.css'
import VueVirtualScroller from 'vue-virtual-scroller'

const app = createApp(App)
app.use(createPinia())
app.use(router)
app.use(websocketPlugin)
app.use(VueVirtualScroller)
// 使用 主题 支持
app.use(createNaiveUI())

// 这行代码会触发 store 的初始化，从而立即应用主题
useThemeStore()

// 获取全局 UUID
const useUuidStore1 = useUuidStore()
logB.success('全局 ID :', useUuidStore1.uuid)
// 设置所有请求头
httpConfig.setConfig({
  headers: {
    uuid: useUuidStore1.uuid,
  },
})

// http 注册
import httpPlugin from './plugins/http'
import { useApplicationStore } from '@/stores/application'

// 注册消息监听
const useApplicationStore1 = useApplicationStore()
useApplicationStore1.subscribe()

// httpPlugin.install(app)

app.use(httpPlugin)

app.mount('#app')

// 注册 C# 函数
window.externalFunctions = {}

// 监听获取 WebSocket 链接
const webSocketStore = useWebSocketStore()

// 监听WebSocket地址
const bridge = CSharpBridgeV2?.getBridge()
if (bridge) {
  const intervalId = setInterval(() => {
    if (webSocketStore.isInitialized) {
      clearInterval(intervalId)
      return
    }
    if (bridge) {
      bridge.send('GetWebSocketPath', {}, (data) => {
        webSocketStore.initialize(data)
        console.log(data, '======')
      })
    }
  }, 500)
} else {
  // todo: 2025/6/25 08:32 开发阶段直接使用固定节点
  webSocketStore.initialize('ws://127.0.0.1:8267')
}
