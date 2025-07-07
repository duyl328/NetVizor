import { ref, computed } from 'vue'
import { defineStore } from 'pinia'

export const useLayoutStore = defineStore('layout', () => {
  // 布局尺寸状态
  const sidebarWidth = ref(400)
  const inspectorWidth = ref(350)
  const timelineHeight = ref(200)
  const mainHeaderHeight = ref(80)

  // 调整分割条范围限制
  const MIN_SIDEBAR_WIDTH = 200
  const MAX_SIDEBAR_WIDTH = 600
  const MIN_INSPECTOR_WIDTH = 250
  const MAX_INSPECTOR_WIDTH = 650
  const MIN_TIMELINE_HEIGHT = 100
  const MAX_TIMELINE_HEIGHT = 800

  // 拖拽状态
  const resizing = ref<{
    type: 'sidebar' | 'inspector' | 'timeline' | null
    startX: number
    startY: number
    startWidth: number
    startHeight: number
  }>({
    type: null,
    startX: 0,
    startY: 0,
    startWidth: 0,
    startHeight: 0,
  })

  // 计算属性
  const isResizing = computed(() => resizing.value.type !== null)

  // 开始拖拽
  const startResize = (type: 'sidebar' | 'inspector' | 'timeline', event: MouseEvent) => {
    event.preventDefault()

    resizing.value = {
      type,
      startX: event.clientX,
      startY: event.clientY,
      startWidth:
        type === 'sidebar' ? sidebarWidth.value : type === 'inspector' ? inspectorWidth.value : 0,
      startHeight: type === 'timeline' ? timelineHeight.value : 0,
    }

    document.addEventListener('mousemove', handleResize)
    document.addEventListener('mouseup', stopResize)
    document.body.style.cursor = type === 'timeline' ? 'row-resize' : 'col-resize'
    document.body.style.userSelect = 'none'
  }

  // 处理拖拽
  const handleResize = (event: MouseEvent) => {
    if (!resizing.value.type) return

    const { type, startX, startY, startWidth, startHeight } = resizing.value

    if (type === 'sidebar') {
      const deltaX = event.clientX - startX
      const newWidth = Math.min(MAX_SIDEBAR_WIDTH, Math.max(MIN_SIDEBAR_WIDTH, startWidth + deltaX))
      sidebarWidth.value = newWidth
    } else if (type === 'inspector') {
      const deltaX = startX - event.clientX
      const newWidth = Math.min(
        MAX_INSPECTOR_WIDTH,
        Math.max(MIN_INSPECTOR_WIDTH, startWidth + deltaX),
      )
      inspectorWidth.value = newWidth
    } else if (type === 'timeline') {
      const deltaY = startY - event.clientY
      const newHeight = Math.min(
        MAX_TIMELINE_HEIGHT,
        Math.max(MIN_TIMELINE_HEIGHT, startHeight + deltaY),
      )
      timelineHeight.value = newHeight
    }
  }

  // 停止拖拽
  const stopResize = () => {
    resizing.value.type = null
    document.removeEventListener('mousemove', handleResize)
    document.removeEventListener('mouseup', stopResize)
    document.body.style.cursor = ''
    document.body.style.userSelect = ''
  }

  // 重置布局
  const resetLayout = () => {
    sidebarWidth.value = 400
    inspectorWidth.value = 350
    timelineHeight.value = 200
    mainHeaderHeight.value = 80
  }

  // 设置布局尺寸（用于恢复状态）
  const setLayoutSizes = (sizes: {
    sidebarWidth?: number
    inspectorWidth?: number
    timelineHeight?: number
    mainHeaderHeight?: number
  }) => {
    if (sizes.sidebarWidth !== undefined) {
      sidebarWidth.value = Math.min(MAX_SIDEBAR_WIDTH, Math.max(MIN_SIDEBAR_WIDTH, sizes.sidebarWidth))
    }
    if (sizes.inspectorWidth !== undefined) {
      inspectorWidth.value = Math.min(MAX_INSPECTOR_WIDTH, Math.max(MIN_INSPECTOR_WIDTH, sizes.inspectorWidth))
    }
    if (sizes.timelineHeight !== undefined) {
      timelineHeight.value = Math.min(MAX_TIMELINE_HEIGHT, Math.max(MIN_TIMELINE_HEIGHT, sizes.timelineHeight))
    }
    if (sizes.mainHeaderHeight !== undefined) {
      mainHeaderHeight.value = sizes.mainHeaderHeight
    }
  }

  // 获取当前布局配置（用于持久化）
  const getLayoutConfig = () => ({
    sidebarWidth: sidebarWidth.value,
    inspectorWidth: inspectorWidth.value,
    timelineHeight: timelineHeight.value,
    mainHeaderHeight: mainHeaderHeight.value,
  })

  // 清理资源
  const cleanup = () => {
    if (isResizing.value) {
      stopResize()
    }
  }

  return {
    // 状态
    sidebarWidth,
    inspectorWidth,
    timelineHeight,
    mainHeaderHeight,
    resizing,
    isResizing,

    // 常量
    MIN_SIDEBAR_WIDTH,
    MAX_SIDEBAR_WIDTH,
    MIN_INSPECTOR_WIDTH,
    MAX_INSPECTOR_WIDTH,
    MIN_TIMELINE_HEIGHT,
    MAX_TIMELINE_HEIGHT,

    // 方法
    startResize,
    handleResize,
    stopResize,
    resetLayout,
    setLayoutSizes,
    getLayoutConfig,
    cleanup,
  }
})
