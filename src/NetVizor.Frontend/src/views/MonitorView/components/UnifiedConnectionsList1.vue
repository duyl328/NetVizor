<template>
  <div class="virtual-list-container">
    <!-- 吸顶元素 -->
    <div v-if="stickyItem && shouldShowSticky" class="sticky-item" :style="{ top: '0px' }">
      <div
        class="item sticky-content"
        :class="{ 'is-collapsed-indicator': stickyItem.isCollapsedIndicator }"
      >
        <template v-if="stickyItem.isCollapsedIndicator">
          <!-- 折叠状态的吸顶 -->
          <div class="collapsed-indicator">
            <span
            >📁 已折叠进程: {{ stickyItem.processName }} ({{
                stickyItem.collapsedCount
              }}个连接)</span
            >
            <button class="expand-btn" @click="expandSection(stickyItem.processIndex)">展开</button>
          </div>
        </template>
        <template v-else-if="stickyItem.isProcess">
          <!-- 进程标题的吸顶 -->
          <div class="process-header">
            <div class="process-icon">🖥️</div>
            <div class="process-info">
              <div class="process-name">{{ stickyItem.processName }} (PID: {{ stickyItem.processId }})</div>
              <div class="process-details">
                内存: {{ formatMemory(stickyItem.useMemory) }} |
                线程: {{ stickyItem.threadCount }} |
                连接: {{ stickyItem.connectionCount }}个
              </div>
            </div>
            <button
              class="collapse-btn"
              @click="toggleCollapseWithScrollAdjust(stickyItem.processIndex)"
            >
              {{ getCollapseIcon(stickyItem.processIndex) }}
            </button>
          </div>
        </template>
      </div>
    </div>

    <n-virtual-list
      ref="virtualListRef"
      style="max-height: 500px"
      :item-size="60"
      :items="displayItems"
      @scroll="handleScroll"
      item-resizable
    >

      <template #default="{ item, index }">
        <div
          :key="item.key"
          class="item"
          :class="{
            'is-process': item.isProcess,
            'is-connection': item.isConnection,
            'is-collapsed-indicator': item.isCollapsedIndicator,
            'is-hidden': shouldHideItem(item, index),
          }"
          style="height: 60px"
        >

          <template v-if="item.isCollapsedIndicator">
            <!-- 折叠状态指示器 -->
            <div class="collapsed-indicator">
              <span>📁 已折叠进程: {{ item.processName }} ({{ item.collapsedCount }}个连接)</span>
              <button class="expand-btn" @click="expandSection(item.processIndex)">展开</button>
            </div>
          </template>

          <template v-else-if="item.isProcess">
            <!-- 进程标题 -->
            <div class="process-header">
              <div class="process-icon">🖥️</div>
              <div class="process-info">
                <div class="process-name">{{ item.processName }} (PID: {{ item.processId }})</div>
                <div class="process-details">
                  内存: {{ formatMemory(item.useMemory) }} |
                  线程: {{ item.threadCount }} |
                  连接: {{ item.connectionCount }}个
                </div>
              </div>
              <button
                class="collapse-btn"
                @click="toggleCollapse(item.processIndex)"
              >
                {{ getCollapseIcon(item.processIndex) }}
              </button>
            </div>
          </template>

          <template v-else-if="item.isConnection">
            <!-- 连接详情 -->
            <div class="connection-item">
              <div class="connection-icon">
                {{ item.protocol === 0 ? '🔗' : '📡' }}
              </div>
              <div class="connection-info">
                <div class="connection-endpoints">
                  <span class="local">{{ item.localEndpoint.address }}:{{ item.localEndpoint.port }}</span>
                  <span class="arrow">→</span>
                  <span class="remote">{{ item.remoteEndpoint.address }}:{{ item.remoteEndpoint.port }}</span>
                  <span class="protocol">{{ item.protocol === 0 ? 'TCP' : 'UDP' }}</span>
                </div>
                <div class="connection-stats">
                  <span class="status" :class="getConnectionStatusClass(item.state)">
                    {{ getConnectionStatus(item.state) }}
                  </span>
                  <span class="traffic">
                    ↑{{ formatBytes(item.bytesSent) }} ↓{{ formatBytes(item.bytesReceived) }}
                  </span>
                  <span class="active" :class="{ 'is-active': item.isActive }">
                    {{ item.isActive ? '活跃' : '非活跃' }}
                  </span>
                </div>
              </div>
            </div>
          </template>
        </div>
      </template>
    </n-virtual-list>

    <!-- 无数据提示 -->
    <div v-if="displayItems.length === 0" class="no-data" style="text-align: center; padding: 40px; color: #999;">
      <div style="font-size: 48px; margin-bottom: 16px;">📡</div>
      <div style="font-size: 16px; margin-bottom: 8px;">暂无网络连接数据</div>
      <div style="font-size: 14px;">请检查数据源是否正确加载</div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, nextTick } from 'vue'
import { ap } from '@/json/test.js'

const virtualListRef = ref()
const scrollTop = ref(0)
const itemSize = 60

// 折叠状态管理 - key是进程索引
const collapsedSections = ref(new Set<number>())

// 记录折叠前的滚动信息
const beforeCollapseState = ref<{
  scrollTop: number
  visibleItemKey: string
  offsetInItem: number
} | null>(null)

// 格式化内存大小
const formatMemory = (bytes: number) => {
  const mb = bytes / (1024 * 1024)
  return mb >= 1 ? `${mb.toFixed(1)}MB` : `${(bytes / 1024).toFixed(1)}KB`
}

// 格式化字节数
const formatBytes = (bytes: number) => {
  if (bytes === 0) return '0B'
  const k = 1024
  const sizes = ['B', 'KB', 'MB', 'GB']
  const i = Math.floor(Math.log(bytes) / Math.log(k))
  return `${(bytes / Math.pow(k, i)).toFixed(1)}${sizes[i]}`
}

// 获取连接状态
const getConnectionStatus = (state: number) => {
  const states = {
    0: '关闭',
    1: '监听',
    2: '已建立',
    3: '等待关闭',
    4: '关闭等待'
  }
  return states[state as keyof typeof states] || '未知'
}

// 获取连接状态样式类
const getConnectionStatusClass = (state: number) => {
  const classes = {
    0: 'status-closed',
    1: 'status-listening',
    2: 'status-established',
    3: 'status-closing',
    4: 'status-waiting'
  }
  return classes[state as keyof typeof classes] || 'status-unknown'
}

// 生成扁平化的显示项列表
const originalItems = computed(() => {
  const result: any[] = []

  // 数据检查和调试
  console.log('ap data:', ap)
  console.log('ap length:', ap?.length)

  if (!ap || !Array.isArray(ap) || ap.length === 0) {
    console.warn('No data available')
    return result
  }

  ap.forEach((process, processIndex) => {
    if (!process) return

    // 添加进程标题项
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
      mainModulePath: process.mainModulePath
    })

    // 添加连接项
    if (process.connections && Array.isArray(process.connections)) {
      process.connections.forEach((connection, connectionIndex) => {
        result.push({
          key: `connection-${processIndex}-${connectionIndex}`,
          isConnection: true,
          processIndex,
          connectionIndex,
          processName: process.processName || 'Unknown Process',
          ...connection
        })
      })
    }
  })

  console.log('originalItems result:', result.length, 'items')
  return result
})

// 获取当前可见的第一个实际元素
const getFirstVisibleItem = () => {
  const currentIndex = Math.floor(scrollTop.value / itemSize)
  const items = displayItems.value

  if (currentIndex < items.length) {
    return {
      item: items[currentIndex],
      index: currentIndex,
      offsetInViewport: scrollTop.value % itemSize,
    }
  }

  return null
}

// 切换折叠状态（从吸顶位置触发时使用）
const toggleCollapseWithScrollAdjust = async (processIndex: number) => {
  if (!collapsedSections.value.has(processIndex)) {
    // 折叠操作
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

    // 等待DOM更新后调整滚动位置
    await nextTick()

    if (beforeCollapseState.value && visibleItem) {
      // 找到同一个元素在新列表中的位置
      const newItems = displayItems.value
      const newIndex = newItems.findIndex(item => item.key === visibleItem.item.key)

      if (newIndex !== -1) {
        // 计算新的滚动位置
        const newScrollTop = newIndex * itemSize - visibleItem.offsetInViewport

        virtualListRef.value?.scrollTo({ top: Math.max(0, newScrollTop) })
      }
    }
  } else {
    // 展开操作
    expandSection(processIndex)
  }
}

// 切换折叠状态（普通情况）
const toggleCollapse = (processIndex: number) => {
  if (collapsedSections.value.has(processIndex)) {
    collapsedSections.value.delete(processIndex)
  } else {
    collapsedSections.value.add(processIndex)
  }
  collapsedSections.value = new Set(collapsedSections.value)
}

// 展开某个进程
const expandSection = (processIndex: number) => {
  collapsedSections.value.delete(processIndex)
  collapsedSections.value = new Set(collapsedSections.value)
}

// 获取折叠按钮图标
const getCollapseIcon = (processIndex: number) => {
  return collapsedSections.value.has(processIndex) ? '📁' : '📂'
}

// 根据折叠状态生成显示的items
const displayItems = computed(() => {
  const result: any[] = []
  const processGroups: { [key: number]: any[] } = {}

  // 按进程分组
  originalItems.value.forEach(item => {
    const processIndex = item.processIndex
    if (!processGroups[processIndex]) {
      processGroups[processIndex] = []
    }
    processGroups[processIndex].push(item)
  })

  // 生成显示列表
  Object.keys(processGroups).forEach(processIndexStr => {
    const processIndex = parseInt(processIndexStr)
    const group = processGroups[processIndex]
    const processItem = group.find(item => item.isProcess)
    const connectionItems = group.filter(item => item.isConnection)

    if (collapsedSections.value.has(processIndex)) {
      // 折叠状态：只显示折叠指示器
      result.push({
        key: `collapsed-${processIndex}`,
        isCollapsedIndicator: true,
        processIndex,
        processName: processItem.processName,
        collapsedCount: connectionItems.length,
      })
    } else {
      // 展开状态：显示进程和所有连接
      result.push(processItem)
      connectionItems.forEach(connection => {
        result.push(connection)
      })
    }
  })

  return result
})

// 计算当前滚动位置对应的项目索引
const getCurrentIndex = () => {
  return Math.floor(scrollTop.value / itemSize)
}

// 找到当前可见区域内最近的进程标题元素
const stickyItem = computed(() => {
  if (displayItems.value.length === 0) return null

  const currentIndex = getCurrentIndex()

  // 向前查找最近的进程标题或折叠指示器
  for (let i = currentIndex; i >= 0; i--) {
    const item = displayItems.value[i]
    if (item && (item.isProcess || item.isCollapsedIndicator)) {
      return {
        ...item,
        originalIndex: i
      }
    }
  }

  return null
})

// 控制是否显示吸顶元素
const shouldShowSticky = computed(() => {
  if (!stickyItem.value) return false

  const currentIndex = getCurrentIndex()
  const stickyIndex = stickyItem.value.originalIndex

  // 只有当前可见区域的第一项不是标题项时，才显示吸顶
  return currentIndex > stickyIndex
})

// 控制是否隐藏列表项（避免与吸顶重复）
const shouldHideItem = (item: any, index: number) => {
  if (!stickyItem.value || !shouldShowSticky.value) return false

  // 如果当前项就是吸顶项，且吸顶正在显示，则隐藏这个项
  return item.key === stickyItem.value.key
}

const handleScroll = (e: Event) => {
  const target = e.target as HTMLElement
  scrollTop.value = target.scrollTop
}
</script>

<style scoped>
.virtual-list-container {
  position: relative;
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
}

.sticky-item {
  position: absolute;
  left: 0;
  right: 0;
  z-index: 10;
  background: rgba(255, 255, 255, 0.95);
  backdrop-filter: blur(8px);
  border-bottom: 1px solid #e0e0e0;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.sticky-content {
  background: #f5f5f5;
  font-weight: 600;
}

.sticky-content.is-collapsed-indicator {
  background: #f0f0f0;
  border-left: 4px solid #999;
}

.item {
  display: flex;
  align-items: center;
  padding: 8px 12px;
  background: white;
  border-bottom: 1px solid #f0f0f0;
  transition: background-color 0.2s;
}

.item:hover {
  background: #f8f8f8;
}

.is-hidden {
  visibility: hidden;
}

.is-process {
  background: #e6f4ff;
  border-left: 4px solid #1890ff;
  font-weight: 500;
}

.is-connection {
  background: white;
  border-left: 4px solid transparent;
  padding-left: 20px;
}

.is-collapsed-indicator {
  background: #f0f0f0;
  border-left: 4px solid #999;
}

.process-header {
  display: flex;
  align-items: center;
  width: 100%;
  gap: 12px;
}

.process-icon {
  font-size: 20px;
  flex-shrink: 0;
}

.process-info {
  flex: 1;
  min-width: 0;
}

.process-name {
  font-size: 14px;
  font-weight: 600;
  color: #1890ff;
  margin-bottom: 2px;
}

.process-details {
  font-size: 12px;
  color: #666;
}

.connection-item {
  display: flex;
  align-items: center;
  width: 100%;
  gap: 12px;
}

.connection-icon {
  font-size: 16px;
  flex-shrink: 0;
}

.connection-info {
  flex: 1;
  min-width: 0;
}

.connection-endpoints {
  font-size: 13px;
  font-family: 'Courier New', monospace;
  margin-bottom: 2px;
  display: flex;
  align-items: center;
  gap: 8px;
}

.local {
  color: #52c41a;
  font-weight: 500;
}

.arrow {
  color: #999;
}

.remote {
  color: #1890ff;
  font-weight: 500;
}

.protocol {
  background: #f0f0f0;
  padding: 1px 4px;
  border-radius: 2px;
  font-size: 11px;
  color: #666;
}

.connection-stats {
  font-size: 11px;
  display: flex;
  gap: 12px;
  align-items: center;
}

.status {
  padding: 1px 6px;
  border-radius: 10px;
  font-weight: 500;
}

.status-established {
  background: #f6ffed;
  color: #52c41a;
  border: 1px solid #d9f7be;
}

.status-listening {
  background: #e6f7ff;
  color: #1890ff;
  border: 1px solid #bae7ff;
}

.status-closed {
  background: #fff2e8;
  color: #fa8c16;
  border: 1px solid #ffd591;
}

.status-closing, .status-waiting {
  background: #fff1f0;
  color: #ff4d4f;
  border: 1px solid #ffccc7;
}

.traffic {
  color: #666;
  font-family: 'Courier New', monospace;
}

.active {
  font-weight: 500;
}

.active.is-active {
  color: #52c41a;
}

.active:not(.is-active) {
  color: #999;
}

.collapsed-indicator {
  display: flex;
  align-items: center;
  justify-content: space-between;
  width: 100%;
  font-style: italic;
  color: #666;
}

.collapse-btn,
.expand-btn {
  background: none;
  border: 1px solid #d9d9d9;
  border-radius: 4px;
  padding: 4px 8px;
  cursor: pointer;
  font-size: 12px;
  transition: all 0.2s;
  flex-shrink: 0;
}

.collapse-btn:hover,
.expand-btn:hover {
  background: #f5f5f5;
  border-color: #40a9ff;
}

.expand-btn {
  background: #1890ff;
  color: white;
  border-color: #1890ff;
}

.expand-btn:hover {
  background: #40a9ff;
}
</style>
