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
          @click="$emit('pause')"
        >
          {{ isPaused ? '继续' : '暂停' }}
        </n-button>
        <n-button size="small" ghost @click="$emit('clear')">清空</n-button>
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

// Props
const props = defineProps<{
  height: number
  events: Array<{
    id: number
    time: string
    type: 'info' | 'warning' | 'success' | 'error'
    eventType: string
    description: string
  }>
  isPaused: boolean
}>()

// Emits
const emit = defineEmits<{
  pause: []
  clear: []
}>()

// 显示的事件（添加动画标记）
const displayEvents = ref(props.events.map(e => ({ ...e, isNew: false })))

// 监听事件变化，标记新事件
watch(() => props.events, (newEvents, oldEvents) => {
  if (!props.isPaused) {
    const oldIds = new Set(oldEvents?.map(e => e.id) || [])
    displayEvents.value = newEvents.map(e => ({
      ...e,
      isNew: !oldIds.has(e.id)
    }))

    // 500ms后移除新事件标记
    setTimeout(() => {
      displayEvents.value = displayEvents.value.map(e => ({ ...e, isNew: false }))
    }, 500)
  }
}, { deep: true })
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
