<template>
  <div class="analyse-view">
    <div class="analyse-container">
      <!-- 顶部时间选择栏 -->
      <div class="analyse-header">
        <div class="header-info">
          <h2 class="view-title">流量分析</h2>
          <p class="view-subtitle">深入分析网络流量模式和应用程序使用情况</p>
        </div>
        <div class="header-controls">
          <div class="time-selector">
            <n-button-group>
              <n-button
                v-for="range in timeRanges"
                :key="range.type"
                :type="selectedTimeRange === range.type ? 'primary' : 'default'"
                :disabled="!range.available"
                @click="selectedTimeRange = range.type"
              >
                {{ range.name }}
              </n-button>
            </n-button-group>
          </div>
          <n-button type="primary" size="medium" @click="refreshData">
            <template #icon>
              <n-icon :component="RefreshOutline" />
            </template>
            刷新数据
          </n-button>
        </div>
      </div>

      <!-- 关键指标卡片 -->
      <div class="metrics-grid" v-if="false ">
        <div class="metric-card">
          <div class="metric-header">
            <span class="metric-title">总流量</span>
            <n-icon :component="TrendingUpOutline" class="metric-icon up" />
          </div>
          <div class="metric-value">2.45 TB</div>
          <div class="metric-trend">
            <span class="trend up">+12.5%</span>
            <span class="trend-text">较上周</span>
          </div>
        </div>

        <div class="metric-card">
          <div class="metric-header">
            <span class="metric-title">威胁检测</span>
            <n-icon :component="WarningOutline" class="metric-icon warning" />
          </div>
          <div class="metric-value">156</div>
          <div class="metric-trend">
            <span class="trend down">-8.2%</span>
            <span class="trend-text">较上周</span>
          </div>
        </div>

        <div class="metric-card">
          <div class="metric-header">
            <span class="metric-title">平均延迟</span>
            <n-icon :component="SpeedometerOutline" class="metric-icon" />
          </div>
          <div class="metric-value">12.3 ms</div>
          <div class="metric-trend">
            <span class="trend stable">0.0%</span>
            <span class="trend-text">稳定</span>
          </div>
        </div>

        <div class="metric-card">
          <div class="metric-header">
            <span class="metric-title">数据包丢失</span>
            <n-icon :component="AlertCircleOutline" class="metric-icon error" />
          </div>
          <div class="metric-value">0.02%</div>
          <div class="metric-trend">
            <span class="trend up">+0.01%</span>
            <span class="trend-text">轻微上升</span>
          </div>
        </div>
      </div>

      <!-- 图表区域 -->
      <div class="charts-grid">
        <!-- 流量趋势图 -->
        <div class="chart-card large">
          <div class="chart-header">
            <h3 class="chart-title">流量趋势</h3>
            <div class="chart-controls">
              <n-select
                v-model:value="selectedInterface"
                :options="interfaceOptions"
                size="small"
                :style="{ minWidth: interfaceSelectWidth }"
              />
            </div>
          </div>
          <div class="chart-body">
            <TrafficTrendChart
              :data="trafficTrendData"
              :interface-id="selectedInterface"
            />
          </div>
        </div>


        <!-- Top应用流量图表 -->
        <div class="chart-card">
          <div class="chart-header">
            <h3 class="chart-title">Top应用流量</h3>
          </div>
          <div class="chart-body">
            <TopAppsChart
              :data="topAppsData"
              :time-range="selectedTimeRange"
            />
          </div>
        </div>
      </div>

      <!-- 软件流量分析区域 -->
      <div class="software-analysis-area">
        <div class="software-ranking-panel" :class="{ 'large-screen': isLargeScreen }">
          <div class="panel-header">
            <h3 class="panel-title">软件流量TOP榜</h3>
          </div>
          <div class="ranking-content">
            <SoftwareRankingList
              :data="softwareRankingData"
              :time-range="selectedTimeRange"
              @select-software="showSoftwareDetail"
            />
          </div>
        </div>
      </div>

      <!-- 软件详情弹窗 -->
      <SoftwareDetailModal
        v-model:show="showDetailModal"
        :software="selectedSoftware"
        :software-info="selectedSoftwareInfo"
        :network-relation-data="networkRelationData"
        :port-stats-data="portStatsData"
        :protocol-data="protocolData"
        :time-range="selectedTimeRange"
      />
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { NButton, NButtonGroup, NIcon, NSelect } from 'naive-ui'
import {
  RefreshOutline,
  TrendingUpOutline,
  WarningOutline,
  SpeedometerOutline,
  AlertCircleOutline,
  ServerOutline,
} from '@vicons/ionicons5'
import { httpClient } from '@/utils/http'
import type { ApiResponse } from '@/types/http'

// 导入组件
import TrafficTrendChart from './components/TrafficTrendChart.vue'
import TopAppsChart from './components/TopAppsChart.vue'
import SoftwareRankingList from './components/SoftwareRankingList.vue'
import SoftwareDetailModal from './components/SoftwareDetailModal.vue'

// API 接口类型定义
interface TimeRange {
  type: string
  name: string
  available: boolean
  startTime?: string
}

interface NetworkInterface {
  id: string
  name: string
  displayName: string
  isActive: boolean
  macAddress: string
}

interface TrafficTrend {
  interface: string
  timeRange: string
  points: {
    timestamp: string
    uploadSpeed: number
    downloadSpeed: number
  }[]
}

interface TopApp {
  processName: string
  displayName: string
  icon: string
  totalBytes: number
}

interface TopTrafficApp {
  rank: number
  processName: string
  displayName: string
  processPath: string
  icon: string
  version: string
  company: string
  totalBytes: number
  uploadBytes: number
  connectionCount: number
}

// 时间范围选项
const timeRanges = ref<TimeRange[]>([
  { type: '1hour', name: '1小时', available: true },
  { type: '1day', name: '24小时', available: true },
  { type: '7days', name: '7天', available: true },
  { type: '30days', name: '30天', available: false },
])

// API 调用函数
const getAvailableRanges = async () => {
  try {
    const res: ApiResponse<TimeRange[]> = await httpClient.get('/statistics/available-ranges')
    if (res.success && res.data) {
      // 根据 API 返回的数据更新时间范围选项
      const ranges = Array.isArray(res.data) ? res.data : [res.data]
      const rangeMap: Record<string, string> = {
        'hour': '1hour',
        'day': '1day',
        'week': '7days',
        'month': '30days'
      }

      // 更新时间范围的可用状态
      timeRanges.value.forEach(range => {
        const matchedRange = ranges.find(r => rangeMap[r.type] === range.type)
        if (matchedRange) {
          range.available = matchedRange.available
        } else {
          range.available = false
        }
      })
    }
  } catch (error) {
    console.error('获取可用时间范围失败:', error)
  }
}

const selectedTimeRange = ref('1hour') // 默认选择1小时，暂不支持10分钟选项

// 网络接口选项
const interfaceOptions = ref([
  { label: '全部网卡', value: 'all' },
])

// 计算网卡选择框的最小宽度
const interfaceSelectWidth = computed(() => {
  if (interfaceOptions.value.length === 0) return '120px'
  
  const maxLength = Math.max(
    ...interfaceOptions.value.map(option => option.label.length)
  )
  
  // 根据字符长度计算宽度，中文字符按2倍宽度计算
  const estimatedWidth = maxLength * 8 + 60 // 基础60px + 字符宽度
  return Math.max(estimatedWidth, 120) + 'px' // 最小120px
})

const getNetworkInterfaces = async () => {
  try {
    const params: Record<string, any> = {
      timeRange: selectedTimeRange.value
    }
    
    const res: ApiResponse<NetworkInterface[]> = await httpClient.get('/statistics/interfaces', params)
    if (res.success && res.data) {
      // 清空现有选项（保留全部网卡选项）
      interfaceOptions.value = [{ label: '全部网卡', value: 'all' }]

      // 添加从 API 获取的网络接口
      res.data.forEach(iface => {
        if (iface.isActive) {
          interfaceOptions.value.push({
            label: iface.displayName,
            value: iface.id
          })
        }
      })
    }
  } catch (error) {
    console.error('获取网络接口失败:', error)
  }
}

const selectedInterface = ref('all')

// 大屏幕检测
const isLargeScreen = computed(() => {
  if (typeof window === 'undefined') return false
  return window.innerHeight >= 900 // 高度大于900px认为是大屏幕
})

// 移除单独的软件排行时间范围，使用顶部统一的时间选择

// 选中的软件和弹窗状态
const selectedSoftware = ref<any>(null)
const showDetailModal = ref(false)

// 流量趋势数据
const trafficTrendData = ref<any[]>([])

const getTrafficTrends = async () => {
  try {
    const params: Record<string, any> = {
      timeRange: selectedTimeRange.value,
      interfaceId: selectedInterface.value
    }

    const res: ApiResponse<TrafficTrend> = await httpClient.get('/traffic/trends', params)
    if (res.success && res.data) {
      // 转换 API 数据为图表需要的格式并按时间排序（最新时间在右侧）
      let processedData = res.data.points
        .map(point => ({
          timestamp: parseInt(point.timestamp) * 1000, // 转换为毫秒时间戳
          uploadSpeed: point.uploadSpeed,
          downloadSpeed: point.downloadSpeed
        }))
        .sort((a, b) => a.timestamp - b.timestamp) // 按时间升序排列，最新时间在右侧
      
      // 限制显示的数据点数量，避免图表过于密集
      const maxPoints = 250 // 最多显示250个点
      if (processedData.length > maxPoints) {
        // 等间隔采样数据点
        const step = Math.floor(processedData.length / maxPoints)
        processedData = processedData.filter((_, index) => index % step === 0)
      }
      
      trafficTrendData.value = processedData
    }
  } catch (error) {
    console.error('获取流量趋势失败:', error)
  }
}

// Top 应用流量数据
const topAppsData = ref<any[]>([])

const getTopApps = async () => {
  try {
    const params: Record<string, any> = {
      timeRange: selectedTimeRange.value,
      limit: 10
    }

    const res: ApiResponse<TopApp[]> = await httpClient.get('/traffic/top-apps', params)
    if (res.success && res.data) {
      // 计算总流量用于计算百分比
      const totalBytes = res.data.reduce((sum, app) => sum + app.totalBytes, 0)

      // 转换数据格式
      topAppsData.value = res.data.map(app => ({
        processName: app.processName,
        displayName: app.displayName,
        icon: app.icon,
        totalBytes: app.totalBytes,
        percentage: totalBytes > 0 ? (app.totalBytes / totalBytes * 100) : 0
      }))
    }
  } catch (error) {
    console.error('获取Top应用流量失败:', error)
  }
}

// 软件流量排行数据
const softwareRankingData = ref<any[]>([])

const getSoftwareRanking = async () => {
  try {
    const params: Record<string, any> = {
      timeRange: selectedTimeRange.value,
      page: 1,
      pageSize: 100
    }

    const res: ApiResponse<{
      total: number
      page: number
      pageSize: number
      items: TopTrafficApp[]
    }> = await httpClient.get('/apps/top-traffic', params)

    if (res.success && res.data) {
      // 计算总流量用于计算百分比
      const totalBytes = res.data.items.reduce((sum, app) => sum + app.totalBytes, 0)

      // 转换数据格式适配组件
      softwareRankingData.value = res.data.items.map(app => ({
        rank: app.rank,
        processName: app.processName,
        displayName: app.displayName,
        processPath: app.processPath,
        icon: app.icon,
        version: app.version,
        company: app.company,
        totalBytes: app.totalBytes,
        uploadBytes: app.uploadBytes,
        connectionCount: app.connectionCount,
        percentage: totalBytes > 0 ? (app.totalBytes / totalBytes * 100) : 0
      }))
    }
  } catch (error) {
    console.error('获取软件流量排行失败:', error)
  }
}

// 选中软件的详细信息
const selectedSoftwareInfo = computed(() => {
  if (!selectedSoftware.value) return null
  return {
    processName: 'chrome.exe',
    displayName: 'Google Chrome',
    version: '119.0.6045.160',
    company: 'Google LLC',
    processPath: 'C:\\Program Files\\Google\\Chrome\\chrome.exe',
    fileSize: 2467840,
  }
})

const networkRelationData = computed(() => {
  if (!selectedSoftware.value) return { nodes: [], links: [] }
  return {
    nodes: [
      { id: 'app_chrome', name: 'Chrome', type: 'application', size: 45.2, category: 0 },
      { id: 'port_443', name: '443/TCP', type: 'port', size: 30.5, category: 1 },
      { id: 'host_google', name: 'google.com', type: 'remote_host', size: 25.8, category: 2 },
    ],
    links: [
      { source: 'app_chrome', target: 'port_443', value: 30.5, label: '30.5MB' },
      { source: 'port_443', target: 'host_google', value: 25.8, label: '25.8MB' },
    ]
  }
})

const portStatsData = computed(() => {
  if (!selectedSoftware.value) return []
  return [
    { port: 443, protocol: 'TCP', connectionCount: 15, totalBytes: 52428800, remoteHosts: ['142.251.42.227', '172.217.164.46'] },
    { port: 80, protocol: 'TCP', connectionCount: 5, totalBytes: 10485760, remoteHosts: ['93.184.216.34'] },
  ]
})

// 协议分布数据（移到弹窗中使用）
const protocolData = computed(() => {
  if (!selectedSoftware.value) return []
  return [
    { protocol: 'HTTPS', bytes: 1610612736, percentage: 37.5, color: '#3b82f6' },
    { protocol: 'HTTP', bytes: 1073741824, percentage: 25.0, color: '#10b981' },
    { protocol: 'DNS', bytes: 751619276, percentage: 17.5, color: '#f59e0b' },
    { protocol: '其他', bytes: 858993459, percentage: 20.0, color: '#ef4444' }
  ]
})

// 事件处理
const showSoftwareDetail = (software: any) => {
  selectedSoftware.value = software
  showDetailModal.value = true
}

const refreshData = async () => {
  // 刷新所有数据
  console.log('Refreshing data...')
  await Promise.all([
    getAvailableRanges(),
    getNetworkInterfaces(),
    getTrafficTrends(),
    getTopApps(),
    getSoftwareRanking()
  ])
}

// 监听时间范围变化
watch(selectedTimeRange, () => {
  // 重新加载相关数据
  getTrafficTrends()
  getTopApps()
  getSoftwareRanking()
})

watch(selectedInterface, () => {
  // 重新加载趋势数据
  getTrafficTrends()
})


onMounted(async () => {
  // 页面初始化，加载所有数据
  await refreshData()
})
</script>

<style scoped>
.analyse-view {
  height: 100%;
  overflow-y: auto;
  background: var(--bg-secondary);
}

.analyse-container {
  padding: 24px;
  max-width: 1600px;
  margin: 0 auto;
}

/* 头部 */
.analyse-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 24px;
  padding: 16px 20px;
  background: var(--bg-card);
  border-radius: 12px;
  border: 1px solid var(--border-primary);
}

.header-info {
  flex: 1;
}

.view-title {
  font-size: 24px;
  font-weight: 700;
  color: var(--text-primary);
  margin: 0;
}

.view-subtitle {
  font-size: 14px;
  color: var(--text-muted);
  margin: 4px 0 0 0;
}

.header-controls {
  display: flex;
  gap: 16px;
  align-items: center;
}

.time-selector {
  display: flex;
  align-items: center;
}

/* 指标卡片 */
.metrics-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
  gap: 16px;
  margin-bottom: 24px;
}

.metric-card {
  background: var(--bg-card);
  backdrop-filter: var(--backdrop-blur);
  border: 1px solid var(--border-primary);
  border-radius: 12px;
  padding: 20px;
  transition: var(--transition);
}

.metric-card:hover {
  transform: translateY(-2px);
  box-shadow: var(--shadow-lg);
}

.metric-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 12px;
}

.metric-title {
  font-size: 14px;
  color: var(--text-muted);
  font-weight: 500;
}

.metric-icon {
  font-size: 20px;
  color: var(--text-quaternary);
}

.metric-icon.up {
  color: var(--accent-success);
}

.metric-icon.warning {
  color: var(--accent-warning);
}

.metric-icon.error {
  color: var(--accent-error);
}

.metric-value {
  font-size: 32px;
  font-weight: 700;
  color: var(--text-primary);
  line-height: 1;
  margin-bottom: 8px;
}

.metric-trend {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 13px;
}

.trend {
  font-weight: 600;
}

.trend.up {
  color: var(--accent-success);
}

.trend.down {
  color: var(--accent-error);
}

.trend.stable {
  color: var(--text-muted);
}

.trend-text {
  color: var(--text-muted);
}

/* 图表网格 */
.charts-grid {
  display: grid;
  grid-template-columns: 2fr 1fr;
  gap: 16px;
  margin-bottom: 24px;
}

.chart-card {
  background: var(--bg-card);
  backdrop-filter: var(--backdrop-blur);
  border: 1px solid var(--border-primary);
  border-radius: 12px;
  padding: 20px;
}

.chart-card.large {
  grid-column: span 1;
}

.chart-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;
}

.chart-title {
  font-size: 16px;
  font-weight: 600;
  color: var(--text-primary);
  margin: 0;
}

.chart-body {
  height: 240px;
  display: flex;
  align-items: center;
  justify-content: center;
}

/* 流量趋势图 */
.chart-placeholder {
  width: 100%;
  height: 100%;
  position: relative;
}

.chart-svg {
  width: 100%;
  height: 100%;
}

/* 饼图 */
.pie-chart {
  width: 100%;
  height: 100%;
  display: flex;
  align-items: center;
  gap: 24px;
}

.pie-center {
  width: 120px;
  height: 120px;
}

.pie-svg {
  width: 100%;
  height: 100%;
}

.pie-legend {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.legend-item {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 13px;
  color: var(--text-secondary);
}

.legend-dot {
  width: 12px;
  height: 12px;
  border-radius: 50%;
}

/* 应用列表 */
.app-list {
  width: 100%;
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.app-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.app-info {
  display: flex;
  align-items: center;
  gap: 12px;
  flex: 1;
}

.app-rank {
  width: 20px;
  height: 20px;
  background: var(--bg-tertiary);
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 11px;
  font-weight: 600;
  color: var(--text-muted);
}

.app-name {
  font-size: 13px;
  color: var(--text-secondary);
  font-weight: 500;
}

.app-traffic {
  display: flex;
  align-items: center;
  gap: 12px;
  flex: 1;
}

.traffic-bar {
  flex: 1;
  height: 6px;
  background: var(--bg-tertiary);
  border-radius: 3px;
  overflow: hidden;
}

.traffic-fill {
  height: 100%;
  background: linear-gradient(90deg, var(--accent-primary), var(--accent-secondary));
  border-radius: 3px;
  transition: width 0.5s ease;
}

.traffic-text {
  font-size: 12px;
  color: var(--text-muted);
  min-width: 60px;
  text-align: right;
}

/* 软件分析区域 */
.software-analysis-area {
  margin-top: 24px;
}

.software-ranking-panel {
  background: var(--bg-card);
  backdrop-filter: var(--backdrop-blur);
  border: 1px solid var(--border-primary);
  border-radius: 12px;
  overflow: hidden;
  height: 500px;
  display: flex;
  flex-direction: column;
}

.software-ranking-panel.large-screen {
  height: calc(100vh - 400px); /* 大屏幕下使用更多高度 */
  min-height: 600px;
}

.panel-header {
  padding: 16px 20px;
  border-bottom: 1px solid var(--border-primary);
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-shrink: 0;
}

.panel-title {
  font-size: 16px;
  font-weight: 600;
  color: var(--text-primary);
  margin: 0;
}

.panel-controls {
  display: flex;
  gap: 8px;
}

.ranking-content {
  flex: 1;
  overflow: hidden;
}


/* 响应式 */
@media (max-width: 1200px) {
  .charts-grid {
    grid-template-columns: 1fr;
  }

  .chart-card.large {
    grid-column: span 1;
  }

  .software-ranking-panel {
    height: 450px;
  }
}

@media (max-width: 768px) {
  .analyse-header {
    flex-direction: column;
    align-items: flex-start;
    gap: 16px;
  }

  .header-controls {
    width: 100%;
    flex-wrap: wrap;
    gap: 12px;
  }

  .metrics-grid {
    grid-template-columns: 1fr 1fr;
  }

  .charts-grid {
    grid-template-columns: 1fr;
  }

  .chart-card.large {
    grid-column: span 1;
  }

  .pie-chart {
    flex-direction: column;
  }
}

/* Naive UI 样式覆盖 */
:deep(.n-date-picker) {
  --n-border: 1px solid var(--border-tertiary);
  --n-border-hover: 1px solid var(--border-hover);
  --n-border-focus: 1px solid var(--accent-primary);
  --n-color: var(--bg-card);
  --n-text-color: var(--text-primary);
}

:deep(.n-button-group .n-button) {
  border-color: var(--border-tertiary);
}

:deep(.n-button-group .n-button--primary-type) {
  background: var(--accent-primary);
  border-color: var(--accent-primary);
}
</style>
