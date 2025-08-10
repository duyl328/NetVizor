<template>
  <n-modal v-model:show="showModal" class="software-detail-modal">
    <div class="modal-card">
      <!-- 弹窗头部 -->
      <div class="modal-header">
        <div class="header-left">
          <img
            v-if="analysisData?.appInfo.icon"
            :src="'data:image/png;base64,' + analysisData.appInfo.icon"
            class="app-icon"
            @error="handleIconError"
          />
          <n-icon v-else :component="DesktopOutline" class="app-icon-fallback" />

          <div class="header-info">
            <h3 class="app-name">{{ analysisData?.appInfo.name || 'Unknown Application' }}</h3>
            <p class="app-details">
              {{ analysisData?.appInfo.company || 'Unknown Company' }}
              <span v-if="analysisData?.appInfo.version">• v{{ analysisData.appInfo.version }}</span>
            </p>
          </div>
        </div>

        <div class="header-actions">
          <n-button quaternary circle @click="refreshData">
            <template #icon>
              <n-icon :component="RefreshOutline" />
            </template>
          </n-button>
          <n-button quaternary circle @click="closeModal">
            <template #icon>
              <n-icon :component="CloseOutline" />
            </template>
          </n-button>
        </div>
      </div>

      <!-- 统计概览卡片 -->
      <div class="stats-overview">
        <div class="stat-card">
          <div class="stat-value">{{ formatBytes(analysisData?.summary.totalTraffic || 0) }}</div>
          <div class="stat-label">总流量</div>
          <div class="stat-trend">
            <span class="upload">↑ {{ formatBytes(analysisData?.summary.totalUpload || 0) }}</span>
            <span class="download">↓ {{ formatBytes(analysisData?.summary.totalDownload || 0) }}</span>
          </div>
        </div>

        <div class="stat-card">
          <div class="stat-value">{{ analysisData?.summary.totalConnections || 0 }}</div>
          <div class="stat-label">总连接数</div>
          <div class="stat-extra">{{ analysisData?.summary.uniqueRemoteIPs || 0 }} 个远程IP</div>
        </div>

        <div class="stat-card">
          <div class="stat-value">{{ analysisData?.summary.uniqueRemotePorts || 0 }}</div>
          <div class="stat-label">使用端口</div>
          <div class="stat-extra">{{ timeRangeText }}</div>
        </div>

        <div class="stat-card">
          <div class="stat-value">{{ analysisData?.portAnalysis.length || 0 }}</div>
          <div class="stat-label">活跃端口</div>
          <div class="stat-extra">多协议通信</div>
        </div>
      </div>

      <div class="modal-body">
        <!-- 主要内容区域 -->
        <div class="content-grid">
          <!-- 网络关系图 - 主图表 -->
          <div class="chart-section main-chart">
            <div class="section-header">
              <h4 class="section-title">
                <n-icon :component="GitNetworkOutline" class="section-icon" />
                网络连接拓扑
              </h4>
              <div class="section-extra">
                <span class="connection-count">{{ analysisData?.topConnections.length || 0 }} 个连接</span>
              </div>
            </div>
            <div class="chart-container">
              <NetworkAnalysisChart
                v-if="analysisData"
                :data="analysisData"
                :loading="loading"
              />
            </div>
          </div>

          <!-- 协议分布图 -->
          <div class="chart-section protocol-chart">
            <div class="section-header">
              <h4 class="section-title">
                <n-icon :component="StatsChartOutline" class="section-icon" />
                协议分布
              </h4>
            </div>
            <div class="chart-container">
              <ProtocolChart v-if="protocolChartData.length > 0" :data="protocolChartData" />
              <div v-else class="empty-chart">
                <n-icon :component="PieChartOutline" size="32" />
                <span>暂无协议数据</span>
              </div>
            </div>
          </div>

          <!-- 时间趋势图 -->
          <div class="chart-section trend-chart">
            <div class="section-header">
              <h4 class="section-title">
                <n-icon :component="TrendingUpOutline" class="section-icon" />
                流量趋势
              </h4>
            </div>
            <div class="chart-container">
              <TimeTrendChart v-if="timeTrendData.length > 0" :data="timeTrendData" />
              <div v-else class="empty-chart">
                <n-icon :component="BarChartOutline" size="32" />
                <span>暂无趋势数据</span>
              </div>
            </div>
          </div>
        </div>

        <!-- 详细数据表格 -->
        <div class="data-tables">
          <div class="table-tabs">
            <n-tabs v-model:value="activeTab" type="line" animated>
              <n-tab-pane name="connections" tab="连接详情">
                <ConnectionsTable :data="analysisData?.topConnections || []" />
              </n-tab-pane>
              <n-tab-pane name="ports" tab="端口统计">
                <PortStatsTable :data="portStatsData" />
              </n-tab-pane>
              <n-tab-pane name="timeline" tab="时间线">
                <TimelineView :data="analysisData?.timeTrends || []" />
              </n-tab-pane>
            </n-tabs>
          </div>
        </div>
      </div>
    </div>
  </n-modal>
</template>

<script setup lang="ts">
import { ref, computed, watch, nextTick } from 'vue'
import { NModal, NButton, NIcon, NTabs, NTabPane } from 'naive-ui'
import {
  DesktopOutline,
  RefreshOutline,
  CloseOutline,
  GitNetworkOutline,
  StatsChartOutline,
  TrendingUpOutline,
  PieChartOutline,
  BarChartOutline
} from '@vicons/ionicons5'
import { httpClient } from '@/utils/http'
import type { ApiResponse } from '@/types/http'

// 导入子组件
import NetworkAnalysisChart from './NetworkAnalysisChart.vue'
import ProtocolChart from './ProtocolChart.vue'
import TimeTrendChart from './TimeTrendChart.vue'
import ConnectionsTable from './ConnectionsTable.vue'
import PortStatsTable from './PortStatsTable.vue'
import TimelineView from './TimelineView.vue'

// 接口定义
interface TopConnection {
  localIP: string
  localPort: number
  remoteIP: string
  remotePort: number
  protocol: 'TCP' | 'UDP'
  totalUpload: number
  totalDownload: number
  totalTraffic: number
  connectionCount: number
  firstSeen: string
  lastSeen: string
}

interface NetworkAnalysisData {
  appInfo: {
    appId: string
    name: string
    company?: string
    version?: string
    path?: string
    icon?: string
    hash?: string
  }
  summary: {
    timeRange: string
    startTime: string
    endTime: string
    totalUpload: number
    totalDownload: number
    totalTraffic: number
    totalConnections: number
    uniqueRemoteIPs: number
    uniqueRemotePorts: number
  }
  topConnections: TopConnection[]
  protocolStats: Array<{
    protocol: string
    connectionCount: number
    totalTraffic: number
    percentage: number
  }>
  timeTrends: Array<{
    timestamp: number
    timeStr: string
    upload: number
    download: number
    connections: number
  }>
  portAnalysis: Array<{
    port: number
    serviceName: string
    connectionCount: number
    totalTraffic: number
    protocols: string[]
  }>
}

// Props和Emits
interface Props {
  show: boolean
  appId: string | null
  timeRange: string
}

const props = defineProps<Props>()
const emit = defineEmits<{
  'update:show': [value: boolean]
}>()

// 响应式数据
const showModal = computed({
  get: () => props.show,
  set: (value) => emit('update:show', value)
})

const analysisData = ref<NetworkAnalysisData | null>(null)
const loading = ref(false)
const activeTab = ref('connections')

// 计算属性
const timeRangeText = computed(() => {
  const rangeMap: Record<string, string> = {
    '1hour': '1小时',
    '1day': '24小时',
    '7days': '7天',
    '30days': '30天'
  }
  return rangeMap[props.timeRange] || props.timeRange
})

const protocolChartData = computed(() => {
  if (!analysisData.value?.protocolStats) return []

  const colors = ['#3b82f6', '#10b981', '#f59e0b', '#ef4444', '#8b5cf6']
  return analysisData.value.protocolStats.map((stat, index) => ({
    protocol: stat.protocol,
    bytes: stat.totalTraffic,
    percentage: stat.percentage,
    color: colors[index % colors.length]
  }))
})

const timeTrendData = computed(() => {
  if (!analysisData.value?.timeTrends) return []

  return analysisData.value.timeTrends.map(trend => ({
    timestamp: trend.timestamp * 1000, // 转换为毫秒
    timeStr: trend.timeStr,
    upload: trend.upload,
    download: trend.download,
    connections: trend.connections
  }))
})

const portStatsData = computed(() => {
  if (!analysisData.value?.portAnalysis || !analysisData.value?.topConnections) return []

  return analysisData.value.portAnalysis.map(port => {
    // 从连接数据中获取使用该端口的远程主机
    const connectionsForPort = analysisData.value!.topConnections.filter(
      conn => conn.remotePort === port.port
    )

    return {
      port: port.port,
      protocol: port.protocols[0] || 'TCP', // 取第一个协议
      connectionCount: port.connectionCount,
      totalBytes: port.totalTraffic,
      remoteHosts: [...new Set(connectionsForPort.map(conn => conn.remoteIP))]
    }
  })
})

// API调用
const fetchAnalysisData = async () => {
  if (!props.appId) return

  loading.value = true
  try {
    const params = {
      appId: props.appId,
      timeRange: props.timeRange
    }

    const response: ApiResponse<NetworkAnalysisData> = await httpClient.get(
      '/apps/network-analysis',
      params
    )

    if (response.success && response.data) {
      analysisData.value = response.data
    } else {
      console.error('获取网络分析数据失败:', response.message)
      analysisData.value = null
    }
  } catch (error) {
    console.error('获取网络分析数据异常:', error)
    analysisData.value = null
  } finally {
    loading.value = false
  }
}

// 事件处理
const refreshData = () => {
  fetchAnalysisData()
}

const closeModal = () => {
  showModal.value = false
}

const handleIconError = (event: Event) => {
  const target = event.target as HTMLImageElement
  target.style.display = 'none'
}

// 工具函数
const formatBytes = (bytes: number): string => {
  if (bytes === 0) return '0 B'

  const k = 1024
  const sizes = ['B', 'KB', 'MB', 'GB', 'TB']
  const i = Math.floor(Math.log(bytes) / Math.log(k))

  return `${parseFloat((bytes / Math.pow(k, i)).toFixed(1))} ${sizes[i]}`
}

// 监听器
watch(() => props.show, (newShow) => {
  if (newShow && props.appId) {
    nextTick(() => {
      fetchAnalysisData()
    })
  }
})

watch(() => [props.appId, props.timeRange], () => {
  if (props.show && props.appId) {
    fetchAnalysisData()
  }
})
</script>

<style scoped>
/* 弹窗基础样式 */
.software-detail-modal :deep(.n-modal) {
  max-width: 90vw;
  max-height: 90vh;
}

.modal-card {
  width: 1200px;
  max-width: 90vw;
  max-height: 90vh;
  background: var(--bg-card);
  border-radius: 16px;
  overflow: hidden;
  display: flex;
  flex-direction: column;
  box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.1), 0 10px 10px -5px rgba(0, 0, 0, 0.04);
}

/* 弹窗头部 */
.modal-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 20px 24px;
  border-bottom: 1px solid var(--border-primary);
  background: linear-gradient(135deg, var(--bg-card) 0%, var(--bg-secondary) 100%);
}

.header-left {
  display: flex;
  align-items: center;
  gap: 16px;
}

.app-icon {
  width: 48px;
  height: 48px;
  border-radius: 12px;
  box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1);
}

.app-icon-fallback {
  width: 48px;
  height: 48px;
  color: var(--text-muted);
  background: var(--bg-tertiary);
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 24px;
}

.header-info h3 {
  margin: 0;
  font-size: 20px;
  font-weight: 700;
  color: var(--text-primary);
}

.header-info p {
  margin: 4px 0 0 0;
  font-size: 14px;
  color: var(--text-secondary);
}

.header-actions {
  display: flex;
  gap: 8px;
}

/* 统计概览 */
.stats-overview {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 1px;
  background: var(--border-primary);
}

.stat-card {
  background: var(--bg-card);
  padding: 20px;
  text-align: center;
  position: relative;
  overflow: hidden;
}

.stat-card::before {
  content: '';
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  height: 3px;
  background: linear-gradient(90deg, var(--accent-primary), var(--accent-secondary));
}

.stat-value {
  font-size: 24px;
  font-weight: 700;
  color: var(--text-primary);
  margin-bottom: 6px;
}

.stat-label {
  font-size: 12px;
  color: var(--text-muted);
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  margin-bottom: 8px;
}

.stat-trend {
  display: flex;
  justify-content: center;
  gap: 12px;
  font-size: 11px;
  font-weight: 500;
}

.stat-trend .upload {
  color: var(--accent-error);
}

.stat-trend .download {
  color: var(--accent-success);
}

.stat-extra {
  font-size: 11px;
  color: var(--text-tertiary);
}

/* 弹窗主体 */
.modal-body {
  flex: 1;
  overflow: hidden;
  display: flex;
  flex-direction: column;
}

/* 内容网格 */
.content-grid {
  display: grid;
  grid-template-columns: 2fr 1fr;
  grid-template-rows: 1fr 1fr;
  gap: 1px;
  height: 400px;
  background: var(--border-primary);
  margin-bottom: 1px;
}

.chart-section {
  background: var(--bg-card);
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.main-chart {
  grid-row: span 2;
}

.section-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 16px 20px;
  border-bottom: 1px solid var(--border-secondary);
  flex-shrink: 0;
}

.section-title {
  font-size: 14px;
  font-weight: 600;
  color: var(--text-primary);
  margin: 0;
  display: flex;
  align-items: center;
  gap: 8px;
}

.section-icon {
  color: var(--accent-primary);
}

.section-extra {
  font-size: 12px;
  color: var(--text-muted);
}

.connection-count {
  background: var(--accent-primary);
  color: white;
  padding: 2px 8px;
  border-radius: 12px;
  font-weight: 600;
}

.chart-container {
  flex: 1;
  padding: 16px;
  overflow: hidden;
}

.empty-chart {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  height: 100%;
  color: var(--text-muted);
  gap: 12px;
}

.empty-chart span {
  font-size: 13px;
}

/* 数据表格 */
.data-tables {
  flex: 1;
  background: var(--bg-card);
  overflow: hidden;
}

.table-tabs {
  height: 100%;
  display: flex;
  flex-direction: column;
}

.table-tabs :deep(.n-tabs) {
  height: 100%;
  display: flex;
  flex-direction: column;
}

.table-tabs :deep(.n-tabs-nav) {
  padding: 0 20px;
  border-bottom: 1px solid var(--border-secondary);
}

.table-tabs :deep(.n-tabs-content) {
  flex: 1;
  overflow: hidden;
}

.table-tabs :deep(.n-tab-pane) {
  height: 100%;
  padding: 0;
}

/* 响应式设计 */
@media (max-width: 1024px) {
  .modal-card {
    width: 95vw;
    max-height: 95vh;
  }

  .stats-overview {
    grid-template-columns: repeat(2, 1fr);
  }

  .content-grid {
    grid-template-columns: 1fr;
    grid-template-rows: 200px 150px 150px;
    height: auto;
  }

  .main-chart {
    grid-row: span 1;
  }
}

@media (max-width: 768px) {
  .modal-card {
    width: 100vw;
    max-height: 100vh;
    border-radius: 0;
  }

  .modal-header {
    padding: 16px 20px;
  }

  .header-info h3 {
    font-size: 18px;
  }

  .app-icon, .app-icon-fallback {
    width: 40px;
    height: 40px;
  }

  .stats-overview {
    grid-template-columns: 1fr;
  }

  .stat-card {
    padding: 16px;
  }

  .content-grid {
    height: auto;
    grid-template-rows: 180px 120px 120px;
  }
}

/* 暗色主题适配 */
@media (prefers-color-scheme: dark) {
  .modal-card {
    box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.4), 0 10px 10px -5px rgba(0, 0, 0, 0.2);
  }
}
</style>
