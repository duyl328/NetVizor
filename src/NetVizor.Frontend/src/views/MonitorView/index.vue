<template>
  <div class="monitor-view">
    <!-- 主内容区域 - 可拖拽布局 -->
    <div class="main-content">
      <!-- 左侧边栏 -->
      <div class="sidebar-wrapper" :style="{ width: sidebarWidth + 'px' }">
        <MonitorSidebar :width="sidebarWidth" />
      </div>

      <!-- 左侧分割线 -->
      <div class="resize-handle-vertical" @mousedown="startResize('sidebar', $event)">
        <div class="resize-handle-hover"></div>
      </div>

      <!-- 中间主视图区域 -->
      <div class="main-panel-wrapper">
        <MonitorMainPanel :width="mainPanelWidth" />
      </div>

      <!-- 右侧分割线 -->
      <div class="resize-handle-vertical" @mousedown="startResize('inspector', $event)">
        <div class="resize-handle-hover"></div>
      </div>

      <!-- 右侧检查器面板 -->
      <div class="inspector-wrapper" :style="{ width: inspectorWidth + 'px' }">
        <MonitorInspector :width="inspectorWidth" />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
defineOptions({
  name: 'MonitorView',
})

import { ref, computed, onMounted, onUnmounted } from 'vue'
import MonitorSidebar from './MonitorSidebar.vue'
import MonitorMainPanel from './MonitorMainPanel.vue'
import MonitorInspector from './MonitorInspector.vue'

// 布局尺寸控制
const sidebarWidth = ref(400)
const inspectorWidth = ref(350)

// 调整分割条范围限制
const MIN_SIDEBAR_WIDTH = 200
const MAX_SIDEBAR_WIDTH = 600
const MIN_INSPECTOR_WIDTH = 250
const MAX_INSPECTOR_WIDTH = 650

// 计算主面板的可用宽度
const mainPanelWidth = computed(() => {
  // 这里可以根据需要计算，如果不需要传递具体宽度，可以移除这个computed
  return window.innerWidth - sidebarWidth.value - inspectorWidth.value - 8 // 8px为分割线宽度
})

// 拖拽状态
const resizing = ref<{
  type: 'sidebar' | 'inspector' | null
  startX: number
  startWidth: number
}>({
  type: null,
  startX: 0,
  startWidth: 0,
})

// 开始拖拽
const startResize = (type: 'sidebar' | 'inspector', event: MouseEvent) => {
  event.preventDefault()

  resizing.value = {
    type,
    startX: event.clientX,
    startWidth: type === 'sidebar' ? sidebarWidth.value : inspectorWidth.value,
  }

  document.addEventListener('mousemove', handleResize)
  document.addEventListener('mouseup', stopResize)
  document.body.style.cursor = 'col-resize'
  document.body.style.userSelect = 'none'
}

// 处理拖拽
const handleResize = (event: MouseEvent) => {
  if (!resizing.value.type) return

  const { type, startX, startWidth } = resizing.value

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
  width: 100%;
  height: 100vh;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

/* 主内容区域 */
.main-content {
  display: flex;
  width: 100%;
  height: 100%;
  position: relative;
  overflow: hidden;
}

/* 侧边栏包装器 */
.sidebar-wrapper {
  height: 100%;
  flex-shrink: 0;
  background: var(--bg-card);
  border-right: 1px solid var(--border-primary);
  overflow-y: auto;
}

/* 主面板包装器 */
.main-panel-wrapper {
  flex: 1;
  min-width: 0;
  height: 100%;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

/* 检查器包装器 */
.inspector-wrapper {
  height: 100%;
  flex-shrink: 0;
  background: var(--bg-card);
  border-left: 1px solid var(--border-primary);
  overflow-y: auto;
}

/* 拖拽分割线 */
.resize-handle-vertical {
  width: 4px;
  height: 100%;
  background: var(--border-tertiary);
  cursor: col-resize;
  transition: background-color 0.2s;
  position: relative;
  z-index: 60;
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

/* 响应式设计 */
@media (max-width: 1200px) {
  .inspector-wrapper {
    display: none;
  }

  .resize-handle-vertical:last-of-type {
    display: none;
  }
}

@media (max-width: 768px) {
  .sidebar-wrapper {
    display: none;
  }

  .resize-handle-vertical:first-of-type {
    display: none;
  }

  .main-panel-wrapper {
    width: 100%;
  }
}
</style>
