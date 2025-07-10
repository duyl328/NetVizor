import { ref, Ref, watch, computed } from 'vue'
import { defineStore, storeToRefs } from 'pinia'
import { useWebSocketStore } from '@/stores/websocketStore'
import { WebSocketResponse } from '@/types/websocket'
import { ApplicationType } from '@/types/infoModel'

export const useApplicationStore = defineStore('applicationInfoSub', () => {
  const appInfos: Ref<ApplicationType[]> = ref([])
  // 状态
  const selectedApp = ref<ApplicationType | null>(null)
  const inspectingAppDetails = ref<ApplicationType | null>(null)

  // 计算属性
  const activeApps = computed(() => {
    return appInfos.value.filter((app) => !app.ExitCode)
  })

  const isInspecting = computed(() => !!inspectingAppDetails.value)

  const inactiveApps = computed(() => {
    return appInfos.value.filter((app) => !!app.ExitCode)
  })

  const selectedAppIndex = computed(() => {
    if (!selectedApp.value) return -1
    return appInfos.value.findIndex((app) => app.id === selectedApp.value?.id)
  })

  const totalMemoryUsage = computed(() => {
    return appInfos.value.reduce((total, app) => total + app.UseMemory, 0)
  })

  const totalProcessCount = computed(() => {
    return appInfos.value.reduce((total, app) => total + app.ProcessIds.length, 0)
  })

  // 操作
  const setAppInfos = (apps: ApplicationType[]) => {
    appInfos.value = apps

    // 如果当前选中的应用不在新列表中，清空选中
    if (selectedApp.value && !apps.find((app) => app.id === selectedApp.value?.id)) {
      selectedApp.value = null
    }
  }

  const setInspectingAppDetails = (app: ApplicationType | null) => {
    inspectingAppDetails.value = app
  }

  const setSelectedApp = (app: ApplicationType | null) => {
    selectedApp.value = app
  }

  const selectNextApp = () => {
    if (appInfos.value.length === 0) return

    const currentIndex = selectedAppIndex.value
    let newIndex = 0

    if (currentIndex === -1 || currentIndex >= appInfos.value.length - 1) {
      newIndex = 0
    } else {
      newIndex = currentIndex + 1
    }

    selectedApp.value = appInfos.value[newIndex]
  }

  const selectPreviousApp = () => {
    if (appInfos.value.length === 0) return

    const currentIndex = selectedAppIndex.value
    let newIndex = appInfos.value.length - 1

    if (currentIndex <= 0) {
      newIndex = appInfos.value.length - 1
    } else {
      newIndex = currentIndex - 1
    }

    selectedApp.value = appInfos.value[newIndex]
  }

  const updateApp = (updatedApp: ApplicationType) => {
    const index = appInfos.value.findIndex((app) => app.id === updatedApp.id)
    if (index !== -1) {
      appInfos.value[index] = updatedApp

      // 如果更新的是当前选中的应用，同步更新选中状态
      if (selectedApp.value?.id === updatedApp.id) {
        selectedApp.value = updatedApp
      }
    }
  }

  const removeApp = (appId: string) => {
    const index = appInfos.value.findIndex((app) => app.id === appId)
    if (index !== -1) {
      appInfos.value.splice(index, 1)

      // 如果删除的是当前选中的应用，选择下一个
      if (selectedApp.value?.id === appId) {
        if (appInfos.value.length > 0) {
          // 优先选择同位置的应用，如果没有则选择前一个
          const newIndex = Math.min(index, appInfos.value.length - 1)
          selectedApp.value = appInfos.value[newIndex]
        } else {
          selectedApp.value = null
        }
      }
    }
  }

  const clearAll = () => {
    appInfos.value = []
    selectedApp.value = null
  }

  /**
   * 订阅信息
   */
  function subscribe() {
    const webSocketStore = useWebSocketStore()
    const { isOpen } = storeToRefs(webSocketStore)
    watch(
      isOpen,
      (newValue) => {
        if (newValue) {
          webSocketStore.registerHandler('ApplicationInfo', (data: WebSocketResponse) => {
            appInfos.value = JSON.parse(data.data)
          })
          webSocketStore.registerHandler('AppDetailInfo', (data: WebSocketResponse) => {
            console.log('AppDetailInfo', data)
            inspectingAppDetails.value = JSON.parse(data.data)
          })
        }
      },
      { immediate: true },
    )
  }

  return {
    subscribe, // 状态
    appInfos,
    selectedApp,
    inspectingAppDetails,

    // 计算属性
    activeApps,
    inactiveApps,
    selectedAppIndex,
    totalMemoryUsage,
    totalProcessCount,
    isInspecting,

    // 操作
    setAppInfos,
    setSelectedApp,
    setInspectingAppDetails,
    selectNextApp,
    selectPreviousApp,
    updateApp,
    removeApp,
    clearAll,
  }
})
