<template>
  <div ref="mainViewRef" class="main-view">
    <!-- 顶部 - 搜索和标题 -->
    <div class="main-header" :style="{ height: headerHeight + 'px' }">
      <div class="header-content">
        <div class="header-info">
          <h2 class="main-header-title">网络连接监控</h2>
          <p class="main-header-subtitle">系统网络活动监控</p>
        </div>

        <div class="header-actions">
          <div class="search-area">
            <n-input
              v-model:value="searchQuery"
              placeholder="搜索连接、域名或IP..."
              size="medium"
            >
              <template #prefix>
                <n-icon :component="SearchOutline" />
              </template>
            </n-input>
          </div>

          <div class="filter-buttons">
            <n-button size="small" type="primary" quaternary @click="handleFilter">
              过滤
            </n-button>
            <n-button size="small" quaternary @click="handleRefresh">
              刷新
            </n-button>
          </div>
        </div>
      </div>
    </div>

    <!-- 连接列表 -->
    <connections-table1
      :connections="filteredConnections"
      :height="connectionsTableHeight"
    />

    <!-- 时间轴分割线 -->
    <div class="resize-handle-horizontal" @mousedown="startTimelineResize">
      <div class="resize-handle-hover-h"></div>
    </div>

    <!-- 时间轴事件流 - 现在更简洁了 -->
    <EventTimeline :height="timelineHeight" />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch, onUnmounted, nextTick } from 'vue'
import { NButton, NInput, NIcon } from 'naive-ui'
import { SearchOutline } from '@vicons/ionicons5'
import EventTimeline from './components/EventTimeline.vue'
import { useApplicationStore } from '@/stores/application'
import { storeToRefs } from 'pinia'
import { ResponseModel, SubscriptionInfo, SubscriptionProcessInfo } from '@/types/response'
import { httpClient } from '@/utils/http'
import { useWebSocketStore } from '@/stores/websocketStore'
import { useProcessStore } from '@/stores/processInfo'

import ConnectionsTable1 from '@/views/MonitorView/components/ConnectionsTable1.vue'

const webSocketStore = useWebSocketStore()
const { isOpen } = storeToRefs(webSocketStore)

const applicationStore = useApplicationStore()
const { selectedApp } = storeToRefs(applicationStore)

const processStore = useProcessStore()
const { processInfos } = storeToRefs(processStore)



// 订阅进程信息
processStore.subscribe()

// Props - 只保留必要的布局相关属性
const props = defineProps<{
  width?: number // 可选的宽度，如果父组件需要控制
}>()

// 组件引用
const mainViewRef = ref<HTMLElement>()

// 内部管理的布局尺寸
const headerHeight = ref(80)
const timelineHeight = ref(200)
const mainViewHeight = ref(600)

// Timeline拖拽限制
const MIN_TIMELINE_HEIGHT = 100
const MAX_TIMELINE_HEIGHT = 800

// Timeline拖拽状态
const timelineResizing = ref<{
  isResizing: boolean
  startY: number
  startHeight: number
}>({
  isResizing: false,
  startY: 0,
  startHeight: 0,
})

// 内部管理的状态
const searchQuery = ref('')
const isTimelinePaused = ref(false)
const expandedProcesses = ref<number[]>([])

// 计算ConnectionsTable1的可用高度
const connectionsTableHeight = computed(() => {
  const totalHeight = mainViewHeight.value
  const usedHeight = headerHeight.value + timelineHeight.value + 4 // 4px为分割线高度
  return `${Math.max(200, totalHeight - usedHeight)}px`
})

// 过滤后的连接数据
const filteredConnections = computed(() => {
  const connections = processInfos.value || []

  // 如果有搜索条件，进行过滤
  if (searchQuery.value) {
    const query = searchQuery.value.toLowerCase()
    return connections.filter(process => {
      // 搜索进程名
      if (process.processName.toLowerCase().includes(query)) {
        return true
      }
      // 搜索连接信息
      return process.connections.some(conn =>
        conn.localEndpoint.address.includes(query) ||
        conn.remoteEndpoint.address.includes(query) ||
        conn.localEndpoint.port.toString().includes(query) ||
        conn.remoteEndpoint.port.toString().includes(query)
      )
    })
  }

  return connections
})
// Timeline拖拽逻辑
const startTimelineResize = (event: MouseEvent) => {
  event.preventDefault()

  timelineResizing.value = {
    isResizing: true,
    startY: event.clientY,
    startHeight: timelineHeight.value,
  }

  document.addEventListener('mousemove', handleTimelineResize)
  document.addEventListener('mouseup', stopTimelineResize)
  document.body.style.cursor = 'row-resize'
  document.body.style.userSelect = 'none'
}

const handleTimelineResize = (event: MouseEvent) => {
  if (!timelineResizing.value.isResizing) return

  const { startY, startHeight } = timelineResizing.value
  const deltaY = startY - event.clientY // 向上拖拽为正值
  const newHeight = Math.min(
    MAX_TIMELINE_HEIGHT,
    Math.max(MIN_TIMELINE_HEIGHT, startHeight + deltaY)
  )

  timelineHeight.value = newHeight
}

const stopTimelineResize = () => {
  timelineResizing.value.isResizing = false
  document.removeEventListener('mousemove', handleTimelineResize)
  document.removeEventListener('mouseup', stopTimelineResize)
  document.body.style.cursor = ''
  document.body.style.userSelect = ''
}

// 更新主视图高度
const updateMainViewHeight = () => {
  if (mainViewRef.value) {
    mainViewHeight.value = mainViewRef.value.clientHeight
  }
}

// 事件处理函数
const handleFilter = () => {
  console.log('打开过滤器')
}

const handleRefresh = () => {
  console.log('刷新数据')
  processStore.refresh?.()
}

// 监听选中应用的变化
watch(selectedApp, (newVal, oldVal) => {
  // 进行更新，先把原始数据清空
  processStore.clear()
  // 清空搜索
  searchQuery.value = ''

  console.log(newVal.id, '==========')
})

onMounted(() => {
  // 初始化主视图高度
  nextTick(() => {
    updateMainViewHeight()
  })

  // 监听窗口大小变化
  window.addEventListener('resize', updateMainViewHeight)

  // 监听 WebSocket 状态
  watch(
    [isOpen, selectedApp],
    ([newValue, newValue1]) => {
      if (newValue && newValue1 && newValue1.processIds && newValue1.processIds.length > 0) {
        // 发送请求【请求订阅软件列表】
        const subAppInfo: SubscriptionProcessInfo = {
          subscriptionType: 'ProcessInfo',
          interval: 400,
          processIds: newValue1.processIds,
        }

        const data = JSON.stringify(subAppInfo)
        httpClient
          .post(`/subscribe-process`, data)
          .then((res: ResponseModel) => {
            console.log('订阅进程信息成功:', res)
          })
          .catch((err) => {
            console.error('订阅进程信息失败:', err)
          })
      }
    },
    { immediate: true },
  )
})

onUnmounted(() => {
  // 清理事件监听
  window.removeEventListener('resize', updateMainViewHeight)
  document.removeEventListener('mousemove', handleTimelineResize)
  document.removeEventListener('mouseup', stopTimelineResize)

  const subAppInfo: SubscriptionInfo = {
    subscriptionType: 'ProcessInfo',
    interval: 1,
  }

  httpClient
    .post(`/unsubscribe`, JSON.stringify(subAppInfo))
    .then((res: ResponseModel) => {
      console.log('取消订阅进程信息成功:', res)
    })
    .catch((err) => {
      console.error('取消订阅进程信息失败:', err)
    })
})
</script>

<style scoped>
/* 主视图容器 */
.main-view {
  flex: 1;
  display: flex;
  flex-direction: column;
  min-width: 0;
  min-height: 0;
  background: var(--bg-card);
  overflow: hidden;
  height: 100%;
}

/* 顶部区域 */
.main-header {
  background: var(--bg-glass);
  backdrop-filter: var(--backdrop-blur);
  border-bottom: 1px solid var(--border-primary);
  padding: 0 24px;
  flex-shrink: 0;
}

.header-content {
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.header-info {
  flex: 0 0 auto;
}

.main-header-title {
  font-size: 20px;
  font-weight: 700;
  color: var(--text-secondary);
  margin: 0;
  line-height: 1;
  white-space: nowrap;
}

.main-header-subtitle {
  font-size: 14px;
  color: var(--text-muted);
  margin: 4px 0 0 0;
  white-space: nowrap;
}

.header-actions {
  display: flex;
  align-items: center;
  gap: 16px;
}

.search-area {
  width: 300px;
}

.filter-buttons {
  display: flex;
  gap: 8px;
  flex-shrink: 0;
}

/* 时间轴分割线 */
.resize-handle-horizontal {
  height: 4px;
  background: var(--border-tertiary);
  cursor: row-resize;
  transition: background-color 0.2s;
  position: relative;
  z-index: 10;
  flex-shrink: 0;
}

.resize-handle-horizontal:hover {
  background: rgba(168, 85, 247, 0.5);
}

.resize-handle-hover-h {
  position: absolute;
  inset: -4px 0;
}

.resize-handle-horizontal:active {
  background-color: var(--accent-primary) !important;
}

/* 自定义 Naive UI 样式 */
:deep(.n-input .n-input__input-el) {
  background: var(--bg-card);
  border-color: var(--border-tertiary);
  color: var(--text-secondary);
}

:deep(.n-input:hover .n-input__input-el) {
  border-color: var(--border-hover);
}

:deep(.n-input.n-input--focus .n-input__input-el) {
  border-color: var(--accent-primary);
}

/* 响应式设计 */
@media (max-width: 1200px) {
  .header-info {
    display: none;
  }

  .header-content {
    justify-content: flex-end;
  }

  .search-area {
    width: 280px;
  }
}

@media (max-width: 900px) {
  .main-header {
    padding: 0 16px;
  }

  .header-actions {
    gap: 12px;
    flex: 1;
    min-width: 0;
  }

  .search-area {
    flex: 1;
    min-width: 150px;
    max-width: 300px;
    width: auto;
  }

  .filter-buttons {
    gap: 6px;
    flex-shrink: 0;
  }
}

@media (max-width: 600px) {
  .main-header {
    padding: 0 12px;
  }

  .header-actions {
    gap: 8px;
  }

  .search-area {
    min-width: 120px;
    max-width: none;
  }

  .filter-buttons {
    flex-direction: column;
    gap: 4px;
  }

  .filter-buttons .n-button {
    font-size: 12px;
    padding: 4px 8px;
  }
}

@media (max-width: 480px) {
  .filter-buttons {
    display: none;
  }

  .search-area {
    min-width: 100px;
  }
}
</style>
