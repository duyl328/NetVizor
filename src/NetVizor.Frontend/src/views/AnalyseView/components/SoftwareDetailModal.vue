<template>
  <n-modal v-model:show="showModal" class="software-detail-modal">
    <div class="modal-card">
      <!-- 弹窗头部 -->
      <div class="modal-header">
        <div class="header-left">
          <img
            v-if="analysisData?.appInfo?.icon"
            :src="'data:image/png;base64,' + analysisData.appInfo.icon"
            class="app-icon"
            @error="handleIconError"
          />
          <n-icon v-else :component="DesktopOutline" class="app-icon-fallback" />

          <div class="header-info">
            <h3 class="app-name">{{ analysisData?.appInfo?.name || 'Unknown Application' }}</h3>
            <p class="app-details">
              {{ analysisData?.appInfo?.company || 'Unknown Company' }}
              <span v-if="analysisData?.appInfo?.version"
                >• v{{ analysisData.appInfo.version }}</span
              >
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
          <div class="stat-value">{{ formatBytes(analysisData?.summary?.totalTraffic || 0) }}</div>
          <div class="stat-label">总流量</div>
          <div class="stat-trend">
            <span class="upload">↑ {{ formatBytes(analysisData?.summary?.totalUpload || 0) }}</span>
            <span class="download"
              >↓ {{ formatBytes(analysisData?.summary?.totalDownload || 0) }}</span
            >
          </div>
        </div>

        <div class="stat-card">
          <div class="stat-value">{{ analysisData?.summary?.totalConnections || 0 }}</div>
          <div class="stat-label">总连接数</div>
          <div class="stat-extra">{{ analysisData?.summary?.uniqueRemoteIPs || 0 }} 个远程IP</div>
        </div>

        <div class="stat-card">
          <div class="stat-value">{{ analysisData?.summary?.uniqueRemotePorts || 0 }}</div>
          <div class="stat-label">使用端口</div>
          <div class="stat-extra">{{ timeRangeText }}</div>
        </div>

        <div class="stat-card">
          <div class="stat-value">{{ analysisData?.portAnalysis?.length || 0 }}</div>
          <div class="stat-label">活跃端口</div>
          <div class="stat-extra">多协议通信</div>
        </div>
      </div>

      <div class="modal-body">
        <!-- 错误状态显示 -->
        <div v-if="errorState.hasError" class="error-container">
          <div class="error-content">
            <div class="error-header">
              <n-icon :component="CloseOutline" class="error-icon" />
              <h4 class="error-title">数据加载失败</h4>
            </div>
            <p class="error-message">{{ errorState.message }}</p>
            <div v-if="errorState.details" class="error-details">
              <details>
                <summary class="error-summary">查看详细信息</summary>
                <pre class="error-details-content">{{ errorState.details }}</pre>
              </details>
            </div>
            <div class="error-actions">
              <n-button type="primary" @click="refreshData">
                <template #icon>
                  <n-icon :component="RefreshOutline" />
                </template>
                重试
              </n-button>
            </div>
          </div>
        </div>
        
        <!-- 主要Tab内容区域 -->
        <n-tabs
          v-else
          v-model:value="activeMainTab"
          type="card"
          class="detail-tabs"
          animated
          :tab-style="{ transition: 'all 0.3s cubic-bezier(0.4, 0, 0.2, 1)' }"
        >
          <!-- 网络拓扑Tab -->
          <n-tab-pane name="topology" tab="网络拓扑">
            <transition name="tab-fade" mode="out-in">
              <div class="tab-content" key="topology">
                <div class="chart-section full-height">
                  <div class="section-header">
                    <h4 class="section-title">
                      <n-icon :component="GitNetworkOutline" class="section-icon" />
                      网络连接拓扑
                    </h4>
                    <div class="section-controls">
                      <span class="connection-count"
                        >{{ analysisData?.topConnections?.length || 0 }} 个连接</span
                      >
                      <n-button size="small" @click="refreshData">
                        <template #icon>
                          <n-icon :component="RefreshOutline" />
                        </template>
                        刷新
                      </n-button>
                    </div>
                  </div>
                  <div class="chart-container extra-large">
                    <NetworkAnalysisChart
                      ref="networkChartRef"
                      v-if="analysisData"
                      :key="`network-${componentKey}`"
                      :data="analysisData"
                      :loading="loading"
                    />
                    <div v-else class="empty-chart">
                      <n-icon :component="GitNetworkOutline" size="48" />
                      <span>暂无网络拓扑数据</span>
                    </div>
                  </div>
                </div>
              </div>
            </transition>
          </n-tab-pane>

          <!-- 流量分析Tab -->
          <n-tab-pane name="traffic" tab="流量分析">
            <transition name="tab-slide" mode="out-in">
              <div class="tab-content" key="traffic">
                <div class="analysis-grid">
                  <!-- 协议分布 -->
                  <div class="chart-section">
                    <div class="section-header">
                      <h4 class="section-title">
                        <n-icon :component="StatsChartOutline" class="section-icon" />
                        协议分布
                      </h4>
                    </div>
                    <div class="chart-container large">
                      <ProtocolChart
                        ref="protocolChartRef"
                        v-if="protocolChartData.length > 0"
                        :key="`protocol-${componentKey}`"
                        :data="protocolChartData"
                      />
                      <div v-else class="empty-chart">
                        <n-icon :component="PieChartOutline" size="32" />
                        <span>暂无协议数据</span>
                      </div>
                    </div>
                  </div>

                  <!-- 流量趋势 -->
                  <div class="chart-section">
                    <div class="section-header">
                      <h4 class="section-title">
                        <n-icon :component="TrendingUpOutline" class="section-icon" />
                        流量趋势
                      </h4>
                    </div>
                    <div class="chart-container large">
                      <TimeTrendChart
                        ref="trendChartRef"
                        v-if="timeTrendData.length > 0"
                        :key="`trend-${componentKey}`"
                        :data="timeTrendData"
                      />
                      <div v-else class="empty-chart">
                        <n-icon :component="BarChartOutline" size="32" />
                        <span>暂无趋势数据</span>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </transition>
          </n-tab-pane>

          <!-- 连接详情Tab -->
          <n-tab-pane name="connections" tab="连接详情">
            <transition name="tab-scale" mode="out-in">
              <div class="tab-content table-content" key="connections">
                <div class="table-section">
                  <div class="section-header">
                    <h4 class="section-title">
                      <n-icon :component="GitNetworkOutline" class="section-icon" />
                      连接详情
                    </h4>
                    <div class="section-controls">
                      <span class="data-count"
                        >共 {{ analysisData?.topConnections?.length || 0 }} 条记录</span
                      >
                      <n-button size="small" @click="refreshData">
                        <template #icon>
                          <n-icon :component="RefreshOutline" />
                        </template>
                        刷新
                      </n-button>
                    </div>
                  </div>
                  <div class="table-container">
                    <ConnectionsTable 
                      :key="`connections-${componentKey}`"
                      :data="analysisData?.topConnections || []" 
                    />
                  </div>
                </div>
              </div>
            </transition>
          </n-tab-pane>

          <!-- 端口统计Tab -->
          <n-tab-pane name="ports" tab="端口统计">
            <transition name="tab-scale" mode="out-in">
              <div class="tab-content table-content" key="ports">
                <div class="table-section">
                  <div class="section-header">
                    <h4 class="section-title">
                      <n-icon :component="StatsChartOutline" class="section-icon" />
                      端口统计
                    </h4>
                    <div class="section-controls">
                      <span class="data-count">共 {{ portStatsData.length || 0 }} 个端口</span>
                      <n-button size="small" @click="refreshData">
                        <template #icon>
                          <n-icon :component="RefreshOutline" />
                        </template>
                        刷新
                      </n-button>
                    </div>
                  </div>
                  <div class="table-container">
                    <PortStatsTable 
                      :key="`ports-${componentKey}`"
                      :data="portStatsData" 
                    />
                  </div>
                </div>
              </div>
            </transition>
          </n-tab-pane>

          <!-- 时间线Tab -->
          <n-tab-pane name="timeline" tab="时间线">
            <transition name="tab-scale" mode="out-in">
              <div class="tab-content table-content" key="timeline">
                <div class="table-section">
                  <div class="section-header">
                    <h4 class="section-title">
                      <n-icon :component="TrendingUpOutline" class="section-icon" />
                      活动时间线
                    </h4>
                    <div class="section-controls">
                      <span class="data-count">{{ timeRangeText }} 内的活动</span>
                      <n-button size="small" @click="refreshData">
                        <template #icon>
                          <n-icon :component="RefreshOutline" />
                        </template>
                        刷新
                      </n-button>
                    </div>
                  </div>
                  <div class="table-container">
                    <TimelineView 
                      :key="`timeline-${componentKey}`"
                      :data="analysisData?.timeTrends || []" 
                    />
                  </div>
                </div>
              </div>
            </transition>
          </n-tab-pane>
        </n-tabs>
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
  BarChartOutline,
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
  set: (value) => emit('update:show', value),
})

const analysisData = ref<NetworkAnalysisData | null>(null)
const loading = ref(false)
const activeTab = ref('connections')
const activeMainTab = ref('topology')

// 错误状态
const errorState = ref<{
  hasError: boolean
  message: string
  details?: string
}>({
  hasError: false,
  message: '',
  details: ''
})

// 组件强制刷新key
const componentKey = ref(0)

// 图表引用
const networkChartRef = ref()
const protocolChartRef = ref()
const trendChartRef = ref()

// 刷新所有图表尺寸
const resizeAllCharts = async () => {
  await nextTick()

  // 延迟执行，确保DOM已完全渲染
  setTimeout(() => {
    try {
      // 刷新网络图表 - 使用新的resize方法
      if (networkChartRef.value && typeof networkChartRef.value.resize === 'function') {
        networkChartRef.value.resize()
      }
      // 刷新协议图表
      if (
        protocolChartRef.value &&
        protocolChartRef.value.chartRef &&
        protocolChartRef.value.chartRef.chart
      ) {
        const chart = protocolChartRef.value.chartRef.chart
        if (chart && typeof chart.resize === 'function') {
          chart.resize()
        }
      }
      // 刷新趋势图表
      if (
        trendChartRef.value &&
        trendChartRef.value.chartRef &&
        trendChartRef.value.chartRef.chart
      ) {
        const chart = trendChartRef.value.chartRef.chart
        if (chart && typeof chart.resize === 'function') {
          chart.resize()
        }
      }
    } catch (error) {
      console.warn('图表 resize 失败:', error)
    }
  }, 300)

  // 再次延迟执行，确保图表完全就绪
  setTimeout(() => {
    try {
      if (networkChartRef.value && typeof networkChartRef.value.resize === 'function') {
        networkChartRef.value.resize()
      }
      if (
        protocolChartRef.value &&
        protocolChartRef.value.chartRef &&
        protocolChartRef.value.chartRef.chart
      ) {
        protocolChartRef.value.chartRef.chart.resize()
      }
      if (
        trendChartRef.value &&
        trendChartRef.value.chartRef &&
        trendChartRef.value.chartRef.chart
      ) {
        trendChartRef.value.chartRef.chart.resize()
      }
    } catch (error) {
      console.warn('图表二次 resize 失败:', error)
    }
  }, 600)
}

// 监听Tab切换，刷新图表
watch(activeMainTab, () => {
  resizeAllCharts()
})

// 监听弹窗显示状态 - 强制刷新组件
watch(
  () => props.show,
  (newShow, oldShow) => {
    console.log('[SoftwareDetailModal] 弹窗显示状态变化:', { newShow, oldShow })
    
    if (newShow && !oldShow) {
      // 弹窗从关闭到打开时，强制刷新所有组件
      console.log('[SoftwareDetailModal] 弹窗打开，强制刷新组件，componentKey:', componentKey.value, '->', componentKey.value + 1)
      componentKey.value++
      
      nextTick(() => {
        resizeAllCharts()
      })
    }
  },
)

// 计算属性
const timeRangeText = computed(() => {
  const rangeMap: Record<string, string> = {
    '1hour': '1小时',
    '1day': '24小时',
    '7days': '7天',
    '30days': '30天',
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
    color: colors[index % colors.length],
  }))
})

const timeTrendData = computed(() => {
  if (!analysisData.value?.timeTrends) return []

  return analysisData.value.timeTrends.map((trend) => ({
    timestamp: trend.timestamp * 1000, // 转换为毫秒
    timeStr: trend.timeStr,
    upload: trend.upload,
    download: trend.download,
    connections: trend.connections,
  }))
})

const portStatsData = computed(() => {
  if (!analysisData.value?.portAnalysis || !analysisData.value?.topConnections) return []

  return analysisData.value.portAnalysis.map((port) => {
    // 从连接数据中获取使用该端口的远程主机
    const connectionsForPort = analysisData.value!.topConnections.filter(
      (conn) => conn.remotePort === port.port,
    )

    return {
      port: port.port,
      protocol: port.protocols[0] || 'TCP', // 取第一个协议
      connectionCount: port.connectionCount,
      totalBytes: port.totalTraffic,
      remoteHosts: [...new Set(connectionsForPort.map((conn) => conn.remoteIP))],
    }
  })
})

// API调用
const fetchAnalysisData = async () => {
  if (!props.appId) {
    console.warn('[SoftwareDetailModal] fetchAnalysisData: appId为空，跳过请求')
    return
  }

  console.log('[SoftwareDetailModal] 开始请求网络分析数据:', {
    appId: props.appId,
    timeRange: props.timeRange,
    show: props.show
  })

  loading.value = true
  // 清除之前的错误状态
  errorState.value = {
    hasError: false,
    message: '',
    details: ''
  }
  
  try {
    const params = {
      appId: props.appId,
      timeRange: props.timeRange,
    }

    console.log('[SoftwareDetailModal] 请求参数:', params)
    console.log('[SoftwareDetailModal] 请求URL: /apps/network-analysis')

    // 使用GET方法，参数作为查询字符串
    const response: ApiResponse<NetworkAnalysisData> = await httpClient.get(
      '/apps/network-analysis',
      params,
    )

    console.log('[SoftwareDetailModal] 收到响应:', {
      success: response.success,
      message: response.message,
      hasData: !!response.data,
      dataKeys: response.data ? Object.keys(response.data) : null
    })

    if (response.success && response.data) {
      analysisData.value = response.data
      console.log('[SoftwareDetailModal] 数据设置成功:', {
        appInfo: response.data.appInfo,
        connectionsCount: response.data.topConnections?.length || 0,
        protocolStatsCount: response.data.protocolStats?.length || 0,
        timeTrendsCount: response.data.timeTrends?.length || 0,
        portAnalysisCount: response.data.portAnalysis?.length || 0
      })
    } else {
      console.error('[SoftwareDetailModal] 获取网络分析数据失败:', {
        success: response.success,
        message: response.message,
        data: response.data
      })
      
      // 设置错误状态
      errorState.value = {
        hasError: true,
        message: response.message || '获取数据失败',
        details: `请求成功但返回失败状态: ${JSON.stringify({
          success: response.success,
          message: response.message
        })}`
      }
      
      analysisData.value = null
    }
  } catch (error) {
    console.error('[SoftwareDetailModal] 获取网络分析数据异常:', error)
    
    let errorMessage = '网络请求失败'
    let errorDetails = ''
    
    // 详细的错误信息
    if (error instanceof Error) {
      console.error('[SoftwareDetailModal] 错误详情:', {
        name: error.name,
        message: error.message,
        stack: error.stack
      })
      errorMessage = error.message
      errorDetails = `错误类型: ${error.name}`
    }
    
    // 如果是HTTP错误，尝试获取更多信息
    if (error && typeof error === 'object' && 'response' in error) {
      const httpError = error as any
      console.error('[SoftwareDetailModal] HTTP错误详情:', {
        status: httpError.response?.status,
        statusText: httpError.response?.statusText,
        data: httpError.response?.data,
        url: httpError.config?.url,
        method: httpError.config?.method,
        params: httpError.config?.params
      })
      
      const status = httpError.response?.status
      const responseData = httpError.response?.data
      
      if (status === 400) {
        errorMessage = '请求参数错误 (400)'
        if (responseData?.message) {
          errorMessage += `: ${responseData.message}`
        }
        errorDetails = `请求URL: ${httpError.config?.url}\n请求参数: ${JSON.stringify(httpError.config?.params, null, 2)}\n响应数据: ${JSON.stringify(responseData, null, 2)}`
      } else if (status === 404) {
        errorMessage = 'API接口不存在 (404)'
        errorDetails = `请求URL: ${httpError.config?.url}`
      } else if (status === 500) {
        errorMessage = '服务器内部错误 (500)'
        errorDetails = responseData?.message || '服务器处理请求时发生错误'
      } else {
        errorMessage = `HTTP ${status} 错误`
        errorDetails = httpError.response?.statusText || '未知HTTP错误'
      }
    }
    
    // 设置错误状态
    errorState.value = {
      hasError: true,
      message: errorMessage,
      details: errorDetails
    }
    
    analysisData.value = null
  } finally {
    loading.value = false
    console.log('[SoftwareDetailModal] 请求完成，loading设置为false')
  }
}

// 事件处理
const refreshData = () => {
  console.log('[SoftwareDetailModal] 手动刷新数据')
  // 强制刷新组件
  componentKey.value++
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

watch(
  () => [props.appId, props.timeRange],
  (newValues, oldValues) => {
    console.log('[SoftwareDetailModal] 监听到参数变化:', {
      newValues,
      oldValues,
      show: props.show,
      appId: props.appId
    })
    
    if (props.show && props.appId) {
      console.log('[SoftwareDetailModal] 参数变化触发数据获取')
      fetchAnalysisData()
    } else {
      console.log('[SoftwareDetailModal] 跳过数据获取，条件不满足')
    }
  },
  { flush: 'post' },
)

// 监听数据变化，刷新图表
watch(
  analysisData,
  () => {
    resizeAllCharts()
  },
  { flush: 'post' },
)
</script>

<style scoped>
/* 弹窗基础样式 */
.software-detail-modal :deep(.n-modal) {
  max-width: 90vw;
  max-height: 90vh;
}

.modal-card {
  width: 1400px;
  max-width: 95vw;
  max-height: 95vh;
  background: var(--bg-card);
  border-radius: 16px;
  overflow: hidden;
  display: flex;
  flex-direction: column;
  box-shadow:
    0 20px 25px -5px rgba(0, 0, 0, 0.1),
    0 10px 10px -5px rgba(0, 0, 0, 0.04);
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

/* 主要Tab样式 */
.detail-tabs {
  height: 100%;
  display: flex;
  flex-direction: column;
}

:deep(.detail-tabs .n-tabs) {
  height: 100%;
  display: flex;
  flex-direction: column;
}

:deep(.detail-tabs .n-tabs-nav) {
  flex-shrink: 0;
  background: var(--bg-tertiary);
  border-bottom: 1px solid var(--border-secondary);
  padding: 12px 20px;
  position: relative;
  z-index: 5;
  min-height: 60px;
}

:deep(.detail-tabs .n-tabs-tab-wrapper) {
  margin: 6px 4px;
}

:deep(.detail-tabs .n-tabs-tab) {
  font-weight: 600;
  padding: 12px 20px;
  border-radius: 8px;
  margin-right: 8px;
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
  position: relative;
  overflow: hidden;
}

:deep(.detail-tabs .n-tabs-tab::before) {
  content: '';
  position: absolute;
  top: 0;
  left: -100%;
  width: 100%;
  height: 100%;
  background: linear-gradient(90deg, transparent, rgba(255, 255, 255, 0.1), transparent);
  transition: left 0.6s cubic-bezier(0.4, 0, 0.2, 1);
}

:deep(.detail-tabs .n-tabs-tab:hover::before) {
  left: 100%;
}

:deep(.detail-tabs .n-tabs-tab--active) {
  background: var(--accent-primary);
  color: white;
  transform: translateY(-1px);
  box-shadow: 0 4px 12px rgba(59, 130, 246, 0.3);
  border-top: 3px solid var(--accent-primary);
  border-left: 1px solid var(--accent-primary);
  border-right: 1px solid var(--accent-primary);
  border-bottom: none;
  margin-bottom: -1px;
  z-index: 10;
  position: relative;
}

:deep(.detail-tabs .n-tabs-tab:hover:not(.n-tabs-tab--active)) {
  background: var(--bg-hover);
  transform: translateY(-1px);
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

:deep(.detail-tabs .n-tabs-content) {
  flex: 1;
  overflow: hidden;
}

:deep(.detail-tabs .n-tab-pane) {
  height: 100%;
  padding: 0;
}

/* Tab内容 */
.tab-content {
  height: 100%;
  display: flex;
  flex-direction: column;
  background: var(--bg-secondary);
  overflow: hidden;
}

.tab-content.table-content {
  background: var(--bg-card);
}

/* Tab切换动画 */
.tab-fade-enter-active,
.tab-fade-leave-active {
  transition: all 0.4s cubic-bezier(0.4, 0, 0.2, 1);
}

.tab-fade-enter-from {
  opacity: 0;
  transform: translateY(20px) scale(0.95);
}

.tab-fade-leave-to {
  opacity: 0;
  transform: translateY(-20px) scale(0.95);
}

.tab-slide-enter-active,
.tab-slide-leave-active {
  transition: all 0.5s cubic-bezier(0.4, 0, 0.2, 1);
}

.tab-slide-enter-from {
  opacity: 0;
  transform: translateX(30px) rotateY(10deg);
}

.tab-slide-leave-to {
  opacity: 0;
  transform: translateX(-30px) rotateY(-10deg);
}

.tab-scale-enter-active,
.tab-scale-leave-active {
  transition: all 0.35s cubic-bezier(0.34, 1.56, 0.64, 1);
}

.tab-scale-enter-from {
  opacity: 0;
  transform: scale(0.9) translateY(10px);
}

.tab-scale-leave-to {
  opacity: 0;
  transform: scale(1.1) translateY(-10px);
}

/* 图表容器动画 */
.chart-container {
  transition: all 0.3s ease;
}

.chart-container:hover {
  transform: translateY(-2px);
}

.chart-section {
  transition: all 0.3s ease;
}

.chart-section:hover {
  box-shadow: 0 8px 25px rgba(0, 0, 0, 0.1);
  border-color: var(--accent-primary);
}

.table-section:hover {
  box-shadow: 0 8px 25px rgba(0, 0, 0, 0.1);
  border-color: var(--accent-primary);
}

/* 分析网格（流量分析Tab中的布局） */
.analysis-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 16px;
  padding: 20px;
  height: 100%;
  overflow-y: auto;
}

/* 图表区域 */
.chart-section {
  background: var(--bg-card);
  border: 1px solid var(--border-primary);
  border-radius: 12px;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.chart-section.full-height {
  margin: 20px;
  height: calc(100% - 40px);
  min-height: 650px;
}

.table-section {
  background: var(--bg-card);
  border: 1px solid var(--border-primary);
  border-radius: 12px;
  margin: 20px;
  height: calc(100% - 40px);
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.section-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 16px 20px;
  border-bottom: 1px solid var(--border-secondary);
  flex-shrink: 0;
  background: var(--bg-tertiary);
  transition: all 0.3s ease;
}

.section-header:hover {
  background: var(--bg-hover);
}

.section-controls {
  display: flex;
  align-items: center;
  gap: 12px;
}

.data-count {
  font-size: 12px;
  color: var(--text-muted);
  padding: 4px 8px;
  background: var(--bg-secondary);
  border-radius: 6px;
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

.chart-container.large {
  height: 300px;
}

.chart-container.extra-large {
  height: 100%;
  min-height: 600px;
}

.table-container {
  flex: 1;
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
  font-size: 14px;
  font-weight: 500;
}

/* 滚动条美化 */
.tab-content::-webkit-scrollbar,
.table-container::-webkit-scrollbar {
  width: 8px;
}

.tab-content::-webkit-scrollbar-track,
.table-container::-webkit-scrollbar-track {
  background: var(--bg-tertiary);
  border-radius: 4px;
}

.tab-content::-webkit-scrollbar-thumb,
.table-container::-webkit-scrollbar-thumb {
  background: var(--border-secondary);
  border-radius: 4px;
  transition: background 0.3s;
}

.tab-content::-webkit-scrollbar-thumb:hover,
.table-container::-webkit-scrollbar-thumb:hover {
  background: var(--border-hover);
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

  .analysis-grid {
    grid-template-columns: 1fr;
    gap: 12px;
    padding: 16px;
  }

  .chart-container.large {
    height: 250px;
  }

  :deep(.detail-tabs .n-tabs-tab) {
    padding: 10px 16px;
    font-size: 14px;
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

  .app-icon,
  .app-icon-fallback {
    width: 40px;
    height: 40px;
  }

  .stats-overview {
    grid-template-columns: repeat(2, 1fr);
  }

  .stat-card {
    padding: 16px;
  }

  .analysis-grid {
    grid-template-columns: 1fr;
    padding: 12px;
  }

  .chart-section.full-height,
  .table-section {
    margin: 12px;
    height: calc(100% - 24px);
  }

  .chart-container.large {
    height: 200px;
  }

  .section-header {
    padding: 12px 16px;
    flex-direction: column;
    align-items: flex-start;
    gap: 8px;
  }

  .section-controls {
    width: 100%;
    justify-content: space-between;
  }

  :deep(.detail-tabs .n-tabs-nav) {
    padding: 8px 12px;
  }

  :deep(.detail-tabs .n-tabs-tab) {
    padding: 8px 12px;
    font-size: 13px;
    margin-right: 4px;
  }
}

/* 错误状态样式 */
.error-container {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 40px 20px;
  background: var(--bg-secondary);
}

.error-content {
  max-width: 600px;
  text-align: center;
  background: var(--bg-card);
  border: 1px solid var(--border-primary);
  border-radius: 12px;
  padding: 32px;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
}

.error-header {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 12px;
  margin-bottom: 16px;
}

.error-icon {
  color: var(--accent-error);
  font-size: 24px;
}

.error-title {
  margin: 0;
  font-size: 20px;
  font-weight: 600;
  color: var(--text-primary);
}

.error-message {
  margin: 0 0 20px 0;
  font-size: 16px;
  color: var(--text-secondary);
  line-height: 1.5;
}

.error-details {
  margin: 20px 0;
  text-align: left;
}

.error-summary {
  font-size: 14px;
  color: var(--text-muted);
  cursor: pointer;
  padding: 8px;
  background: var(--bg-tertiary);
  border-radius: 6px;
  user-select: none;
}

.error-summary:hover {
  background: var(--bg-hover);
}

.error-details-content {
  margin: 12px 0 0 0;
  padding: 12px;
  background: var(--bg-tertiary);
  border: 1px solid var(--border-secondary);
  border-radius: 6px;
  font-size: 12px;
  color: var(--text-secondary);
  white-space: pre-wrap;
  word-break: break-all;
  max-height: 200px;
  overflow-y: auto;
}

.error-actions {
  margin-top: 24px;
}

/* 暗色主题适配 */
@media (prefers-color-scheme: dark) {
  .modal-card {
    box-shadow:
      0 20px 25px -5px rgba(0, 0, 0, 0.4),
      0 10px 10px -5px rgba(0, 0, 0, 0.2);
  }
}
</style>
