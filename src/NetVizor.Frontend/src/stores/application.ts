import { ref, Ref, watch } from 'vue'
import { defineStore, storeToRefs } from 'pinia'
import { useWebSocketStore } from '@/stores/websocketStore'
import { WebSocketResponse } from '@/types/websocket'
import { ApplicationType } from '@/types/infoModel'

export const useApplicationStore = defineStore('applicationInfoSub', () => {
  const appInfos: Ref<ApplicationType[]> = ref([])

  /**
   * 订阅信息
   */
  function subscribe() {
    const webSocketStore = useWebSocketStore()
    const { isOpen } = storeToRefs(webSocketStore)
    watch(
      isOpen,
      (oldValue, newValue) => {
        if (oldValue || newValue) {
          webSocketStore.registerHandler('ApplicationInfo', (data: WebSocketResponse) => {
            appInfos.value = JSON.parse(data.data)
          })
        }
      },
      { immediate: true },
    )
  }

  return { appInfos, subscribe }
})
