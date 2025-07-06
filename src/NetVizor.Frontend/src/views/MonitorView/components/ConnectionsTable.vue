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

    <!-- 进程列表 -->
    <div class="process-list">
      <n-empty
        v-if="connections.length === 0"
        description="暂无进程数据"
        :style="{ padding: '40px' }"
      />

      <transition-group name="process-list" tag="div" v-else>
        <div
          v-for="process in connections"
          :key="process.processId"
          class="process-item"
          :class="{
            'is-expanded': expandedItems.has(process.processId),
            'is-exited': process.hasExited,
          }"
        >
          <!-- 进程头部 -->
          <div class="process-header" @click="toggleExpand(process.processId)">
            <div class="header-left">
              <div class="expand-indicator">
                <n-icon
                  :component="ChevronForwardOutline"
                  :size="expandIconSize"
                  class="expand-arrow"
                />
              </div>

              <div class="process-info">
                <div class="process-title">
                  <h4 class="process-name">{{ process.processName }}</h4>
                  <n-tag :type="getProcessStatus(process).type" :size="tagSize" round>
                    {{ getProcessStatus(process).text }}
                  </n-tag>
                  <n-tag
                    v-if="process.hasExited && process.exitCode !== undefined && !isCompact"
                    :type="process.exitCode === 0 ? 'success' : 'error'"
                    :size="tagSize"
                  >
                    退出代码: {{ process.exitCode }}
                  </n-tag>
                </div>

                <div class="process-details">
                  <span class="detail-item primary">
                    <n-icon :component="DesktopOutline" :size="detailIconSize" />
                    PID: {{ process.processId }}
                  </span>
                  <span class="detail-item" v-show="showProcessDetail('time')">
                    <n-icon :component="TimeOutline" :size="detailIconSize" />
                    {{ process.hasExited ? '运行时长' : '已运行' }}:
                    {{ formatDuration(new Date(process.startTime), process.exitTime ? new Date(process.exitTime) : undefined) }}
                  </span>
                  <span class="detail-item" v-show="showProcessDetail('thread')">
                    <n-icon :component="PeopleOutline" :size="detailIconSize" />
                    {{ process.threadCount }} 线程
                  </span>
                  <span class="detail-item" v-show="showProcessDetail('memory')">
                    <n-icon :component="HardwareChipOutline" :size="detailIconSize" />
                    内存: {{ formatBytes(process.useMemory) }}
                  </span>
                  <span class="detail-item primary">
                    <n-icon :component="GitNetworkOutline" :size="detailIconSize" />
                    {{ process.connections.length }} 个连接
                  </span>
                  <span
                    v-if="process.mainModuleName && showProcessDetail('module')"
                    class="detail-item"
                    :title="process.mainModulePath"
                  >
                    <n-icon :component="FileTrayFullOutline" :size="detailIconSize" />
                    {{ process.mainModuleName }}
                  </span>
                </div>
              </div>
            </div>

            <div class="header-right">
              <div class="traffic-summary">
                <div class="traffic-speed">
                  <div class="traffic-item upload">
                    <n-icon :component="ArrowUpOutline" :size="trafficIconSize" />
                    <span>{{ formatSpeed(process.uploadSpeed) }}</span>
                  </div>
                  <div class="traffic-item download">
                    <n-icon :component="ArrowDownOutline" :size="trafficIconSize" />
                    <span>{{ formatSpeed(process.downloadSpeed) }}</span>
                  </div>
                </div>
                <div class="traffic-total" v-if="!isCompact">
                  <span>
                    总计: ↑{{ formatBytes(process.totalUploaded) }} ↓{{
                      formatBytes(process.totalDownloaded)
                    }}
                  </span>
                </div>
              </div>
            </div>
          </div>

          <!-- 连接列表 -->
          <n-collapse-transition :show="expandedItems.has(process.processId)">
            <div class="connections-content">
              <div class="content-wrapper">
                <div class="connections-header">
                  <h5 class="connections-title">网络连接详情</h5>
                  <span class="connection-count">{{ process.connections.length }} 个连接</span>
                </div>

                <n-empty
                  v-if="process.connections.length === 0"
                  description="没有网络连接"
                  :style="{ padding: isCompact ? '12px' : '20px' }"
                />

                <div v-else class="connection-list">
                  <div
                    v-for="connection in process.connections"
                    :key="connection.connectionKey"
                    class="connection-item"
                    :class="{ 'is-active': connection.isActive }"
                  >
                    <div class="connection-main">
                      <div class="connection-status">
                        <div
                          class="status-indicator"
                          :class="connection.isActive ? 'active' : 'inactive'"
                          :title="connection.isActive ? '活跃' : '空闲'"
                        ></div>
                      </div>

                      <div class="connection-info">
                        <div class="connection-tags">
                          <n-tag
                            :type="getConnectionStateType(connection.state, connection.isActive)"
                            :size="tagSize"
                          >
                            {{ connection.state }}
                          </n-tag>
                          <n-tag :size="tagSize">{{ connection.protocol }}</n-tag>
                          <n-tag :size="tagSize" v-if="!isCompact">
                            <n-icon :component="getDirectionIcon(connection.direction)" size="12" />
                            {{ getDirectionText(connection.direction) }}
                          </n-tag>
                        </div>

                        <div class="connection-addresses">
                          <div class="address-section">
                            <n-icon :component="LocationOutline" :size="addressIconSize" />
                            <span class="address-value">
                              {{ formatEndpoint(connection.localEndpoint) }}
                            </span>
                          </div>
                          <div class="connection-arrow">
                            <n-icon :component="ArrowForwardOutline" :size="arrowIconSize" />
                          </div>
                          <div class="address-section">
                            <n-icon :component="GlobeOutline" :size="addressIconSize" />
                            <span class="address-value">
                              {{ formatEndpoint(connection.remoteEndpoint) }}
                            </span>
                          </div>
                        </div>

                        <div class="connection-meta" v-if="!isCompact">
                          <span class="meta-item">
                            <n-icon :component="TimeOutline" size="12" />
                            持续: {{ formatDuration(new Date(connection.startTime)) }}
                          </span>
                          <span v-if="!connection.isActive" class="meta-item inactive">
                            <n-icon :component="PauseCircleOutline" size="12" />
                            最后活跃: {{ getTimeSinceActive(new Date(connection.lastActiveTime)) }}
                          </span>
                        </div>
                      </div>
                    </div>

                    <div class="connection-side">
                      <div class="connection-traffic">
                        <div class="traffic-stats">
                          <div class="traffic-row upload">
                            <n-icon :component="ArrowUpOutline" :size="trafficIconSize" />
                            <span class="traffic-value">{{
                                formatSpeed(connection.currentSendSpeed)
                              }}</span>
                          </div>
                          <div class="traffic-row download">
                            <n-icon :component="ArrowDownOutline" :size="trafficIconSize" />
                            <span class="traffic-value">{{
                                formatSpeed(connection.currentReceiveSpeed)
                              }}</span>
                          </div>
                        </div>
                        <div class="traffic-total-small" v-if="!isCompact">
                          总计: ↑{{ formatBytes(connection.bytesSent) }} ↓{{
                            formatBytes(connection.bytesReceived)
                          }}
                        </div>
                      </div>

                      <div class="connection-actions" v-if="showActions">
                        <n-tooltip trigger="hover">
                          <template #trigger>
                            <n-button size="tiny" circle quaternary type="info" @click.stop="handleViewDetails(connection)">
                              <template #icon>
                                <n-icon :component="InformationCircleOutline" />
                              </template>
                            </n-button>
                          </template>
                          查看详情
                        </n-tooltip>
                        <n-tooltip trigger="hover">
                          <template #trigger>
                            <n-button size="tiny" circle quaternary type="warning" @click.stop="handleDisconnect(connection)">
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
                </div>
              </div>
            </div>
          </n-collapse-transition>
        </div>
      </transition-group>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, watch } from 'vue'
import { NButton, NIcon, NTag, NEmpty, NCollapseTransition, NTooltip } from 'naive-ui'
import {
  DesktopOutline,
  GitNetworkOutline,
  ArrowUpOutline,
  ArrowDownOutline,
  ChevronForwardOutline,
  TimeOutline,
  HardwareChipOutline,
  LocationOutline,
  GlobeOutline,
  ArrowForwardOutline,
  InformationCircleOutline,
  StopCircleOutline,
  PeopleOutline,
  FileTrayFullOutline,
  PauseCircleOutline,
  SwapHorizontalOutline,
} from '@vicons/ionicons5'
import type { ProcessType, ConnectionInfo, IPEndPoint } from '@/types/process'
import { ProtocolType, ConnectionState, TrafficDirection } from '@/constants/enums'

// 尺寸类型
type SizeType = 'compact' | 'normal' | 'comfortable' | 'spacious'

// Props
interface Props {
  connections: ProcessType[]
  showStats?: boolean
  showActions?: boolean
  defaultExpanded?: number[]
}

const props = withDefaults(defineProps<Props>(), {
  connections: () => [],
  showStats: true,
  showActions: true,
  defaultExpanded: () => [],
})

// Emits
const emit = defineEmits<{
  'view-details': [connection: ConnectionInfo]
  'disconnect': [connection: ConnectionInfo]
  'process-select': [process: ProcessType]
}>()

// 响应式相关
const containerRef = ref<HTMLElement>()
const containerWidth = ref(1200)
const resizeObserver = ref<ResizeObserver>()
const actualSizeType = ref<SizeType>('spacious')

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

// 计算属性
const sizeType = computed(() => actualSizeType.value)
const sizeClass = computed(() => `size-${sizeType.value}`)
const isCompact = computed(() => sizeType.value === 'compact')

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

const expandIconSize = computed(() => (isCompact.value ? 14 : 16))
const detailIconSize = computed(() => (isCompact.value ? 12 : 14))
const trafficIconSize = computed(() => (isCompact.value ? 12 : 14))
const addressIconSize = computed(() => (isCompact.value ? 10 : 12))
const arrowIconSize = computed(() => (isCompact.value ? 12 : 14))

const tagSize = computed(() => {
  switch (sizeType.value) {
    case 'compact':
      return 'tiny' as const
    case 'normal':
      return 'small' as const
    default:
      return 'small' as const
  }
})

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

// 状态管理
const expandedItems = ref(new Set<number>())

// 初始化默认展开项
watch(() => props.defaultExpanded, (newVal) => {
  expandedItems.value = new Set(newVal)
}, { immediate: true })

// 计算统计数据
const stats = computed(() => {
  const activeProcesses = props.connections.filter((p) => !p.hasExited)
  const totalConnections = props.connections.reduce((sum, p) => sum + p.connections.length, 0)
  const activeConnections = props.connections.reduce(
    (sum, p) => sum + p.connections.filter((c) => c.isActive).length,
    0,
  )
  const totalUploadSpeed = activeProcesses.reduce((sum, p) => sum + p.uploadSpeed, 0)
  const totalDownloadSpeed = activeProcesses.reduce((sum, p) => sum + p.downloadSpeed, 0)

  return {
    activeProcesses: activeProcesses.length,
    totalProcesses: props.connections.length,
    totalConnections,
    activeConnections,
    totalUploadSpeed,
    totalDownloadSpeed,
  }
})

// 生命周期
onMounted(() => {
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
})

// 方法
const toggleExpand = (pid: number) => {
  if (expandedItems.value.has(pid)) {
    expandedItems.value.delete(pid)
  } else {
    expandedItems.value.add(pid)
  }

  // 触发进程选择事件
  const process = props.connections.find(p => p.processId === pid)
  if (process) {
    emit('process-select', process)
  }
}

const getProcessStatus = (process: ProcessType) => {
  if (process.hasExited) return { type: 'error' as const, text: '已退出' }
  if (process.connections.some((c) => c.isActive)) return { type: 'success' as const, text: '活跃' }
  return { type: 'warning' as const, text: '空闲' }
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
  return formatBytes(bytesPerSecond) + '/s'
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

  return `${hours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`
}

const getTimeSinceActive = (lastActive: Date): string => {
  const diff = new Date().getTime() - lastActive.getTime()
  if (diff < 1000) return '刚刚'
  if (diff < 60000) return `${Math.floor(diff / 1000)}秒前`
  if (diff < 3600000) return `${Math.floor(diff / 60000)}分钟前`
  return `${Math.floor(diff / 3600000)}小时前`
}

const formatEndpoint = (endpoint: IPEndPoint): string => {
  if (isCompact.value && endpoint.address === '0.0.0.0') {
    return `*:${endpoint.port}`
  }
  return `${endpoint.address}:${endpoint.port}`
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
  overflow-y: auto;
  padding: 16px;
  box-sizing: border-box;
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

/* 进程列表 */
.process-list {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.size-compact .process-list {
  gap: 8px;
}

/* 过渡动画 */
.process-list-enter-active,
.process-list-leave-active {
  transition: all 0.3s ease;
}

.process-list-enter-from {
  opacity: 0;
  transform: translateX(-20px);
}

.process-list-leave-to {
  opacity: 0;
  transform: translateX(20px);
}

.process-item {
  background: var(--bg-card);
  backdrop-filter: var(--backdrop-blur);
  border: 1px solid var(--border-primary);
  border-radius: 12px;
  overflow: hidden;
  margin-top: 5px;
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
}

.size-compact .process-item {
  border-radius: 8px;
}

.process-item.is-exited {
  opacity: 0.8;
}

.process-item:hover {
  box-shadow: 0 4px 20px rgba(0, 0, 0, 0.08);
  border-color: var(--border-hover);
}

.process-item.is-expanded {
  border-color: #3b82f6;
  box-shadow: 0 4px 20px rgba(59, 130, 246, 0.15);
}

/* 进程头部 */
.process-header {
  padding: 16px 20px;
  display: flex;
  justify-content: space-between;
  align-items: center;
  cursor: pointer;
  transition: all 0.2s ease;
  border-bottom: 1px solid transparent;
}

.size-compact .process-header {
  padding: 12px;
}

.size-normal .process-header {
  padding: 14px 16px;
}

.process-item.is-expanded .process-header {
  border-bottom-color: var(--border-primary);
  background: rgba(59, 130, 246, 0.02);
}

.header-left {
  display: flex;
  align-items: center;
  gap: 12px;
  flex: 1;
  min-width: 0;
}

.size-compact .header-left {
  gap: 8px;
}

.expand-indicator {
  width: 24px;
  height: 24px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: var(--bg-tertiary);
  border-radius: 6px;
  transition: all 0.3s ease;
  flex-shrink: 0;
}

.size-compact .expand-indicator {
  width: 20px;
  height: 20px;
  border-radius: 4px;
}

.process-item.is-expanded .expand-indicator {
  background: rgba(59, 130, 246, 0.1);
  color: #3b82f6;
}

.expand-arrow {
  transition: transform 0.3s cubic-bezier(0.4, 0, 0.2, 1);
}

.process-item.is-expanded .expand-arrow {
  transform: rotate(90deg);
}

.process-info {
  flex: 1;
  min-width: 0;
}

.process-title {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 8px;
  flex-wrap: wrap;
}

.size-compact .process-title {
  gap: 6px;
  margin-bottom: 6px;
}

.process-name {
  font-size: 16px;
  font-weight: 600;
  color: var(--text-primary);
  margin: 0;
}

.size-compact .process-name {
  font-size: 14px;
}

.size-normal .process-name {
  font-size: 15px;
}

.process-details {
  display: flex;
  flex-wrap: wrap;
  gap: 16px;
  font-size: 12px;
  color: var(--text-muted);
}

.size-compact .process-details {
  gap: 8px;
  font-size: 11px;
}

.size-normal .process-details {
  gap: 12px;
}

.detail-item {
  display: flex;
  align-items: center;
  gap: 4px;
  white-space: nowrap;
}

.detail-item.primary {
  color: var(--text-secondary);
}

.header-right {
  display: flex;
  align-items: center;
  flex-shrink: 0;
}

.traffic-summary {
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  gap: 6px;
}

.size-compact .traffic-summary {
  gap: 4px;
}

.traffic-speed {
  display: flex;
  gap: 12px;
  font-size: 14px;
  font-weight: 500;
}

.size-compact .traffic-speed {
  gap: 8px;
  font-size: 12px;
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

/* 连接内容区域 */
.connections-content {
  border-top: 1px solid var(--border-primary);
}

.content-wrapper {
  padding: 16px 20px;
}

.size-compact .content-wrapper {
  padding: 12px;
}

.size-normal .content-wrapper {
  padding: 14px 16px;
}

.connections-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 12px;
  padding-bottom: 8px;
  border-bottom: 1px solid var(--border-secondary);
}

.size-compact .connections-header {
  margin-bottom: 8px;
  padding-bottom: 6px;
}

.connections-title {
  font-size: 14px;
  font-weight: 600;
  color: var(--text-secondary);
  margin: 0;
}

.size-compact .connections-title {
  font-size: 12px;
}

.connection-count {
  font-size: 12px;
  color: var(--text-muted);
}

/* 连接列表 */
.connection-list {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.size-compact .connection-list {
  gap: 6px;
}

.connection-item {
  background: var(--bg-tertiary);
  border: 1px solid var(--border-secondary);
  border-radius: 8px;
  padding: 12px;
  display: flex;
  justify-content: space-between;
  gap: 12px;
  transition: all 0.2s ease;
}

.size-compact .connection-item {
  padding: 8px;
  gap: 8px;
  border-radius: 6px;
}

.connection-item.is-active {
  background: rgba(52, 211, 153, 0.05);
  border-color: rgba(52, 211, 153, 0.2);
}

.connection-item:hover {
  background: var(--bg-hover);
  border-color: var(--border-hover);
}

.connection-main {
  display: flex;
  align-items: flex-start;
  gap: 12px;
  flex: 1;
  min-width: 0;
}

.size-compact .connection-main {
  gap: 8px;
}

.connection-status {
  display: flex;
  align-items: center;
  flex-shrink: 0;
  padding-top: 6px;
}

.size-compact .connection-status {
  padding-top: 4px;
}

.status-indicator {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  transition: all 0.3s ease;
}

.size-compact .status-indicator {
  width: 6px;
  height: 6px;
}

.status-indicator.active {
  background: #52c41a;
  box-shadow: 0 0 6px rgba(82, 196, 26, 0.4);
}

.status-indicator.inactive {
  background: #d9d9d9;
}

.connection-info {
  flex: 1;
  min-width: 0;
}

.connection-tags {
  display: flex;
  gap: 6px;
  margin-bottom: 8px;
  flex-wrap: wrap;
}

.size-compact .connection-tags {
  gap: 4px;
  margin-bottom: 6px;
}

.connection-addresses {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 6px;
  font-size: 12px;
  color: var(--text-primary);
  flex-wrap: wrap;
}

.size-compact .connection-addresses {
  font-size: 11px;
  gap: 6px;
  margin-bottom: 4px;
}

.address-section {
  display: flex;
  align-items: center;
  gap: 4px;
}

.address-value {
  font-family: 'Monaco', 'Menlo', monospace;
}

.connection-arrow {
  color: var(--text-quaternary);
}

.connection-meta {
  display: flex;
  gap: 16px;
  font-size: 11px;
  color: var(--text-muted);
  flex-wrap: wrap;
}

.meta-item {
  display: flex;
  align-items: center;
  gap: 4px;
}

.meta-item.inactive {
  color: var(--text-quaternary);
}

.connection-side {
  display: flex;
  align-items: flex-start;
  gap: 12px;
  flex-shrink: 0;
}

.size-compact .connection-side {
  gap: 8px;
}

.connection-traffic {
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  gap: 4px;
  min-width: 100px;
}

.size-compact .connection-traffic {
  min-width: 80px;
  gap: 2px;
}

.traffic-stats {
  display: flex;
  gap: 12px;
  font-size: 12px;
  font-weight: 500;
}

.size-compact .traffic-stats {
  gap: 8px;
  font-size: 11px;
}

.traffic-row {
  display: flex;
  align-items: center;
  gap: 4px;
}

.size-compact .traffic-row {
  gap: 2px;
}

.traffic-row.upload {
  color: #ef4444;
}

.traffic-row.download {
  color: #22c55e;
}

.traffic-value {
  min-width: 60px;
  text-align: right;
}

.size-compact .traffic-value {
  min-width: 45px;
}

.traffic-total-small {
  font-size: 10px;
  color: var(--text-muted);
  text-align: right;
}

.connection-actions {
  display: flex;
  gap: 4px;
  flex-shrink: 0;
}

/* CSS 变量定义 */
:root {
  --bg-card: rgba(255, 255, 255, 0.9);
  --bg-tertiary: #f1f5f9;
  --bg-quaternary: #e2e8f0;
  --bg-hover: #f8fafc;
  --backdrop-blur: blur(8px);
  --border-primary: #e2e8f0;
  --border-secondary: #f1f5f9;
  --border-hover: #cbd5e1;
  --text-primary: #0f172a;
  --text-secondary: #475569;
  --text-muted: #64748b;
  --text-quaternary: #94a3b8;
  --shadow-lg: 0 10px 30px rgba(0, 0, 0, 0.1);
  --transition: all 0.2s ease;
}

/* 暗色模式 */
@media (prefers-color-scheme: dark) {
  :root {
    --bg-card: rgba(30, 41, 59, 0.9);
    --bg-tertiary: #1e293b;
    --bg-quaternary: #334155;
    --bg-hover: #1e293b;
    --border-primary: #334155;
    --border-secondary: #1e293b;
    --border-hover: #475569;
    --text-primary: #f8fafc;
    --text-secondary: #cbd5e1;
    --text-muted: #94a3b8;
    --text-quaternary: #64748b;
  }

  .connection-item.is-active {
    background: rgba(52, 211, 153, 0.08);
    border-color: rgba(52, 211, 153, 0.3);
  }
}
</style>
