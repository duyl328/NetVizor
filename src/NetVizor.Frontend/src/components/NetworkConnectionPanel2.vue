<template>
  <div ref="containerRef" class="process-connection-panel" :class="sizeClass">
    <div class="panel-container">
      <!-- 进程概览统计 -->
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

      <!-- 进程列表 -->
      <div class="process-list">
        <div
          v-for="process in processData"
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
                    {{ formatDuration(process.startTime, process.exitTime) }}
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
                  <span
                    >总计: ↑{{ formatBytes(process.totalUploaded) }} ↓{{
                      formatBytes(process.totalDownloaded)
                    }}</span
                  >
                </div>
              </div>
            </div>
          </div>

          <!-- 连接列表 -->
          <div class="connections-content">
            <div class="content-wrapper">
              <div class="connections-header">
                <h5 class="connections-title">网络连接详情</h5>
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
                          持续: {{ formatDuration(connection.startTime) }}
                        </span>
                        <span v-if="!connection.isActive" class="meta-item inactive">
                          <n-icon :component="PauseCircleOutline" size="12" />
                          最后活跃: {{ getTimeSinceActive(connection.lastActiveTime) }}
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

                    <div class="connection-actions" v-if="!isCompact">
                      <n-button size="tiny" circle quaternary type="info" title="查看详情">
                        <template #icon>
                          <n-icon :component="InformationCircleOutline" />
                        </template>
                      </n-button>
                      <n-button size="tiny" circle quaternary type="warning" title="断开连接">
                        <template #icon>
                          <n-icon :component="StopCircleOutline" />
                        </template>
                      </n-button>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, watch } from 'vue'
import { NButton, NIcon, NTag, NEmpty } from 'naive-ui'
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

// 类型定义
type ProtocolType = 'TCP' | 'UDP' | 'ICMP' | 'Other'
type ConnectionState =
  | 'ESTABLISHED'
  | 'LISTEN'
  | 'TIME_WAIT'
  | 'CLOSE_WAIT'
  | 'SYN_SENT'
  | 'SYN_RECV'
  | 'CLOSING'
  | 'CLOSED'
type TrafficDirection = 'Inbound' | 'Outbound' | 'Both'
type SizeType = 'compact' | 'normal' | 'comfortable' | 'spacious'

interface IPEndPoint {
  address: string
  port: number
}

interface ConnectionInfo {
  connectionKey: string
  processId: number
  protocol: ProtocolType
  localEndpoint: IPEndPoint
  remoteEndpoint: IPEndPoint
  state: ConnectionState
  direction: TrafficDirection
  startTime: Date
  lastActiveTime: Date
  bytesSent: number
  bytesReceived: number
  currentSendSpeed: number
  currentReceiveSpeed: number
  isActive: boolean
}

interface ProcessType {
  processName: string
  processId: number
  startTime: Date
  hasExited: boolean
  exitTime?: Date
  exitCode?: number
  useMemory: number
  threadCount: number
  mainModulePath?: string
  mainModuleName?: string
  totalUploaded: number
  totalDownloaded: number
  uploadSpeed: number
  downloadSpeed: number
  connections: ConnectionInfo[]
}

// Props
interface Props {
  softwareName?: string
}

const props = withDefaults(defineProps<Props>(), {
  softwareName: '未知应用',
})

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
  // 使用不同的阈值来避免频繁切换
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

  // 如果没有跨越阈值，保持当前状态
  return currentType
}

// 使用计算属性而不是直接响应宽度变化
const sizeType = computed(() => actualSizeType.value)

const sizeClass = computed(() => `size-${sizeType.value}`)
const isCompact = computed(() => sizeType.value === 'compact')
const isNormal = computed(() => sizeType.value === 'normal')
const isComfortable = computed(() => sizeType.value === 'comfortable')

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
const expandedItems = ref(new Set<number>([1234]))

// 模拟数据
const processData = ref<ProcessType[]>([
  {
    processName: 'Chrome',
    processId: 1234,
    startTime: new Date(Date.now() - 8136000),
    hasExited: false,
    useMemory: 256901120,
    threadCount: 23,
    mainModulePath: '/Applications/Google Chrome.app/Contents/MacOS/Google Chrome',
    mainModuleName: 'Google Chrome',
    totalUploaded: 2202624,
    totalDownloaded: 16056832,
    uploadSpeed: 2355,
    downloadSpeed: 16076,
    connections: [
      {
        connectionKey: 'tcp-1234-1',
        processId: 1234,
        protocol: 'TCP',
        localEndpoint: { address: '192.168.1.100', port: 54321 },
        remoteEndpoint: { address: '142.250.191.78', port: 443 },
        state: 'ESTABLISHED',
        direction: 'Both',
        startTime: new Date(Date.now() - 932000),
        lastActiveTime: new Date(Date.now() - 2000),
        bytesSent: 1258496,
        bytesReceived: 8912896,
        currentSendSpeed: 1228,
        currentReceiveSpeed: 8704,
        isActive: true,
      },
      {
        connectionKey: 'tcp-1234-2',
        processId: 1234,
        protocol: 'TCP',
        localEndpoint: { address: '192.168.1.100', port: 54322 },
        remoteEndpoint: { address: '151.101.193.140', port: 443 },
        state: 'TIME_WAIT',
        direction: 'Outbound',
        startTime: new Date(Date.now() - 495000),
        lastActiveTime: new Date(Date.now() - 300000),
        bytesSent: 466944,
        bytesReceived: 5452800,
        currentSendSpeed: 0,
        currentReceiveSpeed: 0,
        isActive: false,
      },
      {
        connectionKey: 'tcp-1234-3',
        processId: 1234,
        protocol: 'TCP',
        localEndpoint: { address: '0.0.0.0', port: 8080 },
        remoteEndpoint: { address: '0.0.0.0', port: 0 },
        state: 'LISTEN',
        direction: 'Inbound',
        startTime: new Date(Date.now() - 8136000),
        lastActiveTime: new Date(Date.now() - 8136000),
        bytesSent: 0,
        bytesReceived: 0,
        currentSendSpeed: 0,
        currentReceiveSpeed: 0,
        isActive: true,
      },
    ],
  },
  {
    processName: 'VS Code',
    processId: 5678,
    startTime: new Date(Date.now() - 5025000),
    hasExited: false,
    useMemory: 134217728,
    threadCount: 15,
    mainModulePath: '/Applications/Visual Studio Code.app/Contents/MacOS/Electron',
    mainModuleName: 'Code',
    totalUploaded: 1258496,
    totalDownloaded: 8912896,
    uploadSpeed: 819,
    downloadSpeed: 4300,
    connections: [
      {
        connectionKey: 'tcp-5678-1',
        processId: 5678,
        protocol: 'TCP',
        localEndpoint: { address: '192.168.1.100', port: 12345 },
        remoteEndpoint: { address: '140.82.114.3', port: 443 },
        state: 'ESTABLISHED',
        direction: 'Both',
        startTime: new Date(Date.now() - 5025000),
        lastActiveTime: new Date(Date.now() - 5000),
        bytesSent: 1258496,
        bytesReceived: 8912896,
        currentSendSpeed: 307,
        currentReceiveSpeed: 2150,
        isActive: true,
      },
    ],
  },
  {
    processName: 'node',
    processId: 9012,
    startTime: new Date(Date.now() - 2718000),
    hasExited: true,
    exitTime: new Date(Date.now() - 300000),
    exitCode: 0,
    useMemory: 67108864,
    threadCount: 8,
    mainModulePath: '/usr/local/bin/node',
    mainModuleName: 'node',
    totalUploaded: 0,
    totalDownloaded: 0,
    uploadSpeed: 0,
    downloadSpeed: 0,
    connections: [],
  },
])

// 计算属性
const stats = computed(() => {
  const activeProcesses = processData.value.filter((p) => !p.hasExited)
  const totalConnections = processData.value.reduce((sum, p) => sum + p.connections.length, 0)
  const activeConnections = processData.value.reduce(
    (sum, p) => sum + p.connections.filter((c) => c.isActive).length,
    0,
  )
  const totalUploadSpeed = activeProcesses.reduce((sum, p) => sum + p.uploadSpeed, 0)
  const totalDownloadSpeed = activeProcesses.reduce((sum, p) => sum + p.downloadSpeed, 0)

  return {
    activeProcesses: activeProcesses.length,
    totalProcesses: processData.value.length,
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
}

const getProcessStatus = (process: ProcessType) => {
  if (process.hasExited) return { type: 'error' as const, text: '已退出' }
  if (process.connections.some((c) => c.isActive)) return { type: 'success' as const, text: '活跃' }
  return { type: 'warning' as const, text: '空闲' }
}

const getConnectionStateType = (state: ConnectionState, isActive: boolean) => {
  if (!isActive) return 'default' as const
  switch (state) {
    case 'ESTABLISHED':
      return 'success' as const
    case 'LISTEN':
      return 'info' as const
    case 'TIME_WAIT':
    case 'CLOSE_WAIT':
      return 'warning' as const
    default:
      return 'default' as const
  }
}

const getDirectionIcon = (direction: TrafficDirection) => {
  switch (direction) {
    case 'Inbound':
      return ArrowDownOutline
    case 'Outbound':
      return ArrowUpOutline
    case 'Both':
      return SwapHorizontalOutline
  }
}

const getDirectionText = (direction: TrafficDirection) => {
  switch (direction) {
    case 'Inbound':
      return '入站'
    case 'Outbound':
      return '出站'
    case 'Both':
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
</script>

<style scoped>
.process-connection-panel {
  height: 100%;
  overflow-y: auto;
  background: var(--bg-secondary);
  /* 使用 box-sizing 确保一致的尺寸计算 */
  box-sizing: border-box;
}

.panel-container {
  padding: 20px;
  max-width: 1400px;
  margin: 0 auto;
  /* 确保内部元素不会影响外部容器的宽度 */
  box-sizing: border-box;
}

/* 响应式调整 - 保持 padding 但使用 box-sizing */
.size-compact .panel-container {
  padding: 12px;
}

.size-normal .panel-container {
  padding: 16px;
}

/* 统计卡片样式 */
.stats-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 16px;
  margin-bottom: 20px;
}

.size-compact .stats-grid {
  grid-template-columns: repeat(2, 1fr);
  gap: 8px;
  margin-bottom: 12px;
}

.size-normal .stats-grid {
  grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
  gap: 12px;
  margin-bottom: 16px;
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

.size-normal .stat-label {
  font-size: 12px;
}

/* 进程列表样式 */
.process-list {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.size-compact .process-list {
  gap: 8px;
}

.process-item {
  background: var(--bg-card);
  backdrop-filter: var(--backdrop-blur);
  border: 1px solid var(--border-primary);
  border-radius: 12px;
  overflow: hidden;
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

/* 连接内容区域 - 修改 max-height 动画 */
.connections-content {
  height: 0;
  overflow: hidden;
  transition: height 0.3s cubic-bezier(0.4, 0, 0.2, 1);
}

.process-item.is-expanded .connections-content {
  height: auto;
}

.content-wrapper {
  padding: 0 20px 16px 20px;
}

.size-compact .content-wrapper {
  padding: 0 12px 12px 12px;
}

.size-normal .content-wrapper {
  padding: 0 16px 14px 16px;
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
  --bg-secondary: #f8fafc;
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
    --bg-secondary: #0f172a;
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
