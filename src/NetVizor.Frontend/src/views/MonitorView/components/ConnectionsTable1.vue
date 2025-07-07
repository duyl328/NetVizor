<template>
  <div ref="containerRef" class="connections-table-container" :class="sizeClass">
    <!-- 统计卡片区域 -->
    <div class="stats-section" v-if="showStats">
      <div class="stats-grid">
        <div class="stat-card" v-show="showStatCard('process')">
          <div class="stat-icon-wrapper process">
            <n-icon :component="DesktopOutline" :size="statIconSize" />
          </div>
          <div class="stat-content">
            <div class="stat-number">{{ stats.activeProcesses }}</div>
            <div class="stat-label">活跃进程 / {{ stats.totalProcesses }}</div>
          </div>
        </div>

        <div class="stat-card" v-show="showStatCard('connection')">
          <div class="stat-icon-wrapper connection">
            <n-icon :component="GitNetworkOutline" :size="statIconSize" />
          </div>
          <div class="stat-content">
            <div class="stat-number">{{ stats.activeConnections }}</div>
            <div class="stat-label">活跃连接 / {{ stats.totalConnections }}</div>
          </div>
        </div>

        <div class="stat-card" v-show="showStatCard('upload')">
          <div class="stat-icon-wrapper upload">
            <n-icon :component="ArrowUpOutline" :size="statIconSize" />
          </div>
          <div class="stat-content">
            <div class="stat-number">{{ formatSpeed(stats.totalUploadSpeed) }}</div>
            <div class="stat-label">总上传速度</div>
          </div>
        </div>

        <div class="stat-card" v-show="showStatCard('download')">
          <div class="stat-icon-wrapper download">
            <n-icon :component="ArrowDownOutline" :size="statIconSize" />
          </div>
          <div class="stat-content">
            <div class="stat-number">{{ formatSpeed(stats.totalDownloadSpeed) }}</div>
            <div class="stat-label">总下载速度</div>
          </div>
        </div>
      </div>
    </div>

    <!-- 统一连接列表容器 -->
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
                      {{ formatBytes(stickyItem.useMemory) }}
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
        :style="{ maxHeight: virtualListHeight }"
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
                    <n-tag
                      v-if="item.hasExited && item.exitCode !== undefined && !isCompact"
                      :type="item.exitCode === 0 ? 'success' : 'error'"
                      size="small"
                    >
                      退出代码: {{ item.exitCode }}
                    </n-tag>
                  </div>
                  <div class="process-meta">
                    <span class="meta-item">
                      <n-icon :component="HardwareChipOutline" size="14" />
                      <span>内存: {{ formatBytes(item.useMemory) }}</span>
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
                      <span>{{ item.hasExited ? '运行时长' : '已运行' }}: {{ formatDuration(new Date(item.startTime), item.exitTime ? new Date(item.exitTime) : undefined) }}</span>
                    </span>
                    <span
                      v-if="item.mainModuleName && showProcessDetail('module')"
                      class="meta-item"
                      :title="item.mainModulePath"
                    >
                      <n-icon :component="FileTrayFullOutline" size="14" />
                      {{ item.mainModuleName }}
                    </span>
                  </div>
                </div>
                <div class="process-actions">
                  <div class="traffic-summary">
                    <div class="traffic-speed">
                      <div class="traffic-item upload">
                        <n-icon :component="ArrowUpOutline" :size="trafficIconSize" />
                        <span>{{ formatSpeed(item.uploadSpeed) }}</span>
                      </div>
                      <div class="traffic-item download">
                        <n-icon :component="ArrowDownOutline" :size="trafficIconSize" />
                        <span>{{ formatSpeed(item.downloadSpeed) }}</span>
                      </div>
                    </div>
                    <div class="traffic-total" v-if="!isCompact">
                      <span>
                        总计: ↑{{ formatBytes(item.totalUploaded) }} ↓{{ formatBytes(item.totalDownloaded) }}
                      </span>
                    </div>
                  </div>
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
                        :type="getConnectionStateType(item.state, item.isActive)"
                        size="tiny"
                        round
                      >
                        {{ item.state }}
                      </n-tag>
                      <n-tag size="tiny" round>
                        {{ item.protocol }}
                      </n-tag>
                      <n-tag size="tiny" v-if="!isCompact && item.direction">
                        <n-icon :component="getDirectionIcon(item.direction)" size="12" />
                        {{ getDirectionText(item.direction) }}
                      </n-tag>
                    </div>
                  </div>

                  <div class="connection-stats">
                    <div class="traffic-info">
                      <div class="traffic-item upload">
                        <n-icon :component="ArrowUpOutline" size="12" />
                        <span>{{ formatSpeed(item.currentSendSpeed || 0) }}</span>
                      </div>
                      <div class="traffic-item download">
                        <n-icon :component="ArrowDownOutline" size="12" />
                        <span>{{ formatSpeed(item.currentReceiveSpeed || 0) }}</span>
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

                <div class="connection-side" v-if="showActions">
                  <div class="connection-actions">
                    <n-tooltip trigger="hover">
                      <template #trigger>
                        <n-button size="tiny" circle quaternary type="info" @click.stop="handleViewDetails(item)">
                          <template #icon>
                            <n-icon :component="InformationCircleOutline" />
                          </template>
                        </n-button>
                      </template>
                      查看详情
                    </n-tooltip>
                    <n-tooltip trigger="hover">
                      <template #trigger>
                        <n-button size="tiny" circle quaternary type="warning" @click.stop="handleDisconnect(item)">
                          <template #icon>
                            <n-icon :component="StopCircleOutline" />
                          </template>
                        </n-button>
                      </template>
                      断开连接
                    </n-tooltip>
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
  </div>
</template>

<script setup lang="ts">
import { ref, computed, nextTick, onMounted, onUnmounted, watch } from 'vue'
import { NButton, NIcon, NTag, NEmpty, NVirtualList, NTooltip } from 'naive-ui'
import {
  FolderOutline,
  ChevronDownOutline,
  ChevronUpOutline,
  DesktopOutline,
  HardwareChipOutline,
  GitBranchOutline,
  GitNetworkOutline,
  ArrowForwardOutline,
  ArrowUpOutline,
  ArrowDownOutline,
  CloudOfflineOutline,
  LayersOutline,
  LocationOutline,
  GlobeOutline,
  TimeOutline,
  PauseCircleOutline,
  InformationCircleOutline,
  StopCircleOutline,
  PeopleOutline,
  FileTrayFullOutline,
  SwapHorizontalOutline,
} from '@vicons/ionicons5'
import type { ProcessType, ConnectionInfo, IPEndPoint } from '@/types/process'
import { ProtocolType, ConnectionState, TrafficDirection, FILE_SIZE_UNIT_ENUM } from '@/constants/enums'
import { convertFileSize } from '@/utils/fileUtil'

// 尺寸类型
type SizeType = 'compact' | 'normal' | 'comfortable' | 'spacious'

// Props
interface Props {
  connections: ProcessType[]
  showStats?: boolean
  showActions?: boolean
  defaultExpanded?: number[]
  height?: string
  itemHeight?: number
}

const props = withDefaults(defineProps<Props>(), {
  connections: () => [],
  showStats: true,
  showActions: true,
  defaultExpanded: () => [],
  height: '600px',
  itemHeight: 80
})

// Emits
const emit = defineEmits<{
  'view-details': [connection: ConnectionInfo]
  'disconnect': [connection: ConnectionInfo]
  'process-select': [process: ProcessType]
}>()

// 响应式相关
const containerRef = ref<HTMLElement>()
const virtualListRef = ref()
const containerWidth = ref(1200)
const resizeObserver = ref<ResizeObserver>()
const actualSizeType = ref<SizeType>('spacious')

// 虚拟列表相关
const scrollTop = ref(0)
const scrollbarWidth = ref(0)
const collapsedSections = ref(new Set<number>())
const beforeCollapseState = ref<{
  scrollTop: number
  visibleItemKey: string
  offsetInItem: number
} | null>(null)

// 添加防抖处理
let resizeTimeout: number | undefined

// 使用滞后（hysteresis）机制计算尺寸类型
const calculateSizeType = (width: number): SizeType => {
  const currentType = actualSizeType.value
  const HYSTERESIS = 20 // 滞后阈值，防止在临界值附近抖动

  // 根据当前状态和新宽度计算新的类型
  if (currentType === 'compact') {
    if (width > 500 + HYSTERESIS) return 'normal'
  } else if (currentType === 'normal') {
    if (width < 500 - HYSTERESIS) return 'compact'
    if (width > 800 + HYSTERESIS) return 'comfortable'
  } else if (currentType === 'comfortable') {
    if (width < 800 - HYSTERESIS) return 'normal'
    if (width > 1200 + HYSTERESIS) return 'spacious'
  } else {
    // spacious
    if (width < 1200 - HYSTERESIS) return 'comfortable'
  }

  return currentType
}

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

// 计算属性
const sizeType = computed(() => actualSizeType.value)
const sizeClass = computed(() => `size-${sizeType.value}`)
const isCompact = computed(() => sizeType.value === 'compact')

// 虚拟列表高度计算
const virtualListHeight = computed(() => {
  // 减去统计卡片区域的高度
  if (props.showStats) {
    const statsHeight = isCompact.value ? 100 : 140
    return `calc(${props.height} - ${statsHeight}px)`
  }
  return props.height
})

// 响应式图标大小
const statIconSize = computed(() => {
  switch (sizeType.value) {
    case 'compact':
      return 20
    case 'normal':
      return 22
    default:
      return 24
  }
})

const trafficIconSize = computed(() => (isCompact.value ? 12 : 14))

// 根据尺寸决定是否显示某些元素
const showStatCard = (type: string) => {
  if (!props.showStats) return false
  if (sizeType.value === 'spacious') return true
  if (sizeType.value === 'comfortable') return true
  if (sizeType.value === 'normal') return type !== 'download'
  return type === 'process' || type === 'connection'
}

const showProcessDetail = (type: string) => {
  if (sizeType.value === 'spacious') return true
  if (sizeType.value === 'comfortable') return type !== 'module'
  if (sizeType.value === 'normal') return type === 'time' || type === 'memory'
  return false
}

// 初始化默认展开项
watch(() => props.defaultExpanded, (newVal) => {
  collapsedSections.value = new Set(newVal)
}, { immediate: true })

// 计算统计数据
const stats = computed(() => {
  const activeProcesses = props.connections.filter((p) => !p.hasExited)
  const totalConnections = props.connections.reduce((sum, p) => sum + p.connections.length, 0)
  const activeConnections = props.connections.reduce(
    (sum, p) => sum + p.connections.filter((c) => c.isActive).length,
    0,
  )
  const totalUploadSpeed = activeProcesses.reduce((sum, p) => sum + (p.uploadSpeed || 0), 0)
  const totalDownloadSpeed = activeProcesses.reduce((sum, p) => sum + (p.downloadSpeed || 0), 0)

  return {
    activeProcesses: activeProcesses.length,
    totalProcesses: props.connections.length,
    totalConnections,
    activeConnections,
    totalUploadSpeed,
    totalDownloadSpeed,
  }
})

// 数据处理 - 将 ConnectionsTable1 的数据转换为 UnifiedConnectionsList4 的格式
const originalItems = computed(() => {
  const result: any[] = []

  if (!props.connections || !Array.isArray(props.connections) || props.connections.length === 0) {
    return result
  }

  props.connections.forEach((process, processIndex) => {
    if (!process) return

    // 计算进程级别的统计信息
    const uploadSpeed = process.uploadSpeed || 0
    const downloadSpeed = process.downloadSpeed || 0
    const totalUploaded = process.totalUploaded || 0
    const totalDownloaded = process.totalDownloaded || 0

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
      exitTime: process.exitTime,
      hasExited: process.hasExited || false,
      exitCode: process.exitCode,
      mainModulePath: process.mainModulePath,
      mainModuleName: process.mainModuleName,
      connections: process.connections || [],
      uploadSpeed,
      downloadSpeed,
      totalUploaded,
      totalDownloaded,
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

// 计算吸顶元素的样式
const stickyStyle = computed(() => ({
  top: '0px',
  right: `${scrollbarWidth.value}px`,
}))

// 吸顶逻辑
const getCurrentIndex = () => {
  return Math.floor(scrollTop.value / props.itemHeight)
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

// 生命周期
onMounted(() => {
  scrollbarWidth.value = getScrollbarWidth()
  window.addEventListener('resize', handleResize)

  if (containerRef.value) {
    // 初始化尺寸类型
    const initialWidth = containerRef.value.offsetWidth
    containerWidth.value = initialWidth
    actualSizeType.value = calculateSizeType(initialWidth)

    // 创建 ResizeObserver，使用防抖处理
    resizeObserver.value = new ResizeObserver((entries) => {
      for (const entry of entries) {
        // 清除之前的超时
        if (resizeTimeout) {
          clearTimeout(resizeTimeout)
        }

        // 设置新的超时，防抖处理
        resizeTimeout = window.setTimeout(() => {
          const newWidth = entry.contentRect.width
          containerWidth.value = newWidth
          const newType = calculateSizeType(newWidth)
          if (newType !== actualSizeType.value) {
            actualSizeType.value = newType
          }
        }, 100) // 100ms 防抖延迟
      }
    })

    resizeObserver.value.observe(containerRef.value)
  }
})

onUnmounted(() => {
  if (resizeTimeout) {
    clearTimeout(resizeTimeout)
  }
  if (resizeObserver.value) {
    resizeObserver.value.disconnect()
  }
  window.removeEventListener('resize', handleResize)
})

// 响应式处理
const handleResize = () => {
  scrollbarWidth.value = getScrollbarWidth()
}

// 交互逻辑
const handleScroll = (e: Event) => {
  const target = e.target as HTMLElement
  scrollTop.value = target.scrollTop
}

const getFirstVisibleItem = () => {
  const currentIndex = Math.floor(scrollTop.value / props.itemHeight)
  const items = displayItems.value

  if (currentIndex < items.length) {
    return {
      item: items[currentIndex],
      index: currentIndex,
      offsetInViewport: scrollTop.value % props.itemHeight,
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
        const newScrollTop = newIndex * props.itemHeight - visibleItem.offsetInViewport
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

  // 触发进程选择事件
  const process = props.connections.find(p => p.processId === processIndex)
  if (process) {
    emit('process-select', process)
  }
}

const expandSection = (processIndex: number) => {
  collapsedSections.value.delete(processIndex)
  collapsedSections.value = new Set(collapsedSections.value)
}

// 格式化函数
const formatBytes = (bytes: number): string => {
  if (bytes === 0) return '0 B'
  const k = 1024
  const sizes = ['B', 'KB', 'MB', 'GB', 'TB']
  const i = Math.floor(Math.log(bytes) / Math.log(k))
  const value = parseFloat((bytes / Math.pow(k, i)).toFixed(2))

  // 紧凑模式下简化单位
  if (isCompact.value && i > 0) {
    return value + sizes[i][0]
  }

  return value + ' ' + sizes[i]
}

const formatSpeed = (bytesPerSecond: number): string => {
  const fileSize = convertFileSize(bytesPerSecond,FILE_SIZE_UNIT_ENUM.B)
  return fileSize.size + fileSize.unit + '/s'
}

const formatEndpoint = (endpoint: IPEndPoint): string => {
  if (isCompact.value && endpoint.address === '0.0.0.0') {
    return `*:${endpoint.port}`
  }
  return `${endpoint.address}:${endpoint.port}`
}

const formatDuration = (start: Date, end?: Date): string => {
  const endTime = end || new Date()
  const diff = endTime.getTime() - start.getTime()
  const hours = Math.floor(diff / 3600000)
  const minutes = Math.floor((diff % 3600000) / 60000)
  const seconds = Math.floor((diff % 60000) / 1000)

  if (isCompact.value) {
    return `${hours}h${minutes}m`
  }

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

const getProcessStatus = (process: any) => {
  if (process.hasExited) return { type: 'error' as const, text: '已退出', show: true }
  const hasActiveConnections = process.connections?.some((c: any) => c.isActive)
  if (hasActiveConnections) {
    return { type: 'success' as const, text: '活跃', show: true }
  }
  return { type: 'warning' as const, text: '空闲', show: false }
}

const getConnectionStateType = (state: ConnectionState, isActive: boolean) => {
  if (!isActive) return 'default' as const
  switch (state) {
    case ConnectionState.ESTABLISHED:
      return 'success' as const
    case ConnectionState.LISTEN:
      return 'info' as const
    case ConnectionState.TIME_WAIT:
    case ConnectionState.CLOSE_WAIT:
      return 'warning' as const
    default:
      return 'default' as const
  }
}

const getDirectionIcon = (direction: TrafficDirection) => {
  switch (direction) {
    case TrafficDirection.Inbound:
      return ArrowDownOutline
    case TrafficDirection.Outbound:
      return ArrowUpOutline
    case TrafficDirection.Both:
      return SwapHorizontalOutline
  }
}

const getDirectionText = (direction: TrafficDirection) => {
  switch (direction) {
    case TrafficDirection.Inbound:
      return '入站'
    case TrafficDirection.Outbound:
      return '出站'
    case TrafficDirection.Both:
      return '双向'
  }
}

// 事件处理
const handleViewDetails = (connection: ConnectionInfo) => {
  emit('view-details', connection)
}

const handleDisconnect = (connection: ConnectionInfo) => {
  emit('disconnect', connection)
}
</script>

<style scoped>
.connections-table-container {
  height: 100%;
  overflow: hidden;
  padding: 16px;
  box-sizing: border-box;
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
  background: var(--bg-primary);
}

/* 响应式调整 */
.size-compact .connections-table-container {
  padding: 12px;
}

.size-normal .connections-table-container {
  padding: 14px;
}

/* 统计区域 */
.stats-section {
  margin-bottom: 20px;
}

.size-compact .stats-section {
  margin-bottom: 12px;
}

.stats-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 16px;
}

.size-compact .stats-grid {
  grid-template-columns: repeat(2, 1fr);
  gap: 8px;
}

.size-normal .stats-grid {
  gap: 12px;
}

.stat-card {
  background: var(--bg-card);
  backdrop-filter: var(--backdrop-blur);
  border: 1px solid var(--border-primary);
  border-radius: 12px;
  padding: 20px;
  display: flex;
  align-items: center;
  gap: 16px;
  transition: var(--transition);
}

.size-compact .stat-card {
  padding: 12px;
  gap: 12px;
  border-radius: 8px;
}

.size-normal .stat-card {
  padding: 16px;
  gap: 14px;
}

.stat-card:hover {
  transform: translateY(-2px);
  box-shadow: var(--shadow-lg);
}

.stat-icon-wrapper {
  width: 48px;
  height: 48px;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
}

.size-compact .stat-icon-wrapper {
  width: 36px;
  height: 36px;
  border-radius: 8px;
}

.size-normal .stat-icon-wrapper {
  width: 40px;
  height: 40px;
  border-radius: 10px;
}

.stat-icon-wrapper.process {
  background: rgba(59, 130, 246, 0.1);
  color: #3b82f6;
}

.stat-icon-wrapper.connection {
  background: rgba(168, 85, 247, 0.1);
  color: #a855f7;
}

.stat-icon-wrapper.upload {
  background: rgba(239, 68, 68, 0.1);
  color: #ef4444;
}

.stat-icon-wrapper.download {
  background: rgba(34, 197, 94, 0.1);
  color: #22c55e;
}

.stat-content {
  flex: 1;
  min-width: 0;
}

.stat-number {
  font-size: 28px;
  font-weight: 700;
  color: var(--text-primary);
  line-height: 1.2;
}

.size-compact .stat-number {
  font-size: 20px;
}

.size-normal .stat-number {
  font-size: 24px;
}

.stat-label {
  font-size: 13px;
  color: var(--text-muted);
  margin-top: 4px;
}

.size-compact .stat-label {
  font-size: 11px;
  margin-top: 2px;
}

/* 统一连接列表容器 */
.unified-connections-container {
  position: relative;
  height: 100%;
  background: var(--bg-primary);
  border-radius: 12px;
  overflow: hidden;
  box-shadow: var(--shadow-md);
  flex: 1;
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
  display: flex;
  align-items: center;
  gap: 16px;
  flex-shrink: 0;
}

.traffic-summary {
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  gap: 6px;
}

.traffic-speed {
  display: flex;
  gap: 12px;
  font-size: 14px;
  font-weight: 500;
}

.traffic-item {
  display: flex;
  align-items: center;
  gap: 4px;
}

.traffic-item.upload {
  color: #ef4444;
}

.traffic-item.download {
  color: #22c55e;
}

.traffic-total {
  font-size: 11px;
  color: var(--text-muted);
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

.connection-side {
  display: flex;
  align-items: flex-start;
  gap: 12px;
  flex-shrink: 0;
}

.connection-actions {
  display: flex;
  gap: 4px;
  flex-shrink: 0;
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

/* CSS 变量定义 */
:root {
  --bg-primary: #ffffff;
  --bg-secondary: #f9fafb;
  --bg-hover: #f3f4f6;
  --bg-process: #f0f9ff;
  --bg-active: rgba(16, 185, 129, 0.05);
  --bg-collapsed: #faf5ff;
  --bg-sticky: rgba(255, 255, 255, 0.95);
  --bg-card: rgba(255, 255, 255, 0.9);
  --backdrop-blur: blur(8px);

  --border-primary: #e5e7eb;
  --border-secondary: #f3f4f6;
  --border-hover: #cbd5e1;

  --text-primary: #111827;
  --text-secondary: #4b5563;
  --text-muted: #6b7280;
  --text-quaternary: #9ca3af;
  --text-success: #059669;

  --shadow-md: 0 4px 6px -1px rgba(0, 0, 0, 0.1), 0 2px 4px -1px rgba(0, 0, 0, 0.06);
  --shadow-lg: 0 10px 30px rgba(0, 0, 0, 0.1);
  --transition: all 0.2s ease;
}

/* 暗色模式 */
@media (prefers-color-scheme: dark) {
  :root {
    --bg-primary: #1f2937;
    --bg-secondary: #111827;
    --bg-hover: #374151;
    --bg-process: rgba(59, 130, 246, 0.1);
    --bg-active: rgba(16, 185, 129, 0.1);
    --bg-collapsed: rgba(168, 85, 247, 0.1);
    --bg-sticky: rgba(31, 41, 55, 0.95);
    --bg-card: rgba(30, 41, 59, 0.9);

    --border-primary: #374151;
    --border-secondary: #1f2937;
    --border-hover: #475569;

    --text-primary: #f9fafb;
    --text-secondary: #d1d5db;
    --text-muted: #94a3b8;
    --text-quaternary: #64748b;
    --text-success: #10b981;

    --shadow-md: 0 4px 6px -1px rgba(0, 0, 0, 0.3), 0 2px 4px -1px rgba(0, 0, 0, 0.2);
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
