<template>
  <div class="main-view">
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
              :value="searchQuery"
              @update:value="$emit('update:searchQuery', $event)"
              placeholder="搜索连接、域名或IP..."
              size="medium"
            >
              <template #prefix>
                <n-icon :component="SearchOutline" />
              </template>
            </n-input>
          </div>

          <div class="filter-buttons">
            <n-button size="small" type="primary" ghost @click="handleFilter">过滤</n-button>
            <n-button size="small" ghost @click="handleRefresh">刷新</n-button>
          </div>
        </div>
      </div>
    </div>

    <!-- 连接列表 -->
    <connections-table1 :connections="filteredConnections" />

    <!-- 时间轴分割线 -->
    <div class="resize-handle-horizontal" @mousedown="$emit('resizeTimeline', $event)">
      <div class="resize-handle-hover-h"></div>
    </div>

    <!-- 时间轴事件流 -->
    <EventTimeline
      :height="timelineHeight"
      :events="events"
      :isPaused="isTimelinePaused"
      @pause="isTimelinePaused = !isTimelinePaused"
      @clear="handleClearEvents"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch, onUnmounted } from 'vue'
import { NButton, NInput, NIcon } from 'naive-ui'
import { SearchOutline } from '@vicons/ionicons5'
import ConnectionsTable from './components/ConnectionsTable.vue'
import EventTimeline from './components/EventTimeline.vue'
import { FlashOutline } from '@vicons/ionicons5'
import { useApplicationStore } from '@/stores/application'
import { storeToRefs } from 'pinia'
import { ResponseModel, SubscriptionInfo, SubscriptionProcessInfo } from '@/types/response'
import { httpClient } from '@/utils/http'
import { useWebSocketStore } from '@/stores/websocketStore'
import { useProcessStore } from '@/stores/processInfo'
import { ApplicationType } from '@/types/infoModel'
import ConnectionsTable1 from '@/views/MonitorView/components/ConnectionsTable1.vue'
import UnifiedConnectionsList from '@/views/MonitorView/components/UnifiedConnectionsList.vue'

const webSocketStore = useWebSocketStore()
const { isOpen } = storeToRefs(webSocketStore)

const applicationStore = useApplicationStore()
const { selectedApp } = storeToRefs(applicationStore)

const processStore = useProcessStore()
const { processInfos } = storeToRefs(processStore)
processStore.subscribe()

// 在父组件中
const filteredConnections = computed(() => {
  return processInfos.value || []
})

watch(selectedApp, (newVal, oldVal) => {
  // 进行更新，先把原始数据清空
  processStore.clear()

  console.log(newVal.id, '==========')
})

onMounted(() => {
  // 监听 WebSocket 状态
  watch(
    [isOpen, selectedApp],
    ([newValue, newValue1]) => {
      if (newValue && newValue1 && newValue1.processIds && newValue1.processIds.length > 0) {
        // ApplicationType
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

// Props
const props = defineProps<{
  headerHeight: number
  timelineHeight: number
  searchQuery: string
  connections: Array<unknown>
  events: Array<unknown>
}>()

// Emits
const emit = defineEmits<{
  'update:searchQuery': [value: string]
  resizeTimeline: [event: MouseEvent]
  selectConnection: [connection: unknown]
}>()

// Local state
const isMonitoring = ref(true)
const isTimelinePaused = ref(false)

// 处理过滤
const handleFilter = () => {
  console.log('打开过滤器')
}

// 处理刷新
const handleRefresh = () => {
  console.log('刷新数据')
}

// 清空事件
const handleClearEvents = () => {
  console.log('清空事件')
}
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
/* 中等屏幕：隐藏标题和副标题 */
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

/* 小屏幕：搜索框开始使用弹性布局 */
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

/* 超小屏幕：进一步优化 */
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

/* 超窄屏幕：只保留搜索框 */
@media (max-width: 480px) {
  .filter-buttons {
    display: none;
  }

  .search-area {
    min-width: 100px;
  }
}
</style>
