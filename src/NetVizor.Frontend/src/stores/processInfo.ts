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

  function subscribe() {
    console.log('[ProcessStore] 订阅进程信息')

    // 检查演示模式
    if (environmentDetector.shouldUseMockData()) {
      console.log('[ProcessStore] 演示模式：使用模拟数据')
      loadDemoData()
      startDemoDataUpdates()
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
      console.log('[ProcessStore] 加载演示数据:', processes.length, '个进程')
      processInfos.value = processes
    } catch (error) {
      console.error('[ProcessStore] 加载演示数据失败:', error)
    }
  }

  // 启动演示数据更新
  function startDemoDataUpdates() {
    const subscriptionId = dataSourceAdapter.subscribeRealtimeData((data) => {
      if (data.type === 'process_update' && data.data) {
        // 随机更新一个进程的数据
        const processData = data.data as ProcessType
        const index = processInfos.value.findIndex(p => p.processId === processData.processId)
        if (index !== -1) {
          processInfos.value[index] = processData
        } else {
          // 如果进程不存在，可能是新进程，添加到列表
          processInfos.value.push(processData)
        }
        console.log('[ProcessStore] 实时更新进程数据:', processData.processName)
      }
    }, 3000) // 每3秒更新一次

    console.log('[ProcessStore] 演示模式实时更新已启动:', subscriptionId)
  }

  return {
    subscribe,processInfos,clear
  }
})
