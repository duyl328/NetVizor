<template>
  <div class="monitor-view">
    <!-- 主内容区域 - 可拖拽布局 -->
    <div class="main-content">
      <!-- 左侧边栏 -->
      <MonitorSidebar :width="sidebarWidth" :data="mockData" />

      <!-- 左侧分割线 -->
      <div class="resize-handle-vertical" @mousedown="startResize('sidebar', $event)">
        <div class="resize-handle-hover"></div>
      </div>

      <!-- 中间主视图区域 -->
      <MonitorMainPanel
        v-show="false"
        :headerHeight="mainHeaderHeight"
        :timelineHeight="timelineHeight"
        :searchQuery="searchQuery"
        :connections="mockConnections"
        :events="mockEvents"
        @update:searchQuery="searchQuery = $event"
        @resizeTimeline="startResize('timeline', $event)"
        @selectConnection="handleConnectionSelect"
      />

      <!-- 右侧分割线 -->
      <div class="resize-handle-vertical" @mousedown="startResize('inspector', $event)">
        <div class="resize-handle-hover"></div>
      </div>

      <!-- 右侧检查器面板 -->
      <MonitorInspector
        v-show="false"
        :width="inspectorWidth"
        :selectedConnection="selectedConnection"
      />
    </div>
  </div>
</template>

<script setup lang="ts">
defineOptions({
  name: 'MonitorView',
})

import { ref, onMounted, onUnmounted, provide } from 'vue'
import MonitorSidebar from './MonitorSidebar.vue'
import MonitorMainPanel from './MonitorMainPanel.vue'
import MonitorInspector from './MonitorInspector.vue'

// 布局尺寸控制
const sidebarWidth = ref(300)
const inspectorWidth = ref(350)
const timelineHeight = ref(500)
const mainHeaderHeight = ref(80)

// 调整分割条范围限制
const MIN_SIDEBAR_WIDTH = 200
const MAX_SIDEBAR_WIDTH = 600
const MIN_INSPECTOR_WIDTH = 250
const MAX_INSPECTOR_WIDTH = 650
const MIN_TIMELINE_HEIGHT = 100
const MAX_TIMELINE_HEIGHT = 800

// 响应式数据
const searchQuery = ref('')
const selectedConnection = ref<any>(null)

// 模拟数据
const mockData = ref({
  activeConnections: 247,
  processCount: 43,
  networkSpeed: '5.2 MB/s',
  ruleCount: 156,
})

const mockConnections = ref([
  {
    id: 1,
    process: 'chrome.exe',
    localAddress: '192.168.1.100',
    remoteAddress: 'google.com',
    status: 'established',
    traffic: '542 KB/s',
  },
  // ... 更多连接数据
])

const mockEvents = ref([
  {
    id: 1,
    time: new Date().toLocaleTimeString(),
    type: 'connection',
    eventType: '连接建立',
    description: 'chrome.exe → google.com:443',
  },
  // ... 更多事件数据
])

// 提供布局配置给子组件
provide('layoutConfig', {
  sidebarWidth,
  inspectorWidth,
  timelineHeight,
  mainHeaderHeight,
})

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

// 处理连接选中
const handleConnectionSelect = (connection: any) => {
  selectedConnection.value = connection
}

// 重置布局
const resetLayout = () => {
  sidebarWidth.value = 300
  inspectorWidth.value = 350
  timelineHeight.value = 200
}

// 键盘快捷键
onMounted(() => {
  const handleKeydown = (event: KeyboardEvent) => {
    if (event.ctrlKey && event.key === 'r') {
      event.preventDefault()
      resetLayout()
    }
  }

  document.addEventListener('keydown', handleKeydown)

  onUnmounted(() => {
    document.removeEventListener('keydown', handleKeydown)
    document.removeEventListener('mousemove', handleResize)
    document.removeEventListener('mouseup', stopResize)
  })
})
</script>

<style scoped>
/* 监控视图容器 */
.monitor-view {
  height: 100%;
  width: 100%;
  display: flex;
  flex-direction: column;
  /*  overflow: hidden;*/
}

/* 主内容区域 */
.main-content {
  flex: 1;
  display: flex;
  min-height: 0;
  overflow: hidden;
}

/* 拖拽分割线 */
.resize-handle-vertical {
  width: 4px;
  background: var(--border-tertiary);
  cursor: col-resize;
  transition: background-color 0.2s;
  position: relative;
  z-index: 10;
  flex-shrink: 0;
}

.resize-handle-vertical:hover {
  background: var(--border-hover);
}

.resize-handle-hover {
  position: absolute;
  inset: 0 -4px;
}

/* 拖拽时的视觉反馈 */
.resize-handle-vertical:active {
  background-color: var(--accent-primary) !important;
}
</style>
