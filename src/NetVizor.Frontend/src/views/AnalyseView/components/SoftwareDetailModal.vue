<template>
  <n-modal 
    v-model:show="visible" 
    :mask-closable="false"
    preset="card"
    style="width: 90%; max-width: 1000px;"
    :title="software?.displayName || '软件详情'"
    size="huge"
    :bordered="false"
    :segmented="false"
  >
    <div v-if="software" class="software-detail-content">
      <!-- 基本信息和统计 -->
      <div class="detail-header">
        <div class="software-icon">
          <div class="icon-placeholder">
            {{ software.displayName.charAt(0).toUpperCase() }}
          </div>
        </div>
        <div class="software-summary">
          <h2 class="software-name">{{ software.displayName }}</h2>
          <div class="software-meta">
            <span class="process-name">{{ software.processName }}</span>
            <span class="traffic-info">{{ formatBytes(software.totalBytes) }} ({{ software.percentage.toFixed(1) }}%)</span>
            <span class="connection-info">{{ software.connectionCount }} 个连接</span>
          </div>
        </div>
        <div class="detail-stats">
          <div class="stat-item">
            <span class="stat-label">流量占比</span>
            <span class="stat-value">{{ software.percentage.toFixed(1) }}%</span>
          </div>
          <div class="stat-item">
            <span class="stat-label">连接数</span>
            <span class="stat-value">{{ software.connectionCount }}</span>
          </div>
        </div>
      </div>

      <!-- 选项卡内容 -->
      <div class="detail-tabs">
        <n-tabs default-value="overview" type="line">
          <!-- 概览 -->
          <n-tab-pane name="overview" tab="概览">
            <div class="tab-content">
              <div class="overview-grid">
                <!-- 软件信息卡片 -->
                <div class="info-section">
                  <h4 class="section-title">软件信息</h4>
                  <SoftwareInfoCard :software-info="softwareInfo" />
                </div>
                
                <!-- 协议分布图 -->
                <div class="protocol-section">
                  <h4 class="section-title">协议分布</h4>
                  <ProtocolChart :data="protocolData" />
                </div>
              </div>
            </div>
          </n-tab-pane>
          
          <!-- 网络连接 -->
          <n-tab-pane name="network" tab="网络连接">
            <div class="tab-content">
              <div class="network-grid">
                <!-- 连接关系图 -->
                <div class="relation-section">
                  <h4 class="section-title">连接关系</h4>
                  <NetworkRelationChart 
                    :data="networkRelationData"
                    :software="software"
                  />
                </div>
                
                <!-- 端口统计表 -->
                <div class="ports-section">
                  <h4 class="section-title">端口统计</h4>
                  <PortStatsTable :data="portStatsData" />
                </div>
              </div>
            </div>
          </n-tab-pane>
          
          <!-- 流量详情 -->
          <n-tab-pane name="traffic" tab="流量详情">
            <div class="tab-content">
              <div class="traffic-details">
                <div class="traffic-chart-section">
                  <h4 class="section-title">流量趋势 ({{ timeRange }})</h4>
                  <div class="traffic-chart-placeholder">
                    <div class="placeholder-content">
                      <n-icon :component="TrendingUpOutline" size="48" />
                      <p>流量趋势图</p>
                      <p class="placeholder-desc">展示该软件的上传/下载流量变化</p>
                    </div>
                  </div>
                </div>
                
                <div class="traffic-stats-section">
                  <h4 class="section-title">流量统计</h4>
                  <div class="stats-grid">
                    <div class="stat-card">
                      <div class="stat-label">总流量</div>
                      <div class="stat-value">{{ formatBytes(software.totalBytes || 0) }}</div>
                    </div>
                    <div class="stat-card">
                      <div class="stat-label">上传</div>
                      <div class="stat-value">{{ formatBytes(software.uploadBytes || 0) }}</div>
                    </div>
                    <div class="stat-card">
                      <div class="stat-label">下载</div>
                      <div class="stat-value">{{ formatBytes(software.downloadBytes || 0) }}</div>
                    </div>
                    <div class="stat-card">
                      <div class="stat-label">平均速度</div>
                      <div class="stat-value">{{ formatSpeed(averageSpeed) }}</div>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </n-tab-pane>
        </n-tabs>
      </div>
    </div>
    
    <template #action>
      <div class="modal-actions">
        <n-button @click="visible = false">关闭</n-button>
        <n-button type="primary" @click="exportData">
          <template #icon>
            <n-icon :component="DownloadOutline" />
          </template>
          导出数据
        </n-button>
      </div>
    </template>
  </n-modal>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { NModal, NTabs, NTabPane, NButton, NIcon } from 'naive-ui'
import { TrendingUpOutline, DownloadOutline } from '@vicons/ionicons5'

// 导入子组件
import SoftwareInfoCard from './SoftwareInfoCard.vue'
import NetworkRelationChart from './NetworkRelationChart.vue'
import PortStatsTable from './PortStatsTable.vue'
import ProtocolChart from './ProtocolChart.vue'

// Props定义
const props = defineProps<{
  show: boolean
  software?: any
  softwareInfo?: any
  networkRelationData?: any
  portStatsData?: any[]
  protocolData?: any[]
  timeRange?: string
}>()

// Emits
const emit = defineEmits<{
  'update:show': [value: boolean]
}>()

// 计算属性
const visible = computed({
  get: () => props.show,
  set: (value) => emit('update:show', value)
})

// 计算平均速度
const averageSpeed = computed(() => {
  if (!props.software?.totalBytes) return 0
  // 模拟计算，实际应根据时间范围计算
  return props.software.totalBytes / 3600 // 假设1小时的数据
})

// 格式化字节数
const formatBytes = (bytes: number): string => {
  if (bytes === 0) return '0 B'
  
  const k = 1024
  const sizes = ['B', 'KB', 'MB', 'GB', 'TB']
  const i = Math.floor(Math.log(bytes) / Math.log(k))
  
  return `${parseFloat((bytes / Math.pow(k, i)).toFixed(1))} ${sizes[i]}`
}

// 格式化速度
const formatSpeed = (bytesPerSecond: number): string => {
  return formatBytes(bytesPerSecond) + '/s'
}

// 导出数据
const exportData = () => {
  // 实现数据导出功能
  console.log('Export data for:', props.software?.processName)
}
</script>

<style scoped>
.software-detail-content {
  max-height: 70vh;
  overflow-y: auto;
}

/* 详情头部 */
.detail-header {
  display: flex;
  align-items: flex-start;
  gap: 16px;
  margin-bottom: 24px;
  padding-bottom: 16px;
  border-bottom: 1px solid var(--border-secondary);
}

.software-icon .icon-placeholder {
  width: 48px;
  height: 48px;
  border-radius: 12px;
  background: linear-gradient(135deg, var(--accent-primary), var(--accent-secondary));
  display: flex;
  align-items: center;
  justify-content: center;
  color: white;
  font-weight: 600;
  font-size: 18px;
}

.software-summary {
  flex: 1;
}

.software-name {
  font-size: 20px;
  font-weight: 600;
  color: var(--text-primary);
  margin: 0 0 8px 0;
}

.software-meta {
  display: flex;
  flex-direction: column;
  gap: 4px;
  font-size: 13px;
}

.process-name {
  font-family: 'JetBrains Mono', 'Fira Code', monospace;
  color: var(--accent-primary);
  font-weight: 500;
}

.traffic-info {
  color: var(--text-secondary);
  font-weight: 600;
}

.connection-info {
  color: var(--text-muted);
}

.detail-stats {
  display: flex;
  gap: 24px;
}

.stat-item {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 4px;
}

.stat-label {
  font-size: 11px;
  color: var(--text-muted);
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.stat-value {
  font-size: 18px;
  font-weight: 700;
  color: var(--text-primary);
}

/* 选项卡内容 */
.detail-tabs {
  margin-top: 8px;
}

.tab-content {
  padding: 16px 0;
}

/* 概览页面 */
.overview-grid {
  display: grid;
  grid-template-columns: 1fr 300px;
  gap: 24px;
}

.section-title {
  font-size: 14px;
  font-weight: 600;
  color: var(--text-secondary);
  margin: 0 0 12px 0;
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.protocol-section {
  display: flex;
  flex-direction: column;
}

/* 网络连接页面 */
.network-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 24px;
}

.relation-section {
  display: flex;
  flex-direction: column;
}

.ports-section {
  display: flex;
  flex-direction: column;
}

/* 流量详情页面 */
.traffic-details {
  display: flex;
  flex-direction: column;
  gap: 24px;
}

.traffic-chart-placeholder {
  height: 200px;
  background: var(--bg-tertiary);
  border-radius: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
  border: 1px dashed var(--border-secondary);
}

.placeholder-content {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 8px;
  color: var(--text-muted);
}

.placeholder-desc {
  font-size: 12px;
  margin: 0;
}

.stats-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 16px;
}

.stat-card {
  background: var(--bg-tertiary);
  padding: 16px;
  border-radius: 8px;
  border: 1px solid var(--border-secondary);
  text-align: center;
}

.stat-card .stat-label {
  display: block;
  font-size: 12px;
  color: var(--text-muted);
  margin-bottom: 8px;
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.stat-card .stat-value {
  font-size: 20px;
  font-weight: 700;
  color: var(--text-primary);
  font-family: 'JetBrains Mono', 'Fira Code', monospace;
}

/* 模态框操作 */
.modal-actions {
  display: flex;
  gap: 12px;
  justify-content: flex-end;
}

/* 响应式 */
@media (max-width: 768px) {
  .detail-header {
    flex-direction: column;
    align-items: center;
    text-align: center;
  }
  
  .detail-stats {
    justify-content: center;
  }
  
  .overview-grid,
  .network-grid {
    grid-template-columns: 1fr;
  }
  
  .stats-grid {
    grid-template-columns: repeat(2, 1fr);
  }
}

/* Naive UI 覆盖样式 */
:deep(.n-tabs .n-tab-pane) {
  padding: 0;
}

:deep(.n-tabs .n-tabs-nav) {
  --n-tab-color-hover: var(--bg-hover);
  --n-tab-text-color-active: var(--accent-primary);
  --n-bar-color: var(--accent-primary);
}
</style>