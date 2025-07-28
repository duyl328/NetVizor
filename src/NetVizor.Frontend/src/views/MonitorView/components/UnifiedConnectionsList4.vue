<template>
  <div class="unified-connections-container">
    <!-- 吸顶元素容器 -->
    <Transition name="sticky-slide">
      <div
        v-show="shouldShowSticky && stickyItem"
        class="sticky-header"
        :style="stickyStyle"
      >
        <div class="sticky-content">
          <template v-if="stickyItem?.isCollapsedIndicator">
            <!-- 折叠状态的吸顶 -->
            <div class="sticky-collapsed">
              <div class="sticky-icon collapsed">
                <n-icon :component="FolderOutline" size="20" />
              </div>
              <div class="sticky-info">
                <div class="sticky-title">{{ stickyItem.processName }}</div>
                <div class="sticky-subtitle">
                  <n-icon :component="LayersOutline" size="14" />
                  {{ stickyItem.collapsedCount }} 个连接已折叠
                </div>
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
              <div class="sticky-icon process">
                <n-icon :component="DesktopOutline" size="20" />
              </div>
              <div class="sticky-info">
                <div class="sticky-title">
                  {{ stickyItem.processName }}
                  <n-tag class="sticky-pid" size="tiny" round>
                    PID: {{ stickyItem.processId }}
                  </n-tag>
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
                  <span class="stat-item connections">
                    <n-icon :component="GitNetworkOutline" size="14" />
                    {{ stickyItem.connectionCount }} 连接
                  </span>
                </div>
              </div>
              <div class="sticky-actions">
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
            'is-active': item.isConnection && item.isActive,
          }"
          :style="{ minHeight: `${itemHeight}px` }"
        >
          <template v-if="item.isCollapsedIndicator">
            <!-- 折叠状态指示器 -->
            <div class="collapsed-indicator">
              <div class="indicator-left">
                <div class="collapse-icon">
                  <n-icon :component="LayersOutline" size="18" />
                </div>
                <div class="indicator-content">
                  <span class="indicator-title">{{ item.processName }}</span>
                  <span class="indicator-count">{{ item.collapsedCount }} 个连接已折叠</span>
                </div>
              </div>
              <n-button
                size="small"
                type="primary"
                ghost
                @click="expandSection(item.processIndex)"
              >
                <template #icon>
                  <n-icon :component="ChevronDownOutline" size="16" />
                </template>
                展开查看
              </n-button>
            </div>
          </template>

          <template v-else-if="item.isProcess">
            <!-- 进程标题 -->
            <div class="process-header">
              <div class="process-icon-wrapper">
                <n-icon :component="DesktopOutline" size="24" />
              </div>
              <div class="process-info">
                <div class="process-title">
                  <h4 class="process-name">{{ item.processName }}</h4>
                  <n-tag type="info" size="small" round>
                    PID: {{ item.processId }}
                  </n-tag>
                  <n-tag
                    v-if="getProcessStatus(item).show"
                    :type="getProcessStatus(item).type"
                    size="small"
                    round
                  >
                    {{ getProcessStatus(item).text }}
                  </n-tag>
                </div>
                <div class="process-meta">
                  <span class="meta-item">
                    <n-icon :component="HardwareChipOutline" size="14" />
                    <span>内存: {{ formatMemory(item.useMemory) }}</span>
                  </span>
                  <span class="meta-item">
                    <n-icon :component="GitBranchOutline" size="14" />
                    <span>{{ item.threadCount }} 线程</span>
                  </span>
                  <span class="meta-item primary">
                    <n-icon :component="GitNetworkOutline" size="14" />
                    <span>{{ item.connectionCount }} 个连接</span>
                  </span>
                  <span v-if="item.startTime" class="meta-item">
                    <n-icon :component="TimeOutline" size="14" />
                    <span>运行时长: {{ formatDuration(new Date(item.startTime)) }}</span>
                  </span>
                </div>
              </div>
              <div class="process-actions">
                <n-button
                  size="small"
                  quaternary
                  @click="toggleCollapse(item.processIndex)"
                  class="collapse-btn"
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
            </div>
          </template>

          <template v-else-if="item.isConnection">
            <!-- 连接详情 -->
            <div class="connection-item">
              <div class="connection-status">
                <div
                  class="status-indicator"
                  :class="item.isActive ? 'active' : 'inactive'"
                  :title="item.isActive ? '活跃' : '空闲'"
                ></div>
              </div>

              <div class="connection-main">
                <div class="connection-header">
                  <div class="connection-endpoints">
                    <div class="endpoint-item local">
                      <n-icon :component="LocationOutline" size="14" />
                      <span class="endpoint-address">{{ formatEndpoint(item.localEndpoint) }}</span>
                    </div>
                    <n-icon :component="ArrowForwardOutline" size="16" class="connection-arrow" />
                    <div class="endpoint-item remote">
                      <n-icon :component="GlobeOutline" size="14" />
                      <span class="endpoint-address">{{ formatEndpoint(item.remoteEndpoint) }}</span>
                    </div>
                  </div>

                  <div class="connection-tags">
                    <n-tag
                      :type="item.protocol === 0 ? 'info' : 'warning'"
                      size="tiny"
                      round
                    >
                      {{ item.protocol === 0 ? 'TCP' : 'UDP' }}
                    </n-tag>
                    <n-tag
                      :type="getConnectionStateType(item.state, item.isActive)"
                      size="tiny"
                      round
                    >
                      {{ getConnectionStatus(item.state) }}
                    </n-tag>
                  </div>
                </div>

                <div class="connection-stats">
                  <div class="traffic-info">
                    <div class="traffic-item upload">
                      <n-icon :component="ArrowUpOutline" size="12" />
                      <span>{{ formatBytes(item.bytesSent) }}</span>
                    </div>
                    <div class="traffic-item download">
                      <n-icon :component="ArrowDownOutline" size="12" />
                      <span>{{ formatBytes(item.bytesReceived) }}</span>
                    </div>
                  </div>

                  <div class="connection-meta-info">
                    <span v-if="item.startTime" class="meta-text">
                      <n-icon :component="TimeOutline" size="12" />
                      持续: {{ formatDuration(new Date(item.startTime)) }}
                    </span>
                    <span v-if="!item.isActive && item.lastActiveTime" class="meta-text inactive">
                      <n-icon :component="PauseCircleOutline" size="12" />
                      最后活跃: {{ getTimeSinceActive(new Date(item.lastActiveTime)) }}
                    </span>
                  </div>
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
        <div class="empty-icon">
          <n-icon :component="CloudOfflineOutline" size="64" />
        </div>
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
  GitNetworkOutline,
  LinkOutline,
  WifiOutline,
  ArrowForwardOutline,
  ArrowUpOutline,
  ArrowDownOutline,
  PulseOutline,
  PauseOutline,
  CloudOfflineOutline,
  LayersOutline,
  LocationOutline,
  GlobeOutline,
  TimeOutline,
  PauseCircleOutline,
} from '@vicons/ionicons5'

// Props for container customization
const props = defineProps({
  height: {
    type: String,
    default: '600px'
  },
  itemHeight: {
    type: Number,
    default: 80
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
  return mb >= 1 ? `${mb.toFixed(1)} MB` : `${(bytes / 1024).toFixed(1)} KB`
}

const formatBytes = (bytes: number) => {
  if (bytes === 0) return '0 B'
  const k = 1024
  const sizes = ['B', 'KB', 'MB', 'GB']
  const i = Math.floor(Math.log(bytes) / Math.log(k))
  return `${(bytes / Math.pow(k, i)).toFixed(1)} ${sizes[i]}`
}

const formatEndpoint = (endpoint:unknown) => {
  return `${endpoint.address}:${endpoint.port}`
}

const formatDuration = (start: Date, end?: Date): string => {
  const endTime = end || new Date()
  const diff = endTime.getTime() - start.getTime()
  const hours = Math.floor(diff / 3600000)
  const minutes = Math.floor((diff % 3600000) / 60000)
  const seconds = Math.floor((diff % 60000) / 1000)

  if (hours > 0) {
    return `${hours}时${minutes}分`
  } else if (minutes > 0) {
    return `${minutes}分${seconds}秒`
  } else {
    return `${seconds}秒`
  }
}

const getTimeSinceActive = (lastActive: Date): string => {
  const diff = new Date().getTime() - lastActive.getTime()
  if (diff < 1000) return '刚刚'
  if (diff < 60000) return `${Math.floor(diff / 1000)}秒前`
  if (diff < 3600000) return `${Math.floor(diff / 60000)}分钟前`
  return `${Math.floor(diff / 3600000)}小时前`
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

const getConnectionStateType = (state: number, isActive: boolean) => {
  if (!isActive) return 'default'
  const types = {
    0: 'error',
    1: 'info',
    2: 'success',
    3: 'warning',
    4: 'warning',
  }
  return types[state as keyof typeof types] || 'default'
}

const getProcessStatus = (processunknown) => {
  const hasActiveConnections = process.connections?.some((cunknown) => c.isActive)
  if (hasActiveConnections) {
    return { type: 'success', text: '活跃', show: true }
  }
  return { type: 'warning', text: '空闲', show: false }
}

// 数据处理
const originalItems = computed(() => {
  const result:unknown[] = []

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
      connections: process.connections || [],
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
  const result:unknown[] = []
  const processGroups: { [key: number]:unknown[] } = {}

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

const shouldHideItem = (item:unknown, index: number) => {
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
  border-radius: 12px;
  overflow: hidden;
  box-shadow: var(--shadow-md);
}

/* 吸顶元素 */
.sticky-header {
  position: absolute;
  left: 0;
  z-index: 10;
  background: var(--bg-sticky);
  backdrop-filter: blur(12px);
  border-bottom: 1px solid var(--border-primary);
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.08);
}

.sticky-content {
  padding: 12px 20px;
}

.sticky-collapsed,
.sticky-process {
  display: flex;
  align-items: center;
  gap: 16px;
}

.sticky-icon {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 40px;
  height: 40px;
  border-radius: 10px;
  flex-shrink: 0;
  transition: all 0.3s ease;
}

.sticky-icon.process {
  background: rgba(59, 130, 246, 0.1);
  color: #3b82f6;
}

.sticky-icon.collapsed {
  background: rgba(168, 85, 247, 0.1);
  color: #a855f7;
}

.sticky-info {
  flex: 1;
  min-width: 0;
}

.sticky-title {
  font-size: 15px;
  font-weight: 600;
  color: var(--text-primary);
  display: flex;
  align-items: center;
  gap: 10px;
}

.sticky-pid {
  font-weight: 400;
}

.sticky-subtitle {
  font-size: 13px;
  color: var(--text-muted);
  margin-top: 2px;
  display: flex;
  align-items: center;
  gap: 4px;
}

.sticky-stats {
  display: flex;
  align-items: center;
  gap: 20px;
  margin-top: 4px;
  font-size: 13px;
  color: var(--text-secondary);
}

.stat-item {
  display: flex;
  align-items: center;
  gap: 6px;
}

.stat-item.connections {
  color: #3b82f6;
}

.sticky-actions {
  flex-shrink: 0;
}

/* 虚拟列表 */
.connections-list {
  background: var(--bg-secondary);
}

/* 列表项 */
.list-item {
  background: var(--bg-primary);
  border-bottom: 1px solid var(--border-secondary);
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
}

.list-item:hover {
  background: var(--bg-hover);
}

.is-hidden {
  visibility: hidden;
}

.is-process {
  background: var(--bg-process);
  border-left: 3px solid #3b82f6;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.05);
}

.is-connection {
  padding-left: 20px;
  border-left: 3px solid transparent;
}

.is-connection.is-active {
  background: var(--bg-active);
  border-left-color: #10b981;
}

.is-collapsed-indicator {
  background: var(--bg-collapsed);
  border-left: 3px solid #a855f7;
}

/* 折叠指示器 */
.collapsed-indicator {
  display: flex;
  align-items: center;
  justify-content: space-between;
  width: 100%;
  padding: 16px 20px;
}

.indicator-left {
  display: flex;
  align-items: center;
  gap: 12px;
  flex: 1;
  min-width: 0;
}

.collapse-icon {
  width: 36px;
  height: 36px;
  background: rgba(168, 85, 247, 0.1);
  border-radius: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #a855f7;
}

.indicator-content {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.indicator-title {
  font-size: 14px;
  font-weight: 600;
  color: var(--text-primary);
}

.indicator-count {
  font-size: 12px;
  color: var(--text-muted);
}

/* 进程头部 */
.process-header {
  display: flex;
  align-items: center;
  width: 100%;
  gap: 16px;
  padding: 16px 20px;
}

.process-icon-wrapper {
  width: 48px;
  height: 48px;
  background: rgba(59, 130, 246, 0.1);
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #3b82f6;
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
  margin-bottom: 6px;
  flex-wrap: wrap;
}

.process-name {
  font-size: 16px;
  font-weight: 600;
  color: var(--text-primary);
  margin: 0;
}

.process-meta {
  display: flex;
  align-items: center;
  gap: 20px;
  font-size: 13px;
  color: var(--text-secondary);
  flex-wrap: wrap;
}

.meta-item {
  display: flex;
  align-items: center;
  gap: 6px;
}

.meta-item.primary {
  color: #3b82f6;
}

.process-actions {
  flex-shrink: 0;
}

.collapse-btn {
  transition: all 0.3s ease;
}

.collapse-btn:hover {
  transform: translateY(-2px);
}

/* 连接项 */
.connection-item {
  display: flex;
  align-items: flex-start;
  width: 100%;
  gap: 12px;
  padding: 12px 20px 12px 40px;
}

.connection-status {
  display: flex;
  align-items: center;
  padding-top: 8px;
  flex-shrink: 0;
}

.status-indicator {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  transition: all 0.3s ease;
}

.status-indicator.active {
  background: #10b981;
  box-shadow: 0 0 8px rgba(16, 185, 129, 0.6);
  animation: pulse 2s infinite;
}

.status-indicator.inactive {
  background: #d1d5db;
}

@keyframes pulse {
  0% {
    box-shadow: 0 0 0 0 rgba(16, 185, 129, 0.6);
  }
  70% {
    box-shadow: 0 0 0 6px rgba(16, 185, 129, 0);
  }
  100% {
    box-shadow: 0 0 0 0 rgba(16, 185, 129, 0);
  }
}

.connection-main {
  flex: 1;
  min-width: 0;
}

.connection-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 8px;
  gap: 12px;
}

.connection-endpoints {
  display: flex;
  align-items: center;
  gap: 12px;
  flex: 1;
  flex-wrap: wrap;
}

.endpoint-item {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 13px;
}

.endpoint-item.local {
  color: var(--text-success);
}

.endpoint-item.remote {
  color: var(--text-primary);
}

.endpoint-address {
  font-family: 'SF Mono', 'Monaco', 'Inconsolata', monospace;
  font-weight: 500;
}

.connection-arrow {
  color: var(--text-quaternary);
}

.connection-tags {
  display: flex;
  gap: 6px;
  flex-shrink: 0;
}

.connection-stats {
  display: flex;
  align-items: center;
  gap: 20px;
  font-size: 12px;
}

.traffic-info {
  display: flex;
  align-items: center;
  gap: 16px;
}

.traffic-item {
  display: flex;
  align-items: center;
  gap: 4px;
  font-weight: 500;
}

.traffic-item.upload {
  color: #ef4444;
}

.traffic-item.download {
  color: #22c55e;
}

.connection-meta-info {
  display: flex;
  align-items: center;
  gap: 16px;
  color: var(--text-muted);
}

.meta-text {
  display: flex;
  align-items: center;
  gap: 4px;
}

.meta-text.inactive {
  color: var(--text-quaternary);
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
}

.empty-icon {
  color: var(--text-quaternary);
  margin-bottom: 20px;
}

.empty-state h3 {
  margin: 0 0 8px;
  font-size: 20px;
  font-weight: 600;
  color: var(--text-secondary);
}

.empty-state p {
  margin: 0;
  font-size: 14px;
  color: var(--text-muted);
}

/* 动画 */
.sticky-slide-enter-active,
.sticky-slide-leave-active {
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
}

.sticky-slide-enter-from {
  opacity: 0;
  transform: translateY(-20px);
}

.sticky-slide-leave-to {
  opacity: 0;
  transform: translateY(-10px);
}

.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.3s ease;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}

/* CSS 变量 - 亮色主题 */
:root {
  --bg-primary: #ffffff;
  --bg-secondary: #f9fafb;
  --bg-hover: #f3f4f6;
  --bg-process: #f0f9ff;
  --bg-active: rgba(16, 185, 129, 0.05);
  --bg-collapsed: #faf5ff;
  --bg-sticky: rgba(255, 255, 255, 0.95);

  --border-primary: #e5e7eb;
  --border-secondary: #f3f4f6;

  --text-primary: #111827;
  --text-secondary: #4b5563;
  --text-muted: #6b7280;
  --text-quaternary: #9ca3af;
  --text-success: #059669;

  --shadow-md: 0 4px 6px -1px rgba(0, 0, 0, 0.1), 0 2px 4px -1px rgba(0, 0, 0, 0.06);

  --scrollbar-track-bg: #f3f4f6;
  --scrollbar-thumb-bg: #d1d5db;
  --scrollbar-thumb-bg-hover: #9ca3af;
  --scrollbar-thumb-bg-active: #6b7280;
}

/* 暗色主题 */
@media (prefers-color-scheme: dark) {
  :root {
    --bg-primary: #1f2937;
    --bg-secondary: #111827;
    --bg-hover: #374151;
    --bg-process: rgba(59, 130, 246, 0.1);
    --bg-active: rgba(16, 185, 129, 0.1);
    --bg-collapsed: rgba(168, 85, 247, 0.1);
    --bg-sticky: rgba(31, 41, 55, 0.95);

    --border-primary: #374151;
    --border-secondary: #1f2937;

    --text-primary: #f9fafb;
    --text-secondary: #d1d5db;
    --text-muted: #9ca3af;
    --text-quaternary: #6b7280;
    --text-success: #10b981;

    --shadow-md: 0 4px 6px -1px rgba(0, 0, 0, 0.3), 0 2px 4px -1px rgba(0, 0, 0, 0.2);

    --scrollbar-track-bg: #1f2937;
    --scrollbar-thumb-bg: #4b5563;
    --scrollbar-thumb-bg-hover: #6b7280;
    --scrollbar-thumb-bg-active: #9ca3af;
  }

  .sticky-header {
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.3);
  }

  .is-process {
    box-shadow: 0 1px 3px rgba(0, 0, 0, 0.2);
  }
}

/* 响应式设计 */
@media (max-width: 768px) {
  .sticky-content {
    padding: 10px 16px;
  }

  .process-header,
  .collapsed-indicator {
    padding: 12px 16px;
  }

  .connection-item {
    padding: 10px 16px 10px 32px;
  }

  .process-meta,
  .sticky-stats {
    gap: 12px;
  }

  .connection-stats {
    flex-direction: column;
    align-items: flex-start;
    gap: 8px;
  }

  .traffic-info {
    gap: 12px;
  }
}

@media (max-width: 480px) {
  .process-title {
    flex-direction: column;
    align-items: flex-start;
    gap: 4px;
  }

  .connection-endpoints {
    font-size: 12px;
  }

  .connection-header {
    flex-direction: column;
    gap: 8px;
  }

  .process-icon-wrapper {
    width: 40px;
    height: 40px;
  }

  .sticky-icon {
    width: 32px;
    height: 32px;
  }
}

/* Naive UI 组件样式覆盖 */
:deep(.n-button) {
  font-weight: 500;
}

:deep(.n-tag) {
  font-weight: 500;
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
  transition: background 0.3s ease;
}

:deep(.n-virtual-list__content)::-webkit-scrollbar-thumb:hover {
  background: var(--scrollbar-thumb-bg-hover);
}

:deep(.n-virtual-list__content)::-webkit-scrollbar-thumb:active {
  background: var(--scrollbar-thumb-bg-active);
}
</style>
