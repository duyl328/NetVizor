<template>
  <div class="timeline-area" :style="{ height: height + 'px' }">
    <div class="timeline-header">
      <h3 class="timeline-title">
        <span class="title-icon">📈</span>
        实时事件流
        <span class="event-count">{{ events.length }} 个事件</span>
      </h3>
      <div class="timeline-controls">
        <n-button
          size="small"
          :type="isPaused ? 'info' : 'warning'"
          ghost
          @click="handleTogglePause"
        >
          {{ isPaused ? '继续' : '暂停' }}
        </n-button>
        <n-button size="small" ghost @click="handleClearEvents">清空</n-button>
      </div>
    </div>

    <div class="timeline-content scrollbar-warning scrollbar-animated">
      <div class="events-container">
        <transition-group name="event-list" tag="div">
          <div
            v-for="event in displayEvents"
            :key="event.id"
            class="event-item"
            :class="[`event-${event.type}`, { 'event-new': event.isNew }]"
          >
            <div class="event-time">{{ event.time }}</div>
            <div class="event-content">
              <div class="event-type">{{ event.eventType }}</div>
              <div class="event-desc">{{ event.description }}</div>
            </div>
            <div class="event-indicator"></div>
          </div>
        </transition-group>

        <!-- 空状态 -->
        <div v-if="events.length === 0" class="empty-events">
          <div class="empty-icon">📭</div>
          <div class="empty-text">暂无事件</div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { NButton } from 'naive-ui'
import { useProcessStore } from '@/stores/processInfo'
import { storeToRefs } from 'pinia'

// 只保留高度控制
const props = defineProps<{
  height: number
}>()

// 使用 Process Store
const processStore = useProcessStore()
const { processInfos } = storeToRefs(processStore)

// 组件内部状态管理
const isPaused = ref(false)
const internalEvents = ref<Array<{
  id: number
  time: string
  type: 'info' | 'warning' | 'success' | 'error'
  eventType: string
  description: string
  timestamp: number
}>>([])

// 显示的事件（添加动画标记）
const displayEvents = ref(internalEvents.value.map(e => ({ ...e, isNew: false })))

// 从 processInfos 派生事件数据
const events = computed(() => {
  if (isPaused.value) {
    return internalEvents.value // 暂停时返回内部缓存的事件
  }
  return internalEvents.value
})

// 添加事件的内部方法
const addEvent = (event: {
  time: string
  type: 'info' | 'warning' | 'success' | 'error'
  eventType: string
  description: string
}) => {
  if (isPaused.value) return

  const newEvent = {
    ...event,
    id: Date.now() + Math.random(),
    timestamp: Date.now()
  }

  internalEvents.value.unshift(newEvent)

  // 限制最大事件数量
  if (internalEvents.value.length > 1000) {
    internalEvents.value = internalEvents.value.slice(0, 1000)
  }
}

// 监听 processInfos 变化，自动生成事件
watch(processInfos, (newProcessInfos, oldProcessInfos) => {
  if (!oldProcessInfos || oldProcessInfos.length === 0 || isPaused.value) return

  const now = new Date().toLocaleTimeString()

  newProcessInfos.forEach(newProcess => {
    const oldProcess = oldProcessInfos.find(p => p.processId === newProcess.processId)

    if (!oldProcess) {
      // 新进程
      addEvent({
        time: now,
        type: 'info',
        eventType: '进程启动',
        description: `${newProcess.processName} (PID: ${newProcess.processId})`
      })
    } else {
      // 检测连接变化
      const newConnections = newProcess.connections.filter(newConn =>
        !oldProcess.connections.some(oldConn =>
          oldConn.localEndpoint.address === newConn.localEndpoint.address &&
          oldConn.localEndpoint.port === newConn.localEndpoint.port &&
          oldConn.remoteEndpoint.address === newConn.remoteEndpoint.address &&
          oldConn.remoteEndpoint.port === newConn.remoteEndpoint.port
        )
      )

      newConnections.forEach(conn => {
        addEvent({
          time: now,
          type: 'success',
          eventType: '连接建立',
          description: `${newProcess.processName} → ${conn.remoteEndpoint.address}:${conn.remoteEndpoint.port}`
        })
      })

      // 检测连接断开
      const closedConnections = oldProcess.connections.filter(oldConn =>
        !newProcess.connections.some(newConn =>
          oldConn.localEndpoint.address === newConn.localEndpoint.address &&
          oldConn.localEndpoint.port === newConn.localEndpoint.port &&
          oldConn.remoteEndpoint.address === newConn.remoteEndpoint.address &&
          oldConn.remoteEndpoint.port === newConn.remoteEndpoint.port
        )
      )

      closedConnections.forEach(conn => {
        addEvent({
          time: now,
          type: 'warning',
          eventType: '连接断开',
          description: `${newProcess.processName} ✗ ${conn.remoteEndpoint.address}:${conn.remoteEndpoint.port}`
        })
      })
    }
  })

  // 检测进程结束
  oldProcessInfos.forEach(oldProcess => {
    if (!newProcessInfos.find(p => p.processId === oldProcess.processId)) {
      addEvent({
        time: now,
        type: 'error',
        eventType: '进程结束',
        description: `${oldProcess.processName} (PID: ${oldProcess.processId})`
      })
    }
  })
}, { deep: true })

// 监听内部事件变化，标记新事件
watch(internalEvents, (newEvents, oldEvents) => {
  const oldIds = new Set(oldEvents?.map(e => e.id) || [])
  displayEvents.value = newEvents.map(e => ({
    ...e,
    isNew: !oldIds.has(e.id)
  }))

  // 500ms后移除新事件标记
  setTimeout(() => {
    displayEvents.value = displayEvents.value.map(e => ({ ...e, isNew: false }))
  }, 500)
}, { deep: true })

// 事件处理函数
const handleTogglePause = () => {
  isPaused.value = !isPaused.value
}

const handleClearEvents = () => {
  internalEvents.value = []
}
</script>

<style scoped>
/* 时间轴容器 */
.timeline-area {
  background: var(--bg-glass);
  backdrop-filter: var(--backdrop-blur);
  border-top: 1px solid var(--border-primary);
  overflow: hidden;
  display: flex;
  flex-direction: column;
  flex-shrink: 0;
}

/* 头部 */
.timeline-header {
  padding: 16px 24px;
  border-bottom: 1px solid var(--border-secondary);
  display: flex;
  align-items: center;
  justify-content: space-between;
  background: var(--bg-card);
  flex-shrink: 0;
}

.timeline-title {
  font-size: 16px;
  font-weight: 600;
  color: var(--text-secondary);
  margin: 0;
  display: flex;
  align-items: center;
  gap: 8px;
}

.title-icon {
  font-size: 16px;
}

.event-count {
  font-size: 12px;
  color: var(--text-muted);
  font-weight: 400;
  margin-left: 8px;
}

.timeline-controls {
  display: flex;
  gap: 8px;
}

/* 内容区域 */
.timeline-content {
  flex: 1;
  overflow-y: auto;
  padding: 12px 24px;
  min-height: 0;
}

.events-container {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

/* 事件项 */
.event-item {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 12px 16px;
  background: var(--bg-card);
  border-radius: 8px;
  border-left: 3px solid transparent;
  transition: var(--transition);
  position: relative;
}

.event-item:hover {
  background: var(--bg-hover);
}

.event-item.event-new {
  animation: slideIn 0.3s ease-out;
}

/* 事件类型样式 */
.event-item.event-info {
  border-left-color: var(--accent-primary);
}

.event-item.event-warning {
  border-left-color: var(--accent-warning);
}

.event-item.event-success {
  border-left-color: var(--accent-success);
}

.event-item.event-error {
  border-left-color: var(--accent-error);
}

/* 事件内容 */
.event-time {
  font-size: 11px;
  color: var(--text-muted);
  min-width: 70px;
  font-family: 'JetBrains Mono', 'Fira Code', monospace;
}

.event-content {
  flex: 1;
}

.event-type {
  font-size: 13px;
  font-weight: 500;
  color: var(--text-secondary);
  line-height: 1;
}

.event-desc {
  font-size: 12px;
  color: var(--text-muted);
  margin-top: 2px;
}

/* 事件指示器 */
.event-indicator {
  width: 6px;
  height: 6px;
  border-radius: 50%;
  background: var(--text-disabled);
}

.event-item.event-info .event-indicator {
  background: var(--accent-primary);
  animation: pulse 2s infinite;
}

.event-item.event-warning .event-indicator {
  background: var(--accent-warning);
  animation: pulse 2s infinite;
}

.event-item.event-success .event-indicator {
  background: var(--accent-success);
  animation: pulse 2s infinite;
}

.event-item.event-error .event-indicator {
  background: var(--accent-error);
  animation: pulse 2s infinite;
}

/* 空状态 */
.empty-events {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 48px 24px;
  color: var(--text-muted);
}

.empty-icon {
  font-size: 48px;
  margin-bottom: 16px;
  opacity: 0.5;
}

.empty-text {
  font-size: 14px;
}

/* 动画 */
@keyframes slideIn {
  from {
    opacity: 0;
    transform: translateX(-20px);
  }
  to {
    opacity: 1;
    transform: translateX(0);
  }
}

@keyframes pulse {
  0% {
    box-shadow: 0 0 0 0 currentColor;
  }
  70% {
    box-shadow: 0 0 0 4px transparent;
  }
  100% {
    box-shadow: 0 0 0 0 transparent;
  }
}

/* 列表过渡动画 */
.event-list-enter-active,
.event-list-leave-active {
  transition: all 0.3s ease;
}

.event-list-enter-from {
  opacity: 0;
  transform: translateX(-20px);
}

.event-list-leave-to {
  opacity: 0;
  transform: translateX(20px);
}

.event-list-move {
  transition: transform 0.3s ease;
}
</style>
