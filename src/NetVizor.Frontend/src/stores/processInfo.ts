import { defineStore, storeToRefs } from 'pinia'
import { ref, Ref, watch } from 'vue'
import { ProcessType } from '@/types/process'
import { useWebSocketStore } from '@/stores/websocketStore'
import { WebSocketResponse } from '@/types/websocket'

export const useProcessStore = defineStore('processInfoSub', () => {
  // 进程信息
  const processInfos: Ref<ProcessType[]> = ref([])

  function clear() {
    processInfos.value = []

  }

  function subscribe() {
    const webSocketStore = useWebSocketStore()
    const { isOpen } = storeToRefs(webSocketStore)
    watch(
      isOpen,
      (oldValue, newValue) => {
        if (oldValue || newValue) {
          webSocketStore.registerHandler('ProcessInfo', (data: WebSocketResponse<string>) => {
            // console.log(data.data)
            const parse: ProcessType[] = JSON.parse(data.data)
            console.log(parse);
            processInfos.value = parse
          })
        }
      },
      { immediate: true },
    )
  }

  return {
    subscribe,processInfos,clear
  }
})
