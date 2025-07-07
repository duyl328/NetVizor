<template>
  <div class="unified-connections-container">
    <!-- 吸顶元素容器 -->
    <Transition name="sticky-fade">
      <div
        v-show="shouldShowSticky && stickyItem"
        class="sticky-header"
        :style="stickyStyle"
      >
        <div class="sticky-content">
          <template v-if="stickyItem?.isCollapsedIndicator">
            <!-- 折叠状态的吸顶 -->
            <div class="sticky-collapsed">
              <div class="sticky-icon">
                <n-icon :component="FolderOutline" size="20" />
              </div>
              <div class="sticky-info">
                <div class="sticky-title">{{ stickyItem.processName }}</div>
                <div class="sticky-subtitle">{{ stickyItem.collapsedCount }} 个连接已折叠</div>
              </div>
              <n-button
                size="small"
                type="primary"
                @click="expandSection(stickyItem.processIndex)"
              >
                <template #icon>
                  <n-icon :component="ChevronDownOutline" />
                </template>
                展开
              </n-button>
            </div>
          </template>

          <template v-else-if="stickyItem?.isProcess">
            <!-- 进程标题的吸顶 -->
            <div class="sticky-process">
              <div class="sticky-icon">
                <n-icon :component="DesktopOutline" size="20" />
              </div>
              <div class="sticky-info">
                <div class="sticky-title">
                  {{ stickyItem.processName }}
                  <span class="sticky-pid">PID: {{ stickyItem.processId }}</span>
                </div>
                <div class="sticky-stats">
                  <span class="stat-item">
                    <n-icon :component="HardwareChipOutline" size="14" />
                    {{ formatMemory(stickyItem.useMemory) }}
                  </span>
                  <span class="stat-item">
                    <n-icon :component="GitBranchOutline" size="14" />
                    {{ stickyItem.threadCount }} 线程
                  </span>
                  <span class="stat-item">
                    <n-icon :component="LinkOutline" size="14" />
                    {{ stickyItem.connectionCount }} 连接
                  </span>
                </div>
              </div>
              <n-button
                size="small"
                quaternary
                circle
                @click="toggleCollapseWithScrollAdjust(stickyItem.processIndex)"
              >
                <template #icon>
                  <n-icon
                    :component="collapsedSections.has(stickyItem.processIndex) ? ChevronDownOutline : ChevronUpOutline"
                    size="18"
                  />
                </template>
              </n-button>
            </div>
          </template>
        </div>
      </div>
    </Transition>

    <!-- 虚拟列表 -->
    <n-virtual-list
      ref="virtualListRef"
      class="connections-list"
      :style="{ maxHeight: containerHeight }"
      :item-size="itemHeight"
      :items="displayItems"
      @scroll="handleScroll"
      item-resizable
    >
      <template #default="{ item, index }">
        <div
          :key="item.key"
          class="list-item"
          :class="{
            'is-process': item.isProcess,
            'is-connection': item.isConnection,
            'is-collapsed-indicator': item.isCollapsedIndicator,
            'is-hidden': shouldHideItem(item, index),
          }"
          :style="{ height: `${itemHeight}px` }"
        >
          <template v-if="item.isCollapsedIndicator">
            <!-- 折叠状态指示器 -->
            <div class="collapsed-indicator">
              <div class="indicator-left">
                <n-icon :component="FolderOutline" size="18" />
                <span class="indicator-text">
                  {{ item.processName }} - {{ item.collapsedCount }} 个连接已折叠
                </span>
              </div>
              <n-button
                size="small"
                type="primary"
                ghost
                @click="expandSection(item.processIndex)"
              >
                展开查看
              </n-button>
            </div>
          </template>

          <template v-else-if="item.isProcess">
            <!-- 进程标题 -->
            <div class="process-header">
              <div class="process-icon">
                <n-icon :component="DesktopOutline" size="22" />
              </div>
              <div class="process-info">
                <div class="process-title">
                  <span class="process-name">{{ item.processName }}</span>
                  <n-tag size="small" :bordered="false">PID: {{ item.processId }}</n-tag>
                </div>
                <div class="process-stats">
                  <span class="stat">
                    <n-icon :component="HardwareChipOutline" size="12" />
                    {{ formatMemory(item.useMemory) }}
                  </span>
                  <span class="stat">
                    <n-icon :component="GitBranchOutline" size="12" />
                    {{ item.threadCount }} 线程
                  </span>
                  <span class="stat">
                    <n-icon :component="LinkOutline" size="12" />
                    {{ item.connectionCount }} 连接
                  </span>
                </div>
              </div>
              <n-button
                size="small"
                quaternary
                @click="toggleCollapse(item.processIndex)"
              >
                <template #icon>
                  <n-icon
                    :component="collapsedSections.has(item.processIndex) ? ChevronDownOutline : ChevronUpOutline"
                    size="16"
                  />
                </template>
                {{ collapsedSections.has(item.processIndex) ? '展开' : '折叠' }}
              </n-button>
            </div>
          </template>

          <template v-else-if="item.isConnection">
            <!-- 连接详情 -->
            <div class="connection-item">
              <div class="connection-icon">
                <n-icon
                  :component="item.protocol === 0 ? LinkOutline : WifiOutline"
                  size="16"
                />
              </div>
              <div class="connection-main">
                <div class="connection-endpoints">
                  <span class="endpoint local">{{ item.localEndpoint.address }}:{{ item.localEndpoint.port }}</span>
                  <n-icon :component="ArrowForwardOutline" size="14" class="arrow" />
                  <span class="endpoint remote">{{ item.remoteEndpoint.address }}:{{ item.remoteEndpoint.port }}</span>
                  <n-tag
                    size="tiny"
                    :type="item.protocol === 0 ? 'info' : 'warning'"
                    :bordered="false"
                  >
                    {{ item.protocol === 0 ? 'TCP' : 'UDP' }}
                  </n-tag>
                </div>
                <div class="connection-meta">
                  <n-tag
                    size="tiny"
                    :type="getConnectionStatusType(item.state)"
                    :bordered="false"
                  >
                    {{ getConnectionStatus(item.state) }}
                  </n-tag>
                  <span class="traffic">
                    <n-icon :component="ArrowUpOutline" size="12" />
                    {{ formatBytes(item.bytesSent) }}
                    <n-icon :component="ArrowDownOutline" size="12" />
                    {{ formatBytes(item.bytesReceived) }}
                  </span>
                  <span class="activity" :class="{ active: item.isActive }">
                    <n-icon :component="item.isActive ? PulseOutline : PauseOutline" size="12" />
                    {{ item.isActive ? '活跃' : '空闲' }}
                  </span>
                </div>
              </div>
            </div>
          </template>
        </div>
      </template>
    </n-virtual-list>

    <!-- 无数据提示 -->
    <Transition name="fade">
      <div v-if="displayItems.length === 0" class="empty-state">
        <n-icon :component="CloudOfflineOutline" size="64" />
        <h3>暂无网络连接数据</h3>
        <p>请检查数据源是否正确加载</p>
      </div>
    </Transition>
  </div>
</template>

<script setup lang="ts">
import { ap } from '@/json/test.js'
import { ref, computed, nextTick, onMounted, onUnmounted } from 'vue'
import { NVirtualList, NButton, NIcon, NTag } from 'naive-ui'
import {
  FolderOutline,
  ChevronDownOutline,
  ChevronUpOutline,
  DesktopOutline,
  HardwareChipOutline,
  GitBranchOutline,
  LinkOutline,
  WifiOutline,
  ArrowForwardOutline,
  ArrowUpOutline,
  ArrowDownOutline,
  PulseOutline,
  PauseOutline,
  CloudOfflineOutline,
} from '@vicons/ionicons5'

// Props for container customization
const props = defineProps({
  height: {
    type: String,
    default: '500px'
  },
  itemHeight: {
    type: Number,
    default: 64
  }
})

// 容器高度
const containerHeight = computed(() => props.height)
const itemHeight = computed(() => props.itemHeight)

// 其他响应式数据
const virtualListRef = ref()
const scrollTop = ref(0)
const scrollbarWidth = ref(0)
const collapsedSections = ref(new Set<number>())
const beforeCollapseState = ref<{
  scrollTop: number
  visibleItemKey: string
  offsetInItem: number
} | null>(null)

// 检测滚动条宽度
const getScrollbarWidth = () => {
  const outer = document.createElement('div')
  outer.style.visibility = 'hidden'
  outer.style.overflow = 'scroll'
  outer.style.msOverflowStyle = 'scrollbar'
  document.body.appendChild(outer)

  const inner = document.createElement('div')
  outer.appendChild(inner)

  const width = outer.offsetWidth - inner.offsetWidth
  outer.parentNode?.removeChild(outer)

  return width
}

// 生命周期
onMounted(() => {
  scrollbarWidth.value = getScrollbarWidth()
  window.addEventListener('resize', handleResize)
})

onUnmounted(() => {
  window.removeEventListener('resize', handleResize)
})

// 响应式处理
const handleResize = () => {
  scrollbarWidth.value = getScrollbarWidth()
}

// 计算吸顶元素的样式
const stickyStyle = computed(() => ({
  top: '0px',
  right: `${scrollbarWidth.value}px`,
}))

// 格式化函数
const formatMemory = (bytes: number) => {
  const mb = bytes / (1024 * 1024)
  return mb >= 1 ? `${mb.toFixed(1)}MB` : `${(bytes / 1024).toFixed(1)}KB`
}

const formatBytes = (bytes: number) => {
  if (bytes === 0) return '0B'
  const k = 1024
  const sizes = ['B', 'KB', 'MB', 'GB']
  const i = Math.floor(Math.log(bytes) / Math.log(k))
  return `${(bytes / Math.pow(k, i)).toFixed(1)}${sizes[i]}`
}

const getConnectionStatus = (state: number) => {
  const states = {
    0: '关闭',
    1: '监听',
    2: '已建立',
    3: '等待关闭',
    4: '关闭等待',
  }
  return states[state as keyof typeof states] || '未知'
}

const getConnectionStatusType = (state: number) => {
  const types = {
    0: 'error',
    1: 'info',
    2: 'success',
    3: 'warning',
    4: 'warning',
  }
  return types[state as keyof typeof types] || 'default'
}

// 数据处理
const originalItems = computed(() => {
  const result: any[] = []

  if (!ap || !Array.isArray(ap) || ap.length === 0) {
    return result
  }

  ap.forEach((process, processIndex) => {
    if (!process) return

    result.push({
      key: `process-${processIndex}`,
      isProcess: true,
      processIndex,
      processName: process.processName || 'Unknown Process',
      processId: process.processId || 0,
      useMemory: process.useMemory || 0,
      threadCount: process.threadCount || 0,
      connectionCount: process.connections?.length || 0,
      startTime: process.startTime,
      mainModulePath: process.mainModulePath,
    })

    if (process.connections && Array.isArray(process.connections)) {
      process.connections.forEach((connection, connectionIndex) => {
        result.push({
          key: `connection-${processIndex}-${connectionIndex}`,
          isConnection: true,
          processIndex,
          connectionIndex,
          processName: process.processName || 'Unknown Process',
          ...connection,
        })
      })
    }
  })

  return result
})

// 显示数据
const displayItems = computed(() => {
  const result: any[] = []
  const processGroups: { [key: number]: any[] } = {}

  originalItems.value.forEach((item) => {
    const processIndex = item.processIndex
    if (!processGroups[processIndex]) {
      processGroups[processIndex] = []
    }
    processGroups[processIndex].push(item)
  })

  Object.keys(processGroups).forEach((processIndexStr) => {
    const processIndex = parseInt(processIndexStr)
    const group = processGroups[processIndex]
    const processItem = group.find((item) => item.isProcess)
    const connectionItems = group.filter((item) => item.isConnection)

    if (collapsedSections.value.has(processIndex)) {
      result.push({
        key: `collapsed-${processIndex}`,
        isCollapsedIndicator: true,
        processIndex,
        processName: processItem.processName,
        collapsedCount: connectionItems.length,
      })
    } else {
      result.push(processItem)
      connectionItems.forEach((connection) => {
        result.push(connection)
      })
    }
  })

  return result
})

// 吸顶逻辑
const getCurrentIndex = () => {
  return Math.floor(scrollTop.value / itemHeight.value)
}

const stickyItem = computed(() => {
  if (displayItems.value.length === 0) return null

  const currentIndex = getCurrentIndex()

  for (let i = currentIndex; i >= 0; i--) {
    const item = displayItems.value[i]
    if (item && (item.isProcess || item.isCollapsedIndicator)) {
      return {
        ...item,
        originalIndex: i,
      }
    }
  }

  return null
})

const shouldShowSticky = computed(() => {
  if (!stickyItem.value) return false
  const currentIndex = getCurrentIndex()
  const stickyIndex = stickyItem.value.originalIndex
  return currentIndex > stickyIndex
})

const shouldHideItem = (item: any, index: number) => {
  if (!stickyItem.value || !shouldShowSticky.value) return false
  return item.key === stickyItem.value.key
}

// 交互逻辑
const handleScroll = (e: Event) => {
  const target = e.target as HTMLElement
  scrollTop.value = target.scrollTop
}

const getFirstVisibleItem = () => {
  const currentIndex = Math.floor(scrollTop.value / itemHeight.value)
  const items = displayItems.value

  if (currentIndex < items.length) {
    return {
      item: items[currentIndex],
      index: currentIndex,
      offsetInViewport: scrollTop.value % itemHeight.value,
    }
  }

  return null
}

const toggleCollapseWithScrollAdjust = async (processIndex: number) => {
  if (!collapsedSections.value.has(processIndex)) {
    const visibleItem = getFirstVisibleItem()
    if (visibleItem) {
      beforeCollapseState.value = {
        scrollTop: scrollTop.value,
        visibleItemKey: visibleItem.item.key,
        offsetInItem: visibleItem.offsetInViewport,
      }
    }

    collapsedSections.value.add(processIndex)
    collapsedSections.value = new Set(collapsedSections.value)

    await nextTick()

    if (beforeCollapseState.value && visibleItem) {
      const newItems = displayItems.value
      const newIndex = newItems.findIndex((item) => item.key === visibleItem.item.key)

      if (newIndex !== -1) {
        const newScrollTop = newIndex * itemHeight.value - visibleItem.offsetInViewport
        virtualListRef.value?.scrollTo({ top: Math.max(0, newScrollTop) })
      }
    }
  } else {
    expandSection(processIndex)
  }
}

const toggleCollapse = (processIndex: number) => {
  if (collapsedSections.value.has(processIndex)) {
    collapsedSections.value.delete(processIndex)
  } else {
    collapsedSections.value.add(processIndex)
  }
  collapsedSections.value = new Set(collapsedSections.value)
}

const expandSection = (processIndex: number) => {
  collapsedSections.value.delete(processIndex)
  collapsedSections.value = new Set(collapsedSections.value)
}
</script>

<style scoped>
.unified-connections-container {
  position: relative;
  height: 100%;
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
  background: var(--bg-primary);
  border-radius: 8px;
  overflow: hidden;
}

/* 吸顶元素 */
.sticky-header {
  position: absolute;
  left: 0;
  z-index: 10;
  background: var(--bg-overlay);
  backdrop-filter: var(--backdrop-blur);
  border-bottom: 1px solid var(--border-primary);
  box-shadow: var(--shadow-md);
}

.sticky-content {
  padding: 12px 16px;
}

.sticky-collapsed,
.sticky-process {
  display: flex;
  align-items: center;
  gap: 12px;
}

.sticky-icon {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 36px;
  height: 36px;
  background: var(--bg-quaternary);
  border-radius: 8px;
  color: var(--accent-primary);
  flex-shrink: 0;
}

.sticky-info {
  flex: 1;
  min-width: 0;
}

.sticky-title {
  font-size: 14px;
  font-weight: 600;
  color: var(--text-primary);
  display: flex;
  align-items: center;
  gap: 8px;
}

.sticky-pid {
  font-size: 12px;
  font-weight: 400;
  color: var(--text-muted);
}

.sticky-subtitle {
  font-size: 12px;
  color: var(--text-muted);
  margin-top: 2px;
}

.sticky-stats {
  display: flex;
  align-items: center;
  gap: 16px;
  margin-top: 4px;
  font-size: 12px;
  color: var(--text-tertiary);
}

.stat-item {
  display: flex;
  align-items: center;
  gap: 4px;
}

/* 虚拟列表 */
.connections-list {
  background: var(--bg-secondary);
}

/* 列表项 */
.list-item {
  display: flex;
  align-items: center;
  padding: 12px 16px;
  background: var(--bg-primary);
  border-bottom: 1px solid var(--border-secondary);
  transition: var(--transition-fast);
}

.list-item:hover {
  background: var(--bg-hover);
}

.is-hidden {
  visibility: hidden;
}

.is-process {
  background: var(--bg-tertiary);
  border-left: 3px solid var(--accent-primary);
}

.is-connection {
  padding-left: 32px;
}

.is-collapsed-indicator {
  background: var(--bg-quaternary);
  border-left: 3px solid var(--text-muted);
}

/* 折叠指示器 */
.collapsed-indicator {
  display: flex;
  align-items: center;
  justify-content: space-between;
  width: 100%;
  color: var(--text-secondary);
}

.indicator-left {
  display: flex;
  align-items: center;
  gap: 8px;
  flex: 1;
  min-width: 0;
}

.indicator-text {
  font-size: 13px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

/* 进程头部 */
.process-header {
  display: flex;
  align-items: center;
  width: 100%;
  gap: 12px;
}

.process-icon {
  color: var(--accent-primary);
  flex-shrink: 0;
}

.process-info {
  flex: 1;
  min-width: 0;
}

.process-title {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 4px;
}

.process-name {
  font-size: 14px;
  font-weight: 600;
  color: var(--text-primary);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.process-stats {
  display: flex;
  align-items: center;
  gap: 16px;
  font-size: 12px;
  color: var(--text-tertiary);
}

.stat {
  display: flex;
  align-items: center;
  gap: 4px;
}

/* 连接项 */
.connection-item {
  display: flex;
  align-items: center;
  width: 100%;
  gap: 12px;
}

.connection-icon {
  color: var(--text-quaternary);
  flex-shrink: 0;
}

.connection-main {
  flex: 1;
  min-width: 0;
}

.connection-endpoints {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 6px;
  font-size: 13px;
  font-family: 'SF Mono', 'Monaco', 'Inconsolata', 'Fira Code', monospace;
}

.endpoint {
  font-weight: 500;
}

.endpoint.local {
  color: var(--accent-success);
}

.endpoint.remote {
  color: var(--accent-primary);
}

.arrow {
  color: var(--text-quaternary);
}

.connection-meta {
  display: flex;
  align-items: center;
  gap: 12px;
  font-size: 11px;
}

.traffic {
  display: flex;
  align-items: center;
  gap: 4px;
  color: var(--text-tertiary);
  font-family: 'SF Mono', 'Monaco', 'Inconsolata', 'Fira Code', monospace;
}

.activity {
  display: flex;
  align-items: center;
  gap: 4px;
  color: var(--text-muted);
  transition: var(--transition-fast);
}

.activity.active {
  color: var(--accent-success);
}

/* 空状态 */
.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  height: 100%;
  padding: 48px;
  text-align: center;
  color: var(--text-muted);
}

.empty-state h3 {
  margin: 16px 0 8px;
  font-size: 18px;
  font-weight: 600;
  color: var(--text-secondary);
}

.empty-state p {
  margin: 0;
  font-size: 14px;
}

/* 动画 */
.sticky-fade-enter-active,
.sticky-fade-leave-active {
  transition: opacity 0.2s ease, transform 0.2s ease;
}

.sticky-fade-enter-from {
  opacity: 0;
  transform: translateY(-10px);
}

.sticky-fade-leave-to {
  opacity: 0;
  transform: translateY(-5px);
}

.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.3s ease;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}

/* 响应式设计 */
@media (max-width: 768px) {
  .sticky-stats,
  .process-stats {
    flex-wrap: wrap;
    gap: 8px;
  }

  .connection-endpoints {
    flex-wrap: wrap;
  }

  .connection-meta {
    flex-wrap: wrap;
    gap: 8px;
  }

  .list-item {
    padding: 10px 12px;
  }

  .is-connection {
    padding-left: 20px;
  }
}

@media (max-width: 480px) {
  .sticky-content {
    padding: 10px 12px;
  }

  .process-title {
    flex-direction: column;
    align-items: flex-start;
    gap: 4px;
  }

  .connection-endpoints {
    font-size: 12px;
  }
}

/* Naive UI 组件样式覆盖 */
:deep(.n-button) {
  --n-border: 1px solid var(--border-tertiary);
  --n-border-hover: 1px solid var(--border-hover);
  --n-color: var(--bg-card);
  --n-color-hover: var(--bg-hover);
  --n-text-color: var(--text-primary);
  --n-text-color-hover: var(--text-primary);
}

:deep(.n-tag) {
  --n-color: var(--bg-quaternary);
  --n-text-color: var(--text-secondary);
  --n-border: none;
}

:deep(.n-virtual-list) {
  --n-color: transparent;
}

/* 滚动条样式 */
:deep(.n-virtual-list__content)::-webkit-scrollbar {
  width: 8px;
}

:deep(.n-virtual-list__content)::-webkit-scrollbar-track {
  background: var(--scrollbar-track-bg);
}

:deep(.n-virtual-list__content)::-webkit-scrollbar-thumb {
  background: var(--scrollbar-thumb-bg);
  border-radius: 4px;
  transition: var(--transition-fast);
}

:deep(.n-virtual-list__content)::-webkit-scrollbar-thumb:hover {
  background: var(--scrollbar-thumb-bg-hover);
}

:deep(.n-virtual-list__content)::-webkit-scrollbar-thumb:active {
  background: var(--scrollbar-thumb-bg-active);
}
</style>
