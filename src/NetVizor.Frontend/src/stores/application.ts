import { ref, Ref, watch } from 'vue'
import { defineStore, storeToRefs } from 'pinia'
import { useWebSocketStore } from '@/stores/websocketStore'
import { WebSocketResponse } from '@/types/websocket'
import { ApplicationType } from '@/types/infoModel'

export const useApplicationStore = defineStore('applicationInfoSub', () => {
  const appInfos: Ref<ApplicationType[]> = ref([])
  // 状态
  const selectedApp = ref<ApplicationType | null>(null)

  // 计算属性
  const activeApps = computed(() => {
    return appInfos.value.filter((app) => !app.ExitCode)
  })

  const inactiveApps = computed(() => {
    return appInfos.value.filter((app) => !!app.ExitCode)
  })

  const selectedAppIndex = computed(() => {
    if (!selectedApp.value) return -1
    return appInfos.value.findIndex((app) => app.Id === selectedApp.value?.Id)
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
    if (selectedApp.value && !apps.find((app) => app.Id === selectedApp.value?.Id)) {
      selectedApp.value = null
    }
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
    const index = appInfos.value.findIndex((app) => app.Id === updatedApp.Id)
    if (index !== -1) {
      appInfos.value[index] = updatedApp

      // 如果更新的是当前选中的应用，同步更新选中状态
      if (selectedApp.value?.Id === updatedApp.Id) {
        selectedApp.value = updatedApp
      }
    }
  }

  const removeApp = (appId: string) => {
    const index = appInfos.value.findIndex((app) => app.Id === appId)
    if (index !== -1) {
      appInfos.value.splice(index, 1)

      // 如果删除的是当前选中的应用，选择下一个
      if (selectedApp.value?.Id === appId) {
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

  return {
    subscribe, // 状态
    appInfos,
    selectedApp,

    // 计算属性
    activeApps,
    inactiveApps,
    selectedAppIndex,
    totalMemoryUsage,
    totalProcessCount,

    // 操作
    setAppInfos,
    setSelectedApp,
    selectNextApp,
    selectPreviousApp,
    updateApp,
    removeApp,
    clearAll,
  }
})
