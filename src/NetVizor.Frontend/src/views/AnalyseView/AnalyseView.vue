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
      <div class="metrics-grid">
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
                style="min-width: 120px;"
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

        <!-- 协议分布饼图 -->
        <div class="chart-card">
          <div class="chart-header">
            <h3 class="chart-title">协议分布</h3>
          </div>
          <div class="chart-body">
            <div class="pie-chart">
              <div class="pie-center">
                <svg viewBox="0 0 100 100" class="pie-svg">
                  <circle
                    cx="50"
                    cy="50"
                    r="40"
                    fill="none"
                    stroke="var(--accent-primary)"
                    stroke-width="20"
                    stroke-dasharray="75 125"
                    transform="rotate(-90 50 50)"
                  />
                  <circle
                    cx="50"
                    cy="50"
                    r="40"
                    fill="none"
                    stroke="var(--accent-secondary)"
                    stroke-width="20"
                    stroke-dasharray="50 125"
                    stroke-dashoffset="-75"
                    transform="rotate(-90 50 50)"
                  />
                  <circle
                    cx="50"
                    cy="50"
                    r="40"
                    fill="none"
                    stroke="var(--accent-warning)"
                    stroke-width="20"
                    stroke-dasharray="35 125"
                    stroke-dashoffset="-125"
                    transform="rotate(-90 50 50)"
                  />
                  <circle
                    cx="50"
                    cy="50"
                    r="40"
                    fill="none"
                    stroke="var(--accent-error)"
                    stroke-width="20"
                    stroke-dasharray="40 125"
                    stroke-dashoffset="-160"
                    transform="rotate(-90 50 50)"
                  />
                </svg>
              </div>
              <div class="pie-legend">
                <div class="legend-item">
                  <span class="legend-dot" style="background: var(--accent-primary)"></span>
                  <span>HTTPS (37.5%)</span>
                </div>
                <div class="legend-item">
                  <span class="legend-dot" style="background: var(--accent-secondary)"></span>
                  <span>HTTP (25%)</span>
                </div>
                <div class="legend-item">
                  <span class="legend-dot" style="background: var(--accent-warning)"></span>
                  <span>DNS (17.5%)</span>
                </div>
                <div class="legend-item">
                  <span class="legend-dot" style="background: var(--accent-error)"></span>
                  <span>其他 (20%)</span>
                </div>
              </div>
            </div>
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

      <!-- 底部软件流量分析区域 -->
      <div class="bottom-analysis-area">
        <div class="traffic-ranking-panel">
          <div class="panel-header">
            <h3 class="panel-title">软件流量TOP榜</h3>
            <div class="panel-controls">
              <n-button-group size="small">
                <n-button 
                  v-for="range in rankingTimeRanges" 
                  :key="range"
                  :type="selectedRankingRange === range ? 'primary' : 'default'"
                  @click="selectedRankingRange = range"
                >
                  {{ range }}
                </n-button>
              </n-button-group>
            </div>
          </div>
          <div class="ranking-content">
            <SoftwareRankingList 
              :data="softwareRankingData"
              :time-range="selectedRankingRange"
              :selected-software="selectedSoftware"
              @select-software="onSelectSoftware"
            />
          </div>
        </div>
        
        <div class="software-detail-panel">
          <div class="panel-header">
            <h3 class="panel-title">
              {{ selectedSoftware?.displayName || '选择软件查看详情' }}
            </h3>
          </div>
          <div class="detail-content">
            <div v-if="selectedSoftware" class="software-details">
              <!-- 软件基本信息 -->
              <div class="detail-section">
                <h4 class="section-title">软件信息</h4>
                <SoftwareInfoCard :software-info="selectedSoftwareInfo" />
              </div>
              
              <!-- 网络连接关系图 -->
              <div class="detail-section">
                <h4 class="section-title">网络连接</h4>
                <NetworkRelationChart 
                  :data="networkRelationData"
                  :software="selectedSoftware"
                />
              </div>
              
              <!-- 端口统计 -->
              <div class="detail-section">
                <h4 class="section-title">端口统计</h4>
                <PortStatsTable :data="portStatsData" />
              </div>
            </div>
            <div v-else class="empty-state">
              <n-icon :component="ServerOutline" size="48" />
              <p>请从左侧列表选择软件查看详情</p>
            </div>
          </div>
        </div>
      </div>
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

// 导入组件
import TrafficTrendChart from './components/TrafficTrendChart.vue'
import TopAppsChart from './components/TopAppsChart.vue'
import SoftwareRankingList from './components/SoftwareRankingList.vue'
import SoftwareInfoCard from './components/SoftwareInfoCard.vue'
import NetworkRelationChart from './components/NetworkRelationChart.vue'
import PortStatsTable from './components/PortStatsTable.vue'

// 时间范围选项
const timeRanges = ref([
  { type: '1hour', name: '1小时', available: true },
  { type: '1day', name: '24小时', available: true },
  { type: '7days', name: '7天', available: true },
  { type: '30days', name: '30天', available: false },
])

const selectedTimeRange = ref('1day')

// 网络接口选项
const interfaceOptions = ref([
  { label: '全部网卡', value: 'all' },
  { label: '以太网', value: 'eth0' },
  { label: 'WiFi', value: 'wifi0' },
])

const selectedInterface = ref('all')

// 软件排行时间范围
const rankingTimeRanges = ref(['1小时', '1天', '7天', '30天'])
const selectedRankingRange = ref('1天')

// 选中的软件
const selectedSoftware = ref<any>(null)

// Mock数据
const trafficTrendData = ref([
  { timestamp: Date.now() - 3600000, uploadSpeed: 1024000, downloadSpeed: 5120000 },
  { timestamp: Date.now() - 2400000, uploadSpeed: 2048000, downloadSpeed: 8192000 },
  { timestamp: Date.now() - 1200000, uploadSpeed: 1536000, downloadSpeed: 6144000 },
  { timestamp: Date.now(), uploadSpeed: 3072000, downloadSpeed: 12288000 },
])

const topAppsData = ref([
  { processName: 'chrome.exe', displayName: 'Google Chrome', totalBytes: 1073741824, percentage: 45.2 },
  { processName: 'teams.exe', displayName: 'Microsoft Teams', totalBytes: 536870912, percentage: 22.5 },
  { processName: 'spotify.exe', displayName: 'Spotify', totalBytes: 268435456, percentage: 11.3 },
  { processName: 'code.exe', displayName: 'VS Code', totalBytes: 134217728, percentage: 5.6 },
  { processName: 'slack.exe', displayName: 'Slack', totalBytes: 67108864, percentage: 2.8 },
])

const softwareRankingData = ref([
  { 
    rank: 1, 
    processName: 'chrome.exe', 
    displayName: 'Google Chrome', 
    totalBytes: 1073741824, 
    percentage: 45.2,
    connectionCount: 23 
  },
  { 
    rank: 2, 
    processName: 'teams.exe', 
    displayName: 'Microsoft Teams', 
    totalBytes: 536870912, 
    percentage: 22.5,
    connectionCount: 12 
  },
  // ... 更多数据
])

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

// 事件处理
const onSelectSoftware = (software: any) => {
  selectedSoftware.value = software
}

const refreshData = () => {
  // 刷新数据的逻辑
  console.log('Refreshing data...')
}

// 监听时间范围变化
watch(selectedTimeRange, () => {
  // 重新加载数据
})

watch(selectedInterface, () => {
  // 重新加载趋势数据
})

onMounted(() => {
  // 初始化时选择第一个软件
  if (softwareRankingData.value.length > 0) {
    selectedSoftware.value = softwareRankingData.value[0]
  }
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
  grid-template-columns: 2fr 1fr 1fr;
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

/* 底部分析区域 */
.bottom-analysis-area {
  display: grid;
  grid-template-columns: 400px 1fr;
  gap: 20px;
  margin-top: 24px;
}

.traffic-ranking-panel,
.software-detail-panel {
  background: var(--bg-card);
  backdrop-filter: var(--backdrop-blur);
  border: 1px solid var(--border-primary);
  border-radius: 12px;
  overflow: hidden;
  height: 600px;
  display: flex;
  flex-direction: column;
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

.ranking-content,
.detail-content {
  flex: 1;
  overflow: hidden;
}

.software-details {
  padding: 20px;
  display: flex;
  flex-direction: column;
  gap: 24px;
  height: 100%;
  overflow-y: auto;
}

.detail-section {
  flex-shrink: 0;
}

.section-title {
  font-size: 14px;
  font-weight: 600;
  color: var(--text-secondary);
  margin: 0 0 12px 0;
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  height: 100%;
  color: var(--text-muted);
  gap: 16px;
}

.empty-state p {
  margin: 0;
  font-size: 14px;
}


/* 响应式 */
@media (max-width: 1400px) {
  .bottom-analysis-area {
    grid-template-columns: 350px 1fr;
  }
}

@media (max-width: 1200px) {
  .charts-grid {
    grid-template-columns: 1fr 1fr;
  }

  .chart-card.large {
    grid-column: span 2;
  }
  
  .bottom-analysis-area {
    grid-template-columns: 1fr;
    gap: 16px;
  }
  
  .traffic-ranking-panel,
  .software-detail-panel {
    height: 500px;
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
