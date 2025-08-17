import { defineStore, storeToRefs } from 'pinia'
import { ref, Ref, watch } from 'vue'
import { ProcessType } from '@/types/process'
import { useWebSocketStore } from '@/stores/websocketStore'
import { WebSocketResponse } from '@/types/websocket'
import { environmentDetector } from '@/utils/environmentDetector'
import { dataSourceAdapter } from '@/utils/dataSourceAdapter'

export const useProcessStore = defineStore('processInfoSub', () => {
  // 进程信息
  const processInfos: Ref<ProcessType[]> = ref([])

  function clear() {
    processInfos.value = []

  }

  async function subscribe() {
    console.log(`[${new Date().toLocaleTimeString()}] [ProcessStore] 🚀 开始订阅进程信息`)

    // 检查演示模式
    const shouldUseMockData = environmentDetector.shouldUseMockData()
    console.log(`[${new Date().toLocaleTimeString()}] [ProcessStore] 🔍 环境检测结果:`, shouldUseMockData)
    
    if (shouldUseMockData) {
      console.log(`[${new Date().toLocaleTimeString()}] [ProcessStore] 🎭 演示模式：使用模拟数据`)
      try {
        await loadDemoData() // 等待初始数据加载完成
        console.log(`[${new Date().toLocaleTimeString()}] [ProcessStore] ✅ 初始数据加载完成`)
        startDemoDataUpdates()
        console.log(`[${new Date().toLocaleTimeString()}] [ProcessStore] ⚡ 实时更新已启动`)
      } catch (error) {
        console.error(`[${new Date().toLocaleTimeString()}] [ProcessStore] ❌ 演示数据加载失败:`, error)
      }
      return
    }

    // 真实模式：使用WebSocket
    const webSocketStore = useWebSocketStore()
    const { isOpen } = storeToRefs(webSocketStore)
    watch(
      isOpen,
      (oldValue, newValue) => {
        if (oldValue || newValue) {
          webSocketStore.registerHandler('ProcessInfo', (data: WebSocketResponse<string>) => {
            // console.log(data.data)
            const parse: ProcessType[] = JSON.parse(data.data)
            console.log('[ProcessStore] 接收到进程数据:', parse.length, '个进程')
            processInfos.value = parse
          })
        }
      },
      { immediate: true },
    )
  }

  // 加载演示数据
  async function loadDemoData() {
    try {
      const processes = await dataSourceAdapter.getProcessList()
      console.log(`[${new Date().toLocaleTimeString()}] [ProcessStore] 加载演示数据:`, processes.length, '个进程')
      
      // 立即设置数据，不等待
      processInfos.value = processes
      
      // 验证连接数据
      const totalConnections = processes.reduce((sum, p) => sum + (p.connections?.length || 0), 0)
      console.log(`[${new Date().toLocaleTimeString()}] [ProcessStore] 总连接数:`, totalConnections)
    } catch (error) {
      console.error('[ProcessStore] 加载演示数据失败:', error)
    }
  }

  // 启动演示数据更新
  function startDemoDataUpdates() {
    console.log(`[${new Date().toLocaleTimeString()}] [ProcessStore] 🎬 启动实时更新，当前进程数:`, processInfos.value.length)
    
    const subscriptionId = dataSourceAdapter.subscribeRealtimeData((data) => {
      if (data.type === 'process_update' && data.data) {
        // 检查当前数据状态
        console.log(`[${new Date().toLocaleTimeString()}] [ProcessStore] 📊 实时更新前进程数:`, processInfos.value.length)
        
        // 随机更新一个进程的数据
        const processData = data.data as ProcessType
        const index = processInfos.value.findIndex(p => p.processId === processData.processId)
        if (index !== -1) {
          processInfos.value[index] = processData
          console.log(`[${new Date().toLocaleTimeString()}] [ProcessStore] 🔄 更新现有进程:`, processData.processName)
        } else {
          // 如果进程不存在，可能是新进程，添加到列表
          processInfos.value.push(processData)
          console.log(`[${new Date().toLocaleTimeString()}] [ProcessStore] ➕ 添加新进程:`, processData.processName)
        }
        
        console.log(`[${new Date().toLocaleTimeString()}] [ProcessStore] 📊 实时更新后进程数:`, processInfos.value.length)
      }
    }, 3000) // 每3秒更新一次

    console.log(`[${new Date().toLocaleTimeString()}] [ProcessStore] ⚡ 演示模式实时更新已启动:`, subscriptionId)
  }

  return {
    subscribe,processInfos,clear
  }
})
