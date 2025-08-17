import CSharpBridgeV2 from '@/correspond/CSharpBridgeV2'
import websocketPlugin from '@/plugins/websocketPlugin'
import { httpConfig } from '@/config/httpConfig'
import { environmentDetector } from '@/utils/environmentDetector'

// 注册 C# 函数
window.externalFunctions = {}

import './assets/main.css'
import { createApp } from 'vue'
import { createPinia } from 'pinia'

import App from './App.vue'
import router from './router'

import { createNaiveUI } from './naive'
import { useThemeStore } from '@/stores/theme'
import { useUuidStore } from '@/stores/uuidStore'
import { logB } from '@/utils/logHelper/logUtils'

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

// 监听获取 WebSocket 链接 - 需要在 Pinia 初始化后进行
import { useWebSocketStore } from '@/stores/websocketStore'

// 初始化WebSocket连接
const initializeWebSocket = () => {
  const webSocketStore = useWebSocketStore()

  console.log('[Main] 初始化WebSocket连接')
  console.log('[Main] 环境信息:', environmentDetector.getEnvironmentInfo())

  // 检查是否为演示模式
  if (environmentDetector.shouldUseMockData()) {
    console.log('[Main] 演示模式：跳过WebSocket初始化')
    return
  }

  const bridge = CSharpBridgeV2?.getBridge()
  if (bridge) {
    console.log('[Main] 检测到WebView2环境，使用桥接获取WebSocket路径')
    const intervalId = setInterval(() => {
      if (webSocketStore.isInitialized) {
        clearInterval(intervalId)
        return
      }
      if (bridge) {
        bridge.send('GetWebSocketPath', {}, (data) => {
          webSocketStore.initialize(data)
          console.log('[Main] WebSocket路径已设置:', data)
        })
        bridge.send('GetHttpPath', {}, (data) => {
          httpConfig.setUrl(data + '/api')
          console.log('[Main] HTTP路径已设置:', data)
        })
      }
    }, 100)
  } else {
    console.log('[Main] 开发环境：使用默认WebSocket路径')
    // 开发阶段直接使用固定节点
    webSocketStore.initialize('ws://127.0.0.1:8267')
  }
}

// 延迟初始化，确保所有store都已准备好
setTimeout(initializeWebSocket, 100)

// 演示模式测试工具
if (environmentDetector.shouldUseMockData()) {
  import('@/utils/demoModeTest').then(({ demoModeTest }) => {
    console.log('[Main] 演示模式测试工具已加载')
    console.log('[Main] 在控制台运行 testDemoMode() 开始测试')
  }).catch(error => {
    console.warn('[Main] 演示模式测试工具加载失败:', error)
  })
}
