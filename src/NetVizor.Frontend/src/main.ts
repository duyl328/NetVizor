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
// src\views\AnalyseView\AnalyseView.vue 中主要进行数据分析页面，而views\AnalyseView\components\SoftwareDetailModal.vue中可以获取到对应软件的AppId和时间信息，我们后端准备了
// /apps/network-analysis 接口，通过Get调用，这是我的测试用http://localhost:8268/api/apps/network-analysis?appId=0e1b50411d632b2c，目前没问题的，返回的信息格式
//   在C:\Users\charlatans\AppData\Roaming\JetBrains\WebStorm2025.1\scratches\scratch.json中有详细展示，你可以根据其定义内容以及你页面的展示方式，对于IP和端口信息我想使用
// Echarts中的graph-circular-layout来进行呈现。并且我的数据也很详细，针对软件自身的信息，以及网络信息可以有很多展示的内容，并且这个信息我更多的是希望用图标，比如Echarts来进行展示。
// 你帮我设计展示方式让其现代、优雅
