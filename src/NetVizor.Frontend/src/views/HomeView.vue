<template>
  <div class="app-container">
    <!-- 顶部标题栏 -->
    <div class="title-bar">
      <div class="title-bar-left">
        <div class="app-title">
          <div class="app-icon">
            <span>N</span>
          </div>
          <span class="app-name">NetGuard</span>
        </div>

        <div class="view-switcher">
          <n-radio-group v-model:value="currentView" size="small">
            <n-radio-button
              v-for="option in viewOptions"
              :key="option.value"
              :value="option.value"
            >
              {{ option.label }}
            </n-radio-button>
          </n-radio-group>
        </div>
      </div>

      <div class="title-bar-right">
        <div class="status-indicator">
          <div class="status-dot"></div>
          <span>实时监控中</span>
        </div>

        <n-button
          circle
          size="small"
          @click="openSettings"
        >
          <template #icon>
            <n-icon :component="SettingsOutline" />
          </template>
        </n-button>

        <n-button
          circle
          size="small"
          @click="toggleDarkMode"
        >
          <template #icon>
            <n-icon :component="MoonOutline" />
          </template>
        </n-button>
      </div>
    </div>

    <!-- 主内容区域 - 可拖拽布局 -->
    <div class="main-content">
      <!-- 左侧边栏 -->
      <div
        class="sidebar"
        :style="{ width: sidebarWidth + 'px' }"
      >
        <div class="sidebar-content">
          <h3 class="sidebar-title">侧边栏区域</h3>
          <div class="sidebar-items">
            <div>• 系统概览组件</div>
            <div>• 进程列表组件</div>
            <div>• 快速统计组件</div>
          </div>

          <!-- 模拟数据展示 -->
          <div class="sidebar-stats">
            <div class="stat-item">活跃连接: {{ mockData.activeConnections }}</div>
            <div class="stat-item">进程数: {{ mockData.processCount }}</div>
            <div class="stat-item">网速: {{ mockData.networkSpeed }}</div>
          </div>
        </div>
      </div>

      <!-- 左侧分割线 -->
      <div
        class="resize-handle-vertical"
        @mousedown="startResize('sidebar', $event)"
      >
        <div class="resize-handle-hover"></div>
      </div>

      <!-- 中间主视图区域 -->
      <div class="main-view">
        <!-- 中间顶部 - 搜索和标题 -->
        <div
          class="main-header"
          :style="{ height: mainHeaderHeight + 'px' }"
        >
          <div>
            <h2 class="main-header-title">主视图标题区域</h2>
            <p class="main-header-subtitle">搜索和控制组件</p>
          </div>

          <div class="search-area">
            <n-input
              v-model:value="searchQuery"
              placeholder="搜索连接、域名或IP..."
              size="small"
            >
              <template #prefix>
                <n-icon :component="SearchOutline" />
              </template>
            </n-input>
          </div>
        </div>

        <!-- 中间主体 - 连接列表表格 -->
        <div class="connections-area">
          <h3 class="connections-title">连接列表表格区域</h3>
          <div class="connections-content">
            <div>• 连接状态表格组件</div>
            <div>• 实时数据展示</div>
            <div>• 可选择和过滤</div>
            <div>• 响应式表格布局</div>

            <!-- 模拟表格区域 -->
            <div class="mock-table">
              <div
                v-for="i in 20"
                :key="i"
                class="mock-row"
              >
                <span>连接 {{ i }} - 模拟数据行 ({{ searchQuery || '无搜索' }})</span>
              </div>
            </div>
          </div>
        </div>

        <!-- 时间轴分割线 -->
        <div
          class="resize-handle-horizontal"
          @mousedown="startResize('timeline', $event)"
        >
          <div class="resize-handle-hover-h"></div>
        </div>

        <!-- 中间底部 - 时间轴事件流 -->
        <div
          class="timeline-area"
          :style="{ height: timelineHeight + 'px' }"
        >
          <div class="timeline-header">
            <h3 class="timeline-title">时间轴事件流区域</h3>
            <div class="timeline-controls">
              <n-button size="small" type="primary">暂停</n-button>
              <n-button size="small">清空</n-button>
            </div>
          </div>

          <div class="timeline-content">
            <div class="timeline-items">
              <div>• 实时事件组件</div>
              <div>• 事件流展示</div>
              <div>• 时间轴控制</div>

              <!-- 模拟事件列表 -->
              <div class="mock-events">
                <div
                  v-for="i in 10"
                  :key="i"
                  class="mock-event"
                >
                  <span>事件 {{ i }} - {{ new Date().toLocaleTimeString() }}</span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- 右侧分割线 -->
      <div
        class="resize-handle-vertical"
        @mousedown="startResize('inspector', $event)"
      >
        <div class="resize-handle-hover"></div>
      </div>

      <!-- 右侧检查器面板 -->
      <div
        class="inspector"
        :style="{ width: inspectorWidth + 'px' }"
      >
        <div class="inspector-content">
          <h3 class="inspector-title">连接详情检查器</h3>
          <div class="inspector-items">
            <div>• 连接详情组件</div>
            <div>• 地理位置信息</div>
            <div>• 安全信息展示</div>
            <div>• 流量统计图表</div>
          </div>

          <!-- 模拟详情数据 -->
          <div class="inspector-details">
            <div class="detail-card">
              <div class="detail-item">选中连接: {{ mockData.selectedConnection }}</div>
              <div class="detail-item">状态: {{ mockData.connectionStatus }}</div>
              <div class="detail-item">宽度: {{ inspectorWidth }}px</div>
            </div>

            <!-- 更多模拟内容 -->
            <div class="mock-content">
              <div v-for="i in 15" :key="i" class="mock-bar"></div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue'
import { NButton, NInput, NIcon, NRadioGroup, NRadioButton } from 'naive-ui'
import { SettingsOutline, MoonOutline, SearchOutline } from '@vicons/ionicons5'

// 响应式数据
const currentView = ref('monitor')
const searchQuery = ref('')
const isDarkMode = ref(true)

// 布局尺寸控制
const sidebarWidth = ref(280)
const inspectorWidth = ref(320)
const timelineHeight = ref(180)
const mainHeaderHeight = ref(72)

// 最小尺寸限制
const MIN_SIDEBAR_WIDTH = 200
const MAX_SIDEBAR_WIDTH = 500
const MIN_INSPECTOR_WIDTH = 250
const MAX_INSPECTOR_WIDTH = 600
const MIN_TIMELINE_HEIGHT = 120
const MAX_TIMELINE_HEIGHT = 400

// 视图选项
const viewOptions = [
  { label: '实时监控', value: 'monitor' },
  { label: '防火墙', value: 'firewall' },
  { label: '分析', value: 'analysis' }
]

// 模拟数据
const mockData = ref({
  activeConnections: 247,
  processCount: 43,
  networkSpeed: '5.2 MB/s',
  ruleCount: 156,
  selectedConnection: 'chrome.exe → google.com',
  connectionStatus: '已建立',
  localIP: '192.168.1.100'
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
  startHeight: 0
})

// 开始拖拽
const startResize = (type: 'sidebar' | 'inspector' | 'timeline', event: MouseEvent) => {
  event.preventDefault()

  resizing.value = {
    type,
    startX: event.clientX,
    startY: event.clientY,
    startWidth: type === 'sidebar' ? sidebarWidth.value :
      type === 'inspector' ? inspectorWidth.value : 0,
    startHeight: type === 'timeline' ? timelineHeight.value : 0
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
    const deltaX = startX - event.clientX // 反向计算，因为右侧面板
    const newWidth = Math.min(MAX_INSPECTOR_WIDTH, Math.max(MIN_INSPECTOR_WIDTH, startWidth + deltaX))
    inspectorWidth.value = newWidth
  } else if (type === 'timeline') {
    const deltaY = startY - event.clientY // 反向计算，因为底部面板
    const newHeight = Math.min(MAX_TIMELINE_HEIGHT, Math.max(MIN_TIMELINE_HEIGHT, startHeight + deltaY))
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

// 方法
const openSettings = () => {
  console.log('打开设置')
}

const toggleDarkMode = () => {
  isDarkMode.value = !isDarkMode.value
  console.log('切换暗色模式:', isDarkMode.value)
}

// 键盘快捷键 - 重置布局
const resetLayout = () => {
  sidebarWidth.value = 280
  inspectorWidth.value = 320
  timelineHeight.value = 180
}

// 监听键盘事件
onMounted(() => {
  const handleKeydown = (event: KeyboardEvent) => {
    if (event.ctrlKey && event.key === 'r') {
      event.preventDefault()
      resetLayout()
    }
  }

  document.addEventListener('keydown', handleKeydown)

  // 清理函数
  onUnmounted(() => {
    document.removeEventListener('keydown', handleKeydown)
    document.removeEventListener('mousemove', handleResize)
    document.removeEventListener('mouseup', stopResize)
  })
})
</script>

<style scoped>
/* 容器样式 */
.app-container {
  min-height: 100vh;
  background-color: #111827;
  color: white;
  user-select: none;
}

/* 顶部标题栏 */
.title-bar {
  height: 3rem;
  border-bottom: 1px solid #374151;
  background-color: #1f2937;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 1rem;
}

.title-bar-left {
  display: flex;
  align-items: center;
  gap: 1rem;
}

.app-title {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.app-icon {
  width: 1.5rem;
  height: 1.5rem;
  background-color: #3b82f6;
  border-radius: 0.25rem;
  display: flex;
  align-items: center;
  justify-content: center;
}

.app-icon span {
  font-size: 0.75rem;
}

.app-name {
  font-weight: 600;
}

.title-bar-right {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.status-indicator {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 0.875rem;
  color: #d1d5db;
}

.status-dot {
  width: 0.5rem;
  height: 0.5rem;
  background-color: #34d399;
  border-radius: 50%;
  animation: pulse 2s infinite;
}

/* 主内容区域 */
.main-content {
  height: calc(100vh - 3rem);
  display: flex;
}

/* 侧边栏 */
.sidebar {
  background-color: #1f2937;
  border: 1px solid #3b82f6;
  overflow: hidden;
  display: flex;
  flex-direction: column;
}

.sidebar-content {
  padding: 1rem;
  overflow-y: auto;
  flex: 1;
}

.sidebar-title {
  font-size: 0.875rem;
  font-weight: 500;
  color: #d1d5db;
  margin-bottom: 0.75rem;
}

.sidebar-items {
  font-size: 0.75rem;
  color: #9ca3af;
  line-height: 1.5;
}

.sidebar-stats {
  margin-top: 1rem;
  padding: 0.75rem;
  background-color: #374151;
  border-radius: 0.25rem;
}

.stat-item {
  font-size: 0.75rem;
  color: #d1d5db;
  margin-bottom: 0.25rem;
}

.stat-item:last-child {
  margin-bottom: 0;
}

/* 拖拽分割线 */
.resize-handle-vertical {
  width: 0.25rem;
  background-color: #4b5563;
  cursor: col-resize;
  transition: background-color 0.2s;
  position: relative;
  z-index: 10;
}

.resize-handle-vertical:hover {
  background-color: #60a5fa;
}

.resize-handle-hover {
  position: absolute;
  inset: 0 -0.25rem;
}

.resize-handle-horizontal {
  height: 0.25rem;
  background-color: #4b5563;
  cursor: row-resize;
  transition: background-color 0.2s;
  position: relative;
  z-index: 10;
}

.resize-handle-horizontal:hover {
  background-color: #a78bfa;
}

.resize-handle-hover-h {
  position: absolute;
  inset: -0.25rem 0;
}

/* 主视图 */
.main-view {
  flex: 1;
  display: flex;
  flex-direction: column;
  min-width: 0;
}

.main-header {
  background-color: #1f2937;
  border: 1px solid #10b981;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 1rem;
}

.main-header-title {
  font-size: 1rem;
  font-weight: 600;
}

.main-header-subtitle {
  font-size: 0.75rem;
  color: #9ca3af;
}

.search-area {
  width: 16rem;
}

/* 连接区域 */
.connections-area {
  flex: 1;
  background-color: #1f2937;
  border: 1px solid #eab308;
  padding: 1rem;
  overflow: hidden;
}

.connections-title {
  font-size: 0.875rem;
  font-weight: 500;
  color: #d1d5db;
  margin-bottom: 0.75rem;
}

.connections-content {
  font-size: 0.75rem;
  color: #9ca3af;
  overflow-y: auto;
  height: 100%;
}

.mock-table {
  margin-top: 1rem;
}

.mock-row {
  height: 2rem;
  background-color: #374151;
  border-radius: 0.25rem;
  display: flex;
  align-items: center;
  padding: 0 0.75rem;
  margin-bottom: 0.5rem;
}

.mock-row span {
  font-size: 0.75rem;
  color: #d1d5db;
}

/* 时间轴区域 */
.timeline-area {
  background-color: #1f2937;
  border: 1px solid #a855f7;
  padding: 1rem;
  overflow: hidden;
}

.timeline-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 0.5rem;
}

.timeline-title {
  font-size: 0.875rem;
  font-weight: 500;
  color: #d1d5db;
}

.timeline-controls {
  display: flex;
  gap: 0.5rem;
}

.timeline-content {
  height: 100%;
  overflow-y: auto;
}

.timeline-items {
  font-size: 0.75rem;
  color: #9ca3af;
}

.mock-events {
  margin-top: 0.5rem;
}

.mock-event {
  height: 1.5rem;
  background-color: #374151;
  border-radius: 0.25rem;
  display: flex;
  align-items: center;
  padding: 0 0.5rem;
  margin-bottom: 0.5rem;
}

.mock-event span {
  font-size: 0.75rem;
  color: #d1d5db;
}

/* 检查器面板 */
.inspector {
  background-color: #1f2937;
  border: 1px solid #ef4444;
  overflow: hidden;
}

.inspector-content {
  padding: 1rem;
  height: 100%;
  overflow-y: auto;
}

.inspector-title {
  font-size: 0.875rem;
  font-weight: 500;
  color: #d1d5db;
  margin-bottom: 0.75rem;
}

.inspector-items {
  font-size: 0.75rem;
  color: #9ca3af;
  line-height: 1.5;
}

.inspector-details {
  margin-top: 1rem;
}

.detail-card {
  padding: 0.75rem;
  background-color: #374151;
  border-radius: 0.25rem;
}

.detail-item {
  font-size: 0.75rem;
  color: #d1d5db;
  margin-bottom: 0.25rem;
}

.detail-item:last-child {
  margin-bottom: 0;
}

.mock-content {
  margin-top: 0.75rem;
}

.mock-bar {
  height: 1rem;
  background-color: #374151;
  border-radius: 0.25rem;
  margin-bottom: 0.5rem;
}

/* 滚动条样式 */
::-webkit-scrollbar {
  width: 6px;
  height: 6px;
}

::-webkit-scrollbar-track {
  background: transparent;
}

::-webkit-scrollbar-thumb {
  background: rgba(156, 163, 175, 0.3);
  border-radius: 3px;
}

::-webkit-scrollbar-thumb:hover {
  background: rgba(156, 163, 175, 0.5);
}

/* 动画 */
@keyframes pulse {
  0%, 100% {
    opacity: 1;
  }
  50% {
    opacity: 0.5;
  }
}

/* 拖拽时的视觉反馈 */
.resize-handle-vertical:active,
.resize-handle-horizontal:active {
  background-color: rgba(59, 130, 246, 0.8) !important;
}

/* 防止文本选择 */
.select-none {
  -webkit-user-select: none;
  -moz-user-select: none;
  -ms-user-select: none;
  user-select: none;
}

/* 确保内容不会溢出 */
.overflow-hidden {
  overflow: hidden;
}

.min-w-0 {
  min-width: 0;
}
</style>
