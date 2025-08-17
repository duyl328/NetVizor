<template>
  <div ref="mainViewRef" class="main-view">
    <!-- 顶部 - 搜索和标题 -->
    <div class="main-header" :style="{ height: headerHeight + 'px' }">
      <div class="header-content">
        <div class="header-info">
          <h2 class="main-header-title">
            {{ isFiltering && filterText ? '过滤结果 - 网络连接监控' : '网络连接监控' }}
          </h2>
          <p class="main-header-subtitle">
            {{
              isFiltering && filterText
                ? `正在显示包含 "${filterText}" 的连接信息`
                : '系统网络活动监控'
            }}
          </p>
        </div>

        <div class="header-actions" v-if="false">
          <div class="search-area">
            <n-input
              v-model:value="filterText"
              placeholder="搜索连接、IP、端口、进程名..."
              size="medium"
              clearable
              @keydown.enter="handleFilter"
              @keydown.escape="handleClearFilter"
              @clear="handleClearFilter"
            >
              <template #prefix>
                <n-icon :component="SearchOutline" />
              </template>
            </n-input>
          </div>

          <div class="filter-buttons">
            <n-button
              size="small"
              :type="isFiltering ? 'primary' : 'default'"
              @click="handleFilter"
            >
              {{ isFiltering ? '取消过滤' : '过滤' }}
            </n-button>
            <n-button size="small" quaternary @click="handleRefresh">
              刷新
            </n-button>
          </div>
        </div>
      </div>
    </div>

    <!-- 统计卡片区域 -->
    <div class="stats-section">
      <div class="stats-grid">
        <div class="stat-card">
          <div class="stat-icon-wrapper process">
            <n-icon :component="DesktopOutline" :size="24" />
          </div>
          <div class="stat-content">
            <div class="stat-number">{{ stats.activeProcesses }}</div>
            <div class="stat-label">活跃进程 / {{ stats.totalProcesses }}</div>
          </div>
        </div>

        <div class="stat-card">
          <div class="stat-icon-wrapper connection">
            <n-icon :component="GitNetworkOutline" :size="24" />
          </div>
          <div class="stat-content">
            <div class="stat-number">{{ stats.activeConnections }}</div>
            <div class="stat-label">活跃连接 / {{ stats.totalConnections }}</div>
          </div>
        </div>

        <div class="stat-card">
          <div class="stat-icon-wrapper upload">
            <n-icon :component="ArrowUpOutline" :size="24" />
          </div>
          <div class="stat-content">
            <div class="stat-number">{{ formatSpeed(stats.totalUploadSpeed) }}</div>
            <div class="stat-label">总上传速度</div>
          </div>
        </div>

        <div class="stat-card">
          <div class="stat-icon-wrapper download">
            <n-icon :component="ArrowDownOutline" :size="24" />
          </div>
          <div class="stat-content">
            <div class="stat-number">{{ formatSpeed(stats.totalDownloadSpeed) }}</div>
            <div class="stat-label">总下载速度</div>
          </div>
        </div>
      </div>
    </div>

    <!-- 进程过滤器 -->
    <div class="process-filter">
      <div class="filter-header">
        <div class="filter-title-section">
          <h3 class="filter-title">进程过滤器</h3>
          <div class="filter-actions">
            <n-button
              size="tiny"
              quaternary
              @click="toggleProcessDetails"
              :type="showProcessDetails ? 'primary' : 'default'"
            >
              <template #icon>
                <n-icon :component="showProcessDetails ? EyeOutline : SettingsOutline" />
              </template>
              {{ showProcessDetails ? '隐藏详情' : '显示详情' }}
            </n-button>
            <n-button
              size="tiny"
              quaternary
              @click="toggleAllProcesses"
            >
              {{ selectedProcessIds.size === 0 ? '全选' : '清空' }}
            </n-button>
          </div>
        </div>
        <div class="filter-stats">
          <span class="stats-text">
            已选择 {{ selectedProcessIds.size }} / {{ displayProcesses.length }} 个进程
            {{ selectedProcessIds.size > 0 ? `· ${displayConnections.length} 个连接` : '' }}
          </span>
        </div>
      </div>

      <div class="process-grid">
        <div
          v-for="process in processSelectors"
          :key="process.processId"
          class="process-card"
          :class="{
            'is-selected': process.isSelected,
            'is-active': !process.hasExited,
            'is-exited': process.hasExited
          }"
          @click="toggleProcessSelection(process.processId)"
        >
          <div class="process-card-header">
            <div class="process-card-icon">
              <n-icon :component="DesktopOutline" size="20" />
            </div>
            <div class="process-card-status">
              <div
                class="status-indicator"
                :class="process.hasExited ? 'exited' : 'active'"
              ></div>
            </div>
            <div v-if="process.isSelected" class="process-selected-indicator">
              <!-- 移除对号图标 -->
            </div>
          </div>

          <div class="process-card-content">
            <div class="process-card-name" :title="process.processName">
              {{ process.processName }}
            </div>
            <div class="process-card-basic-info">
              <span class="basic-info-item">PID: {{ process.processId }}</span>
              <span class="basic-info-item connections">
                <n-icon :component="GitNetworkOutline" size="12" />
                {{ process.activeConnectionCount }}/{{ process.connectionCount }}
              </span>
              <span class="basic-info-item">
                <n-icon :component="TimeOutline" size="12" />
                {{ formatUptime(process.startTime) }}
              </span>
              <span class="basic-info-item">
                <n-icon :component="HardwareChipOutline" size="12" />
                {{ formatBytes(process.useMemory) }}
              </span>
            </div>

            <!-- 详细信息（仅在选中且开启详情时显示） -->
            <div v-if="process.isSelected && showProcessDetails" class="process-card-details">
              <div class="detail-row">
                <span class="detail-item">
                  <n-icon :component="GitBranchOutline" size="12" />
                  {{ process.threadCount }} 线程
                </span>
                <span class="detail-item">
                  总流量: {{ formatBytes(process.totalTraffic) }}
                </span>
              </div>
              <div class="detail-row traffic">
                <span class="detail-item upload">
                  <n-icon :component="ArrowUpOutline" size="12" />
                  {{ formatSpeed(process.uploadSpeed || 0) }}
                </span>
                <span class="detail-item download">
                  <n-icon :component="ArrowDownOutline" size="12" />
                  {{ formatSpeed(process.downloadSpeed || 0) }}
                </span>
              </div>
              <div v-if="process.mainModulePath" class="detail-row">
                <span class="detail-item path" :title="process.mainModulePath">
                  路径: {{ process.mainModuleFileName }}
                </span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- 连接列表容器 -->
    <div class="connections-container">
      <div class="connections-header">
        <h3 class="connections-title">
          网络连接
          <span class="connections-count">
            ({{ displayConnections.length }} 个连接)
          </span>
        </h3>
        <div class="connections-actions">
          <n-select
            v-model:value="sortBy"
            size="small"
            style="width: 120px"
            :options="sortOptions"
          />
        </div>
      </div>

      <!-- Vue Virtual Scroller - 表格化连接列表 -->
      <div class="connections-table">
        <div class="table-header">
          <div class="table-cell header-cell protocol">协议</div>
          <div class="table-cell header-cell status">状态</div>
          <div class="table-cell header-cell endpoints">连接信息</div>
          <div class="table-cell header-cell traffic">流量</div>
          <div class="table-cell header-cell time">时间</div>
        </div>

        <RecycleScroller
          ref="virtualListRef"
          class="connections-scroller"
          :style="{ height: virtualListHeight }"
          :items="displayConnections"
          :item-size="60"
          key-field="key"
          v-slot="{ item }"
        >
          <div
            class="table-row connection-row"
            :class="{
              'is-active': item.isActive,
              'is-selected-process': selectedProcessIds.has(item.processId)
            }"
          >
            <div class="table-cell protocol-cell">
              <n-tag 
                :type="getProtocolTagType(item.protocol)"
                size="tiny" 
                round
              >
                {{ formatProtocol(item.protocol) }}
              </n-tag>
            </div>

            <div class="table-cell status-cell">
              <div
                class="status-indicator"
                :class="item.isActive ? 'active' : 'inactive'"
                :title="item.isActive ? '活跃' : '空闲'"
              ></div>
              <n-tag
                :type="getConnectionStateTagType(item.state, item.isActive)"
                size="tiny"
                round
              >
                {{ formatConnectionState(item.state) }}
              </n-tag>
            </div>

            <div class="table-cell endpoints-cell">
              <div class="endpoint-row">
                <div class="endpoint local">
                  <n-icon :component="LocationOutline" size="12" />
                  <span class="endpoint-address">{{ formatEndpoint(item.localEndpoint) }}</span>
                </div>
                <n-icon :component="ArrowForwardOutline" size="12" class="connection-arrow" />
                <div class="endpoint remote">
                  <n-icon :component="GlobeOutline" size="12" />
                  <span class="endpoint-address">{{ formatEndpoint(item.remoteEndpoint) }}</span>
                </div>
              </div>
            </div>

            <div class="table-cell traffic-cell">
              <div class="traffic-row">
                <div class="traffic-item upload">
                  <n-icon :component="ArrowUpOutline" size="12" />
                  <span>{{ formatSpeed(item.currentSendSpeed || 0) }}</span>
                </div>
                <div class="traffic-item download">
                  <n-icon :component="ArrowDownOutline" size="12" />
                  <span>{{ formatSpeed(item.currentReceiveSpeed || 0) }}</span>
                </div>
              </div>
            </div>

            <div class="table-cell time-cell">
              <div v-if="item.startTime" class="time-info">
                <span class="duration">{{ formatDuration(new Date(item.startTime)) }}</span>
                <span v-if="!item.isActive && item.lastActiveTime" class="last-active">
                  {{ getTimeSinceActive(new Date(item.lastActiveTime)) }}前
                </span>
              </div>
            </div>
          </div>
        </RecycleScroller>
      </div>

      <!-- 无数据提示 -->
      <div v-if="displayConnections.length === 0" class="empty-state">
        <div class="empty-icon">
          <n-icon :component="CloudOfflineOutline" size="64" />
        </div>
        <h3>暂无网络连接数据</h3>
        <p>{{ allConnections.length === 0 ? '系统中暂无活跃的网络连接' : '当前过滤条件下无匹配的连接' }}</p>
        <div v-if="selectedProcessIds.size > 0" class="empty-actions">
          <n-button size="small" @click="selectedProcessIds.clear(); selectedProcessIds = new Set()">
            清除进程过滤
          </n-button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch, onUnmounted, nextTick } from 'vue'
import { NButton, NInput, NIcon, NTag, NTooltip, NSelect } from 'naive-ui'
import {
  SearchOutline,
  DesktopOutline,
  GitNetworkOutline,
  ArrowUpOutline,
  ArrowDownOutline,
  HardwareChipOutline,
  GitBranchOutline,
  LocationOutline,
  GlobeOutline,
  ArrowForwardOutline,
  TimeOutline,
  PauseCircleOutline,
  InformationCircleOutline,
  StopCircleOutline,
  CloudOfflineOutline,
  EyeOutline,
  SettingsOutline,
} from '@vicons/ionicons5'
import { RecycleScroller } from 'vue-virtual-scroller'
import { useApplicationStore } from '@/stores/application'
import { storeToRefs } from 'pinia'
import { ResponseModel, SubscriptionInfo, SubscriptionProcessInfo } from '@/types/response'
import { httpClient } from '@/utils/http'
import { useWebSocketStore } from '@/stores/websocketStore'
import { useProcessStore } from '@/stores/processInfo'
import { useFilterStore } from '@/stores/filterStore'
import { useTrafficStore } from '@/stores/trafficStore'
import type { ProcessType, ConnectionInfo, IPEndPoint } from '@/types/process'
import { ConnectionState, FILE_SIZE_UNIT_ENUM } from '@/constants/enums'
import { convertFileSize } from '@/utils/fileUtil'
import { environmentDetector } from '@/utils/environmentDetector'
import { mockDataService } from '@/utils/mockDataService'
import 'vue-virtual-scroller/dist/vue-virtual-scroller.css'

const webSocketStore = useWebSocketStore()
const { isOpen } = storeToRefs(webSocketStore)

const applicationStore = useApplicationStore()
const { selectedApp } = storeToRefs(applicationStore)

const processStore = useProcessStore()
const { processInfos } = storeToRefs(processStore)

const filterStore = useFilterStore()
const { filterText, isFiltering, filteredProcesses } = storeToRefs(filterStore)

const trafficStore = useTrafficStore()

// 立即输出基础日志
console.log(`[${new Date().toLocaleTimeString()}] [MonitorMainPanel] 🔥 组件脚本开始执行`)
console.log(`[${new Date().toLocaleTimeString()}] [MonitorMainPanel] 🌍 当前模式:`, import.meta.env.MODE)
console.log(`[${new Date().toLocaleTimeString()}] [MonitorMainPanel] 🎭 DEMO模式:`, import.meta.env.VITE_DEMO_MODE)

// 为应用生成相关的进程数据
const generateRelatedProcesses = async (appName: string): Promise<ProcessType[]> => {
  const processes: ProcessType[] = []
  
  // 生成2-3个与应用相关的进程
  const processCount = Math.random() > 0.5 ? 2 : 3
  
  for (let i = 0; i < processCount; i++) {
    const relatedProcessName = mockDataService.getRelatedProcessName(appName)
    const process = mockDataService.generateProcessInfo()
    
    // 修改进程名为相关名称
    process.processName = relatedProcessName
    process.mainModuleName = relatedProcessName
    process.mainModulePath = `C:\\Program Files\\${appName}\\${relatedProcessName}`
    
    processes.push(process)
  }
  
  return processes
}

// Props
const props = defineProps<{
  width?: number
}>()

// 组件引用
const mainViewRef = ref<HTMLElement>()
const virtualListRef = ref()

// 布局尺寸
const headerHeight = ref(80)
const mainViewHeight = ref(600)

// 选中的进程ID集合
const selectedProcessIds = ref<Set<number>>(new Set())

// 进程详细信息显示控制
const showProcessDetails = ref(false)

// 排序选项
const sortBy = ref('time')
const sortOptions = [
  { label: '时间', value: 'time' },
  { label: '流量', value: 'traffic' },
  { label: '状态', value: 'state' },
  { label: '进程', value: 'process' },
]

// 计算虚拟列表高度
const virtualListHeight = computed(() => {
  const totalHeight = mainViewHeight.value
  const usedHeight = headerHeight.value + 140 + 120 + 50 // header + stats + selector + connections header
  return `${Math.max(200, totalHeight - usedHeight)}px`
})

// 计算统计数据
const stats = computed(() => {
  const activeProcesses = displayProcesses.value.filter((p) => !p.hasExited)
  const totalConnections = displayProcesses.value.reduce((sum, p) => sum + p.connections.length, 0)
  const activeConnections = displayProcesses.value.reduce(
    (sum, p) => sum + p.connections.filter((c) => c.isActive).length,
    0,
  )
  const totalUploadSpeed = activeProcesses.reduce((sum, p) => sum + (p.uploadSpeed || 0), 0)
  const totalDownloadSpeed = activeProcesses.reduce((sum, p) => sum + (p.downloadSpeed || 0), 0)

  return {
    activeProcesses: activeProcesses.length,
    totalProcesses: displayProcesses.value.length,
    totalConnections,
    activeConnections,
    totalUploadSpeed,
    totalDownloadSpeed,
  }
})

// 数据收集定时器
let dataCollectionTimer: number | null = null

// 收集网络流量数据
const collectTrafficData = () => {
  const currentStats = stats.value
  trafficStore.addTrafficData(
    currentStats.totalUploadSpeed,
    currentStats.totalDownloadSpeed
  )
}

// 启动数据收集
const startDataCollection = () => {
  // 立即收集一次数据
  collectTrafficData()
  
  // 每20秒收集一次数据
  dataCollectionTimer = window.setInterval(() => {
    collectTrafficData()
  }, trafficStore.dataInterval)
}

// 停止数据收集
const stopDataCollection = () => {
  if (dataCollectionTimer) {
    clearInterval(dataCollectionTimer)
    dataCollectionTimer = null
  }
}

// 演示模式下根据选中应用过滤进程
const displayProcesses = computed(() => {
  if (environmentDetector.shouldUseMockData() && selectedApp.value) {
    // 演示模式：根据选中应用过滤相关进程
    const appName = selectedApp.value.name || selectedApp.value.processName
    const relatedProcesses = filteredProcesses.value.filter(process => {
      // 进程名包含应用名，或者是常见的系统进程
      const processName = process.processName.toLowerCase()
      const targetApp = appName.toLowerCase()
      
      return processName.includes(targetApp) || 
             processName.includes(targetApp.replace('.exe', '')) ||
             ['svchost.exe', 'system'].includes(processName) // 保留一些系统进程
    })
    
    // 如果没有找到相关进程，返回前2个进程作为示例
    if (relatedProcesses.length === 0) {
      return filteredProcesses.value.slice(0, 2)
    }
    
    // 最多显示3个相关进程
    return relatedProcesses.slice(0, 3)
  }
  
  // 非演示模式：使用所有过滤后的进程
  return filteredProcesses.value
})

// 获取所有连接列表（统一显示）
const allConnections = computed(() => {
  const connections: unknown[] = []

  displayProcesses.value.forEach((process) => {
    if (process.connections && Array.isArray(process.connections)) {
      process.connections.forEach((connection, index) => {
        connections.push({
          key: `${process.processId}-${index}`,
          ...connection,
          processId: process.processId,
          processName: process.processName,
          processInfo: process, // 添加完整的进程信息
        })
      })
    }
  })

  console.log(`[${new Date().toLocaleTimeString()}] [MonitorMainPanel] 计算连接列表: ${displayProcesses.value.length} 个进程, ${connections.length} 个连接`)
  return connections
})

// 根据选中的进程过滤连接
const displayConnections = computed(() => {
  let connections = allConnections.value

  // 如果选中了特定进程，只显示这些进程的连接
  if (selectedProcessIds.value.size > 0) {
    connections = connections.filter(conn => selectedProcessIds.value.has(conn.processId))
  }

  // 根据排序选项排序
  return connections.sort((a, b) => {
    switch (sortBy.value) {
      case 'time':
        return new Date(b.startTime).getTime() - new Date(a.startTime).getTime()
      case 'traffic':
        const aTraffic = (a.currentSendSpeed || 0) + (a.currentReceiveSpeed || 0)
        const bTraffic = (b.currentSendSpeed || 0) + (b.currentReceiveSpeed || 0)
        return bTraffic - aTraffic
      case 'state':
        const aState = String(a.state || '')
        const bState = String(b.state || '')
        return aState.localeCompare(bState)
      case 'process':
        return a.processName.localeCompare(b.processName)
      default:
        return 0
    }
  })
})

// 获取进程选择器显示的进程信息
const processSelectors = computed(() => {
  return displayProcesses.value.map(process => ({
    ...process,
    isSelected: selectedProcessIds.value.has(process.processId),
    connectionCount: process.connections.length,
    activeConnectionCount: process.connections.filter(c => c.isActive).length,
    // 添加更多源信息
    totalTraffic: (process.totalUploaded || 0) + (process.totalDownloaded || 0),
    currentTraffic: (process.uploadSpeed || 0) + (process.downloadSpeed || 0),
    mainModuleFileName: process.mainModuleName || process.processName,
    uptimeMs: new Date().getTime() - new Date(process.startTime).getTime(),
  }))
})


// 切换进程选择
const toggleProcessSelection = (processId: number) => {
  if (selectedProcessIds.value.has(processId)) {
    selectedProcessIds.value.delete(processId)
  } else {
    selectedProcessIds.value.add(processId)
  }
  selectedProcessIds.value = new Set(selectedProcessIds.value)
}

// 切换进程详细信息显示
const toggleProcessDetails = () => {
  showProcessDetails.value = !showProcessDetails.value
}

// 格式化协议类型
const formatProtocol = (protocol: unknown): string => {
  // 如果是数字，转换为对应的协议名称
  if (typeof protocol === 'number') {
    switch (protocol) {
      case 6:
        return 'TCP'
      case 17:
        return 'UDP'
      case 1:
        return 'ICMP'
      default:
        return `协议${protocol}`
    }
  }
  // 如果已经是字符串，直接返回
  return String(protocol || 'Unknown')
}

// 格式化连接状态
const formatConnectionState = (state: unknown): string => {
  // 如果是数字，转换为对应的状态名称
  if (typeof state === 'number') {
    switch (state) {
      case 1:
        return '已建立' // ESTABLISHED
      case 2:
        return '正在监听' // LISTEN
      case 3:
        return '等待关闭' // TIME_WAIT
      case 4:
        return '等待远程关闭' // CLOSE_WAIT
      case 5:
        return '正在关闭' // CLOSING
      case 6:
        return '已关闭' // CLOSED
      case 7:
        return '正在连接' // SYN_SENT
      case 8:
        return '连接请求已收到' // SYN_RECEIVED
      case 9:
        return '最后ACK等待' // LAST_ACK
      default:
        return `状态${state}`
    }
  }
  // 如果已经是字符串，进行中文翻译
  const stateStr = String(state || 'Unknown')
  switch (stateStr) {
    case 'ESTABLISHED':
      return '已建立'
    case 'LISTEN':
      return '正在监听'
    case 'TIME_WAIT':
      return '等待关闭'
    case 'CLOSE_WAIT':
      return '等待远程关闭'
    case 'CLOSING':
      return '正在关闭'
    case 'CLOSED':
      return '已关闭'
    case 'SYN_SENT':
      return '正在连接'
    case 'SYN_RECEIVED':
      return '连接请求已收到'
    case 'LAST_ACK':
      return '最后ACK等待'
    case 'Connecting':
      return '连接中'
    case 'Connected':
      return '已连接'
    case 'Disconnected':
      return '已断开'
    case 'Listening':
      return '正在监听'
    default:
      return stateStr
  }
}

// 格式化存活时间
const formatUptime = (startTime: Date): string => {
  const diff = new Date().getTime() - new Date(startTime).getTime()
  const days = Math.floor(diff / 86400000)
  const hours = Math.floor((diff % 86400000) / 3600000)
  const minutes = Math.floor((diff % 3600000) / 60000)

  if (days > 0) {
    return `${days}天${hours}小时`
  } else if (hours > 0) {
    return `${hours}小时${minutes}分钟`
  } else {
    return `${minutes}分钟`
  }
}

// 全选/清空进程
const toggleAllProcesses = () => {
  if (selectedProcessIds.value.size === 0) {
    displayProcesses.value.forEach(p => selectedProcessIds.value.add(p.processId))
  } else {
    selectedProcessIds.value.clear()
  }
  selectedProcessIds.value = new Set(selectedProcessIds.value)
}

// 格式化函数
const formatBytes = (bytes: number): string => {
  if (bytes === 0) return '0 B'
  const k = 1024
  const sizes = ['B', 'KB', 'MB', 'GB', 'TB']
  const i = Math.floor(Math.log(bytes) / Math.log(k))
  const value = parseFloat((bytes / Math.pow(k, i)).toFixed(2))
  return value + ' ' + sizes[i]
}

const formatSpeed = (bytesPerSecond: number): string => {
  const fileSize = convertFileSize(bytesPerSecond, FILE_SIZE_UNIT_ENUM.B)
  return fileSize.size + fileSize.unit + '/s'
}

const formatEndpoint = (endpoint: IPEndPoint): string => {
  return `${endpoint.address}:${endpoint.port}`
}

const formatDuration = (start: Date): string => {
  const diff = new Date().getTime() - start.getTime()
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

// 获取协议标签类型
const getProtocolTagType = (protocol: unknown) => {
  const protocolStr = formatProtocol(protocol)
  switch (protocolStr) {
    case 'TCP':
      return 'info' as const
    case 'UDP':
      return 'warning' as const
    case 'ICMP':
      return 'error' as const
    default:
      return 'default' as const
  }
}

// 获取连接状态标签类型
const getConnectionStateTagType = (state: unknown, isActive: boolean) => {
  if (!isActive) return 'default' as const

  const stateStr = String(state || '')
  // 检查数字状态
  if (typeof state === 'number') {
    switch (state) {
      case 1: // ESTABLISHED
        return 'success' as const
      case 2: // LISTEN
        return 'info' as const
      case 3: // TIME_WAIT
      case 4: // CLOSE_WAIT
        return 'warning' as const
      case 5: // CLOSING
      case 6: // CLOSED
        return 'error' as const
      case 7: // SYN_SENT
      case 8: // SYN_RECEIVED
        return 'primary' as const
      default:
        return 'default' as const
    }
  }

  // 检查字符串状态
  switch (stateStr) {
    case 'ESTABLISHED':
      return 'success' as const
    case 'LISTEN':
    case 'Listening':
      return 'info' as const
    case 'TIME_WAIT':
    case 'CLOSE_WAIT':
      return 'warning' as const
    case 'CLOSING':
    case 'CLOSED':
    case 'Disconnected':
      return 'error' as const
    case 'SYN_SENT':
    case 'SYN_RECEIVED':
    case 'Connecting':
      return 'primary' as const
    case 'Connected':
      return 'success' as const
    default:
      return 'default' as const
  }
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

// 事件处理函数
const handleFilter = () => {
  if (isFiltering.value) {
    filterStore.clearFilter()
  } else {
    filterStore.applyFilter()
  }
}

const handleClearFilter = () => {
  filterStore.clearFilter()
}

const handleRefresh = () => {
  console.log('刷新数据')
  processStore.refresh?.()
}

const handleViewDetails = (connection: ConnectionInfo) => {
  console.log('查看连接详情:', connection)
}

const handleDisconnect = (connection: ConnectionInfo) => {
  console.log('断开连接:', connection)
}

// 更新主视图高度
const updateMainViewHeight = () => {
  if (mainViewRef.value) {
    mainViewHeight.value = mainViewRef.value.clientHeight
  }
}

// 监听选中应用的变化
watch(selectedApp, async (newVal, oldVal) => {
  console.log(`[${new Date().toLocaleTimeString()}] [MonitorMainPanel] 🔄 应用切换:`, newVal?.name)
  
  if (!environmentDetector.shouldUseMockData()) {
    // 非演示模式：清空进程数据
    processStore.clear()
    console.log(`[${new Date().toLocaleTimeString()}] [MonitorMainPanel] 🗑️ 清空进程数据（非演示模式）`)
  } else if (newVal) {
    // 演示模式：为新选中的应用生成相关进程数据
    console.log(`[${new Date().toLocaleTimeString()}] [MonitorMainPanel] 🎭 演示模式为应用生成相关进程:`, newVal.name)
    
    try {
      // 生成与应用相关的进程数据
      const relatedProcesses = await generateRelatedProcesses(newVal.name)
      console.log(`[${new Date().toLocaleTimeString()}] [MonitorMainPanel] ✅ 生成了${relatedProcesses.length}个相关进程`)
      
      // 替换进程数据
      processStore.clear()
      processInfos.value.push(...relatedProcesses)
    } catch (error) {
      console.error(`[${new Date().toLocaleTimeString()}] [MonitorMainPanel] ❌ 生成相关进程失败:`, error)
    }
  }
  
  filterStore.clearFilter()
  selectedProcessIds.value.clear()
  
  // 演示模式下应用切换后重新自动选择进程
  if (environmentDetector.shouldUseMockData() && newVal) {
    // 等待下一个tick确保数据更新完成
    await nextTick()
    
    // 自动选择新生成的进程
    const newProcesses = processInfos.value.filter(p => p.connections && p.connections.length > 0)
    if (newProcesses.length > 0) {
      newProcesses.forEach(p => selectedProcessIds.value.add(p.processId))
      selectedProcessIds.value = new Set(selectedProcessIds.value)
      
      console.log(`[${new Date().toLocaleTimeString()}] [MonitorMainPanel] 🎯 应用切换后自动选择进程:`, {
        进程数: newProcesses.length,
        进程名: newProcesses.map(p => p.processName)
      })
    }
  }
  
  console.log(`[${new Date().toLocaleTimeString()}] [MonitorMainPanel] 🎯 应用切换完成:`, newVal?.id)
})

onMounted(async () => {
  console.log(`[${new Date().toLocaleTimeString()}] [MonitorMainPanel] 🚀 组件已挂载，开始初始化`)
  
  nextTick(() => {
    updateMainViewHeight()
  })

  window.addEventListener('resize', updateMainViewHeight)

  // 订阅进程信息 - 在组件挂载后执行
  console.log(`[${new Date().toLocaleTimeString()}] [MonitorMainPanel] 💥 开始订阅进程信息`)
  console.log(`[${new Date().toLocaleTimeString()}] [MonitorMainPanel] 📊 当前进程数量:`, processInfos.value.length)

  try {
    await processStore.subscribe()
    console.log(`[${new Date().toLocaleTimeString()}] [MonitorMainPanel] ✅ 进程信息订阅完成，当前数量:`, processInfos.value.length)
  } catch (error) {
    console.error(`[${new Date().toLocaleTimeString()}] [MonitorMainPanel] ❌ 进程信息订阅失败:`, error)
  }

  // 启动数据收集
  startDataCollection()

  // 演示模式下自动选择进程
  if (environmentDetector.shouldUseMockData()) {
    // 监听显示的进程数据变化，自动选择进程
    let stopWatcher: (() => void) | null = null
    
    stopWatcher = watch(
      displayProcesses,
      (newProcesses) => {
        if (newProcesses.length > 0 && selectedProcessIds.value.size === 0) {
          // 自动选择所有显示的进程（已经过滤过，数量不多）
          const processesToSelect = newProcesses.filter(p => p.connections && p.connections.length > 0)
          
          processesToSelect.forEach(p => selectedProcessIds.value.add(p.processId))
          selectedProcessIds.value = new Set(selectedProcessIds.value)
          
          const totalConnections = processesToSelect.reduce((sum, p) => sum + p.connections.length, 0)
          console.log(`[${new Date().toLocaleTimeString()}] [MonitorMainPanel] 演示模式自动选择进程:`, {
            进程数: processesToSelect.length,
            进程名: processesToSelect.map(p => p.processName),
            总连接数: totalConnections
          })
          
          // 只执行一次，然后停止监听
          if (stopWatcher) stopWatcher()
        }
      },
      { immediate: true, flush: 'sync' }
    )
  }

  watch(
    [isOpen, selectedApp],
    ([newValue, newValue1]) => {
      if (newValue && newValue1 && newValue1.processIds && newValue1.processIds.length > 0) {
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
  window.removeEventListener('resize', updateMainViewHeight)

  // 停止数据收集
  stopDataCollection()

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
  background: var(--bg-primary);
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
  color: var(--text-primary);
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

/* 统计卡片区域 */
.stats-section {
  padding: 16px 24px;
  background: var(--bg-secondary);
  border-bottom: 1px solid var(--border-primary);
}

.stats-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 16px;
}

.stat-card {
  background: var(--bg-card);
  border: 1px solid var(--border-primary);
  border-radius: 12px;
  padding: 16px;
  display: flex;
  align-items: center;
  gap: 16px;
  transition: all 0.3s ease;
}

.stat-card:hover {
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
}

.stat-icon-wrapper {
  width: 48px;
  height: 48px;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
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
  font-size: 24px;
  font-weight: 700;
  color: var(--text-primary);
  line-height: 1.2;
}

.stat-label {
  font-size: 13px;
  color: var(--text-muted);
  margin-top: 4px;
}

/* 进程过滤器 */
.process-filter {
  padding: 16px 24px;
  background: var(--bg-secondary);
  border-bottom: 1px solid var(--border-primary);
}

.filter-header {
  margin-bottom: 16px;
}

.filter-title-section {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 8px;
}

.filter-title {
  font-size: 16px;
  font-weight: 600;
  color: var(--text-primary);
  margin: 0;
}

.filter-actions {
  display: flex;
  gap: 8px;
}

.filter-stats {
  display: flex;
  align-items: center;
}

.stats-text {
  font-size: 13px;
  color: var(--text-muted);
}

.process-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
  gap: 12px;
}

.process-card {
  background: var(--bg-card);
  border: 1px solid var(--border-primary);
  border-radius: 8px;
  padding: 12px;
  cursor: pointer;
  transition: all 0.2s ease;
  position: relative;
  overflow: hidden;
}

.process-card:hover {
  background: var(--bg-hover);
  border-color: var(--border-hover);
  transform: translateY(-1px);
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.process-card.is-selected {
  background: rgba(59, 130, 246, 0.05);
  border-color: #3b82f6;
  box-shadow: 0 0 0 1px rgba(59, 130, 246, 0.1);
}

.process-card.is-exited {
  opacity: 0.6;
}

.process-card-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 8px;
}

.process-card-icon {
  display: flex;
  align-items: center;
  justify-content: center;
  color: #6b7280;
}

.process-card.is-selected .process-card-icon {
  color: #3b82f6;
}

.process-card-status {
  display: flex;
  align-items: center;
}

.process-selected-indicator {
  position: absolute;
  top: 8px;
  right: 8px;
  color: #3b82f6;
  /* 隐藏选中指示器 */
  display: none;
}

.process-card-content {
  flex: 1;
  min-width: 0;
}

.process-card-name {
  font-size: 14px;
  font-weight: 500;
  color: var(--text-primary);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  margin-bottom: 4px;
}

.process-card-basic-info {
  display: flex;
  align-items: center;
  gap: 12px;
  font-size: 12px;
  color: var(--text-muted);
  margin-bottom: 8px;
}

.basic-info-item {
  display: flex;
  align-items: center;
  gap: 4px;
  white-space: nowrap;
}

.basic-info-item.connections {
  color: #3b82f6;
}

.process-card-details {
  padding-top: 8px;
  border-top: 1px solid var(--border-secondary);
  font-size: 12px;
}

.detail-row {
  display: flex;
  align-items: center;
  gap: 16px;
  margin-bottom: 4px;
}

.detail-row:last-child {
  margin-bottom: 0;
}

.detail-item {
  display: flex;
  align-items: center;
  gap: 4px;
  color: var(--text-muted);
  white-space: nowrap;
}

.detail-row.traffic .detail-item.upload {
  color: #ef4444;
}

.detail-row.traffic .detail-item.download {
  color: #22c55e;
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
}

.status-indicator.exited {
  background: #d1d5db;
}

/* 连接列表容器 */
.connections-container {
  flex: 1;
  display: flex;
  flex-direction: column;
  min-height: 0;
  padding: 16px 24px;
}

/* 表格样式连接列表 */
.connections-table {
  flex: 1;
  display: flex;
  flex-direction: column;
  min-height: 0;
}

.table-header {
  display: grid;
  grid-template-columns: 80px 100px 1fr 120px 100px;
  gap: 12px;
  padding: 8px 12px;
  background: var(--bg-card);
  border: 1px solid var(--border-primary);
  border-bottom: none;
  border-radius: 8px 8px 0 0;
  font-size: 12px;
  font-weight: 600;
  color: var(--text-secondary);
}

.header-cell {
  display: flex;
  align-items: center;
  white-space: nowrap;
}

.connections-scroller {
  flex: 1;
  border: 1px solid var(--border-primary);
  border-radius: 0 0 8px 8px;
  background: var(--bg-card);
}

.table-row {
  display: grid;
  grid-template-columns: 80px 100px 1fr 120px 100px;
  gap: 12px;
  padding: 8px 12px;
  border-bottom: 1px solid var(--border-secondary);
  transition: all 0.2s ease;
  align-items: center;
}

.table-row:hover {
  background: var(--bg-hover);
}

.table-row.is-active {
  background: rgba(16, 185, 129, 0.03);
}

.table-row.is-selected-process {
  background: rgba(59, 130, 246, 0.06);
  border-left: 3px solid #3b82f6;
}

.table-cell {
  display: flex;
  align-items: center;
  min-width: 0;
  font-size: 12px;
}

.protocol-cell {
  display: flex;
  align-items: center;
  justify-content: center;
}

.status-cell {
  display: flex;
  align-items: center;
  gap: 6px;
}

.protocol-cell .n-tag,
.status-cell .n-tag {
  font-weight: 500;
  border: 1px solid transparent;
  transition: all 0.2s ease;
  cursor: default;
}

.protocol-cell .n-tag:hover,
.status-cell .n-tag:hover {
  transform: translateY(-1px);
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

/* 协议和状态标签自定义样式 */
.protocol-cell .n-tag--info,
.status-cell .n-tag--info {
  background: rgba(59, 130, 246, 0.1);
  color: #2563eb;
  border-color: rgba(59, 130, 246, 0.2);
}

.protocol-cell .n-tag--warning,
.status-cell .n-tag--warning {
  background: rgba(245, 158, 11, 0.1);
  color: #d97706;
  border-color: rgba(245, 158, 11, 0.2);
}

.protocol-cell .n-tag--error,
.status-cell .n-tag--error {
  background: rgba(239, 68, 68, 0.1);
  color: #dc2626;
  border-color: rgba(239, 68, 68, 0.2);
}

.protocol-cell .n-tag--success,
.status-cell .n-tag--success {
  background: rgba(34, 197, 94, 0.1);
  color: #16a34a;
  border-color: rgba(34, 197, 94, 0.2);
}

.protocol-cell .n-tag--primary,
.status-cell .n-tag--primary {
  background: rgba(168, 85, 247, 0.1);
  color: #9333ea;
  border-color: rgba(168, 85, 247, 0.2);
}

.protocol-cell .n-tag--default,
.status-cell .n-tag--default {
  background: rgba(107, 114, 128, 0.1);
  color: #6b7280;
  border-color: rgba(107, 114, 128, 0.2);
}

.endpoints-cell {
  min-width: 0;
}

.endpoint-row {
  display: flex;
  align-items: center;
  gap: 8px;
  min-width: 0;
}

.endpoint {
  display: flex;
  align-items: center;
  gap: 4px;
  min-width: 0;
}

.endpoint.local {
  color: var(--text-success);
}

.endpoint.remote {
  color: var(--text-primary);
}

.endpoint-address {
  font-family: 'SF Mono', 'Monaco', 'Inconsolata', monospace;
  font-weight: 500;
  font-size: 11px;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  min-width: 0;
}

.connection-arrow {
  color: var(--text-quaternary);
  flex-shrink: 0;
}

.traffic-cell {
  flex-direction: column;
  align-items: flex-start;
  gap: 2px;
}

.traffic-row {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.traffic-item {
  display: flex;
  align-items: center;
  gap: 4px;
  font-weight: 500;
  font-size: 11px;
}

.traffic-item.upload {
  color: #ef4444;
}

.traffic-item.download {
  color: #22c55e;
}

.time-cell {
  flex-direction: column;
  align-items: flex-start;
  gap: 2px;
}

.time-info {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.duration {
  font-weight: 500;
  color: var(--text-primary);
  font-size: 11px;
}

.last-active {
  color: var(--text-muted);
  font-size: 10px;
}

.connections-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 16px;
}

.connections-title {
  font-size: 16px;
  font-weight: 600;
  color: var(--text-primary);
  margin: 0;
  display: flex;
  align-items: center;
  gap: 8px;
}

.connections-count {
  font-size: 14px;
  font-weight: 400;
  color: var(--text-muted);
}

.connections-actions {
  display: flex;
  gap: 12px;
}


/* 空状态 */
.empty-state {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
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

.empty-actions {
  margin-top: 16px;
}

/* CSS 变量定义 */
:root {
  --bg-primary: #ffffff;
  --bg-secondary: #f9fafb;
  --bg-card: #ffffff;
  --bg-hover: #f3f4f6;
  --bg-glass: rgba(255, 255, 255, 0.8);
  --backdrop-blur: blur(8px);

  --border-primary: #e5e7eb;
  --border-secondary: #f3f4f6;
  --border-hover: #d1d5db;

  --text-primary: #111827;
  --text-secondary: #4b5563;
  --text-muted: #6b7280;
  --text-quaternary: #9ca3af;
  --text-success: #059669;
}

/* 暗色模式 */
@media (prefers-color-scheme: dark) {
  :root {
    --bg-primary: #1f2937;
    --bg-secondary: #111827;
    --bg-card: #1f2937;
    --bg-hover: #374151;
    --bg-glass: rgba(31, 41, 55, 0.8);

    --border-primary: #374151;
    --border-secondary: #1f2937;
    --border-hover: #4b5563;

    --text-primary: #f9fafb;
    --text-secondary: #d1d5db;
    --text-muted: #9ca3af;
    --text-quaternary: #6b7280;
    --text-success: #10b981;
  }

  .stat-card {
    box-shadow: 0 1px 3px rgba(0, 0, 0, 0.3);
  }

  .stat-card:hover {
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.4);
  }
}

/* 自定义 Naive UI 样式 */
:deep(.n-input .n-input__input-el) {
  background: var(--bg-card);
  border-color: var(--border-primary);
  color: var(--text-primary);
}

:deep(.n-input:hover .n-input__input-el) {
  border-color: var(--border-hover);
}

:deep(.n-input.n-input--focus .n-input__input-el) {
  border-color: #3b82f6;
}

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
  background: var(--border-secondary);
}

:deep(.n-virtual-list__content)::-webkit-scrollbar-thumb {
  background: var(--border-primary);
  border-radius: 4px;
}

:deep(.n-virtual-list__content)::-webkit-scrollbar-thumb:hover {
  background: var(--border-hover);
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

  .stats-grid {
    grid-template-columns: repeat(2, 1fr);
  }
}

@media (max-width: 900px) {
  .main-header {
    padding: 0 16px;
  }

  .stats-section,
  .process-filter,
  .connections-container {
    padding: 12px 16px;
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

  .process-grid {
    grid-template-columns: repeat(auto-fill, minmax(240px, 1fr));
    gap: 8px;
  }

  .process-card {
    padding: 10px;
  }

  .table-header {
    grid-template-columns: 70px 90px 1fr 100px 80px;
    gap: 8px;
    font-size: 11px;
  }

  .table-row {
    grid-template-columns: 70px 90px 1fr 100px 80px;
    gap: 8px;
  }

  .protocol-cell .n-tag,
  .status-cell .n-tag {
    font-size: 10px;
    padding: 1px 6px;
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

  .stats-grid {
    grid-template-columns: 1fr;
    gap: 8px;
  }

  .stat-card {
    padding: 12px;
  }

  .stat-icon-wrapper {
    width: 40px;
    height: 40px;
  }

  .stat-number {
    font-size: 20px;
  }

  .process-grid {
    grid-template-columns: 1fr;
    gap: 8px;
  }

}

@media (max-width: 480px) {
  .filter-buttons {
    display: none;
  }

  .search-area {
    min-width: 100px;
  }

  .filter-title-section {
    flex-direction: column;
    align-items: flex-start;
    gap: 8px;
  }

  .filter-actions {
    flex-direction: column;
    gap: 4px;
  }
}
</style>
