<template>
  <div class="analyse-view">
    <div class="analyse-container">
      <!-- 顶部概览 -->
      <div class="analyse-header">
        <div class="header-info">
          <h2 class="view-title">流量分析</h2>
          <p class="view-subtitle">深入分析网络流量模式和安全威胁</p>
        </div>
        <div class="header-actions">
          <n-date-picker
            v-model:value="dateRange"
            type="daterange"
            clearable
            size="medium"
          />
          <n-button type="primary" size="medium">
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
              <n-button-group size="tiny">
                <n-button>1小时</n-button>
                <n-button type="primary">24小时</n-button>
                <n-button>7天</n-button>
                <n-button>30天</n-button>
              </n-button-group>
            </div>
          </div>
          <div class="chart-body">
            <div class="chart-placeholder">
              <div class="chart-line"></div>
              <svg viewBox="0 0 400 200" class="chart-svg">
                <path
                  d="M 0 180 Q 100 100 200 120 T 400 80"
                  fill="none"
                  stroke="var(--accent-primary)"
                  stroke-width="2"
                />
                <path
                  d="M 0 180 Q 100 100 200 120 T 400 80 L 400 200 L 0 200 Z"
                  fill="url(#gradient)"
                  opacity="0.2"
                />
                <defs>
                  <linearGradient id="gradient" x1="0%" y1="0%" x2="0%" y2="100%">
                    <stop offset="0%" style="stop-color:var(--accent-primary);stop-opacity:0.8" />
                    <stop offset="100%" style="stop-color:var(--accent-primary);stop-opacity:0" />
                  </linearGradient>
                </defs>
              </svg>
            </div>
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

        <!-- Top 应用 -->
        <div class="chart-card">
          <div class="chart-header">
            <h3 class="chart-title">Top 应用流量</h3>
          </div>
          <div class="chart-body">
            <div class="app-list">
              <div v-for="(app, index) in topApps" :key="index" class="app-item">
                <div class="app-info">
                  <span class="app-rank">{{ index + 1 }}</span>
                  <span class="app-name">{{ app.name }}</span>
                </div>
                <div class="app-traffic">
                  <div class="traffic-bar">
                    <div
                      class="traffic-fill"
                      :style="{ width: app.percentage + '%' }"
                    ></div>
                  </div>
                  <span class="traffic-text">{{ app.traffic }}</span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- 详细表格 -->
      <div class="detail-panel">
        <div class="panel-header">
          <h3 class="panel-title">异常流量检测</h3>
          <div class="panel-controls">
            <n-tag type="error" size="small">
              {{ anomalies.length }} 个异常
            </n-tag>
          </div>
        </div>
        <div class="anomaly-list">
          <div v-for="anomaly in anomalies" :key="anomaly.id" class="anomaly-item">
            <div class="anomaly-severity" :class="'severity-' + anomaly.severity">
              <n-icon
                :component="anomaly.severity === 'high' ? AlertOutline :
                           anomaly.severity === 'medium' ? WarningOutline :
                           InformationCircleOutline"
                size="16"
              />
            </div>
            <div class="anomaly-info">
              <div class="anomaly-title">{{ anomaly.title }}</div>
              <div class="anomaly-details">
                <span>{{ anomaly.time }}</span>
                <span>•</span>
                <span>{{ anomaly.source }}</span>
                <span>→</span>
                <span>{{ anomaly.destination }}</span>
              </div>
            </div>
            <div class="anomaly-action">
              <n-button size="tiny" quaternary>查看详情</n-button>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { NButton, NButtonGroup, NIcon, NDatePicker, NTag } from 'naive-ui'
import {
  RefreshOutline,
  TrendingUpOutline,
  WarningOutline,
  SpeedometerOutline,
  AlertCircleOutline,
  AlertOutline,
  InformationCircleOutline,
} from '@vicons/ionicons5'

// 日期范围
const dateRange = ref<[number, number] | null>(null)

// Top 应用数据
const topApps = ref([
  { name: 'Chrome', traffic: '523 GB', percentage: 85 },
  { name: 'Microsoft Teams', traffic: '387 GB', percentage: 63 },
  { name: 'Spotify', traffic: '256 GB', percentage: 42 },
  { name: 'Visual Studio Code', traffic: '198 GB', percentage: 32 },
  { name: 'Slack', traffic: '142 GB', percentage: 23 },
])

// 异常数据
const anomalies = ref([
  {
    id: 1,
    severity: 'high',
    title: '异常大量数据传输',
    time: '10:23:45',
    source: '192.168.1.105',
    destination: '104.21.58.93',
  },
  {
    id: 2,
    severity: 'medium',
    title: '可疑端口扫描活动',
    time: '10:15:32',
    source: '10.0.0.158',
    destination: '192.168.1.1',
  },
  {
    id: 3,
    severity: 'low',
    title: '非标准协议使用',
    time: '09:58:21',
    source: '192.168.1.201',
    destination: '172.16.0.1',
  },
])
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

.header-actions {
  display: flex;
  gap: 12px;
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

/* 详细面板 */
.detail-panel {
  background: var(--bg-card);
  backdrop-filter: var(--backdrop-blur);
  border: 1px solid var(--border-primary);
  border-radius: 12px;
  overflow: hidden;
}

.panel-header {
  padding: 20px 24px;
  border-bottom: 1px solid var(--border-primary);
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.panel-title {
  font-size: 18px;
  font-weight: 600;
  color: var(--text-primary);
  margin: 0;
}

/* 异常列表 */
.anomaly-list {
  padding: 12px;
}

.anomaly-item {
  display: flex;
  align-items: center;
  gap: 16px;
  padding: 12px 16px;
  background: var(--bg-tertiary);
  border-radius: 8px;
  margin-bottom: 8px;
  transition: var(--transition);
}

.anomaly-item:hover {
  background: var(--bg-hover);
}

.anomaly-item:last-child {
  margin-bottom: 0;
}

.anomaly-severity {
  width: 32px;
  height: 32px;
  border-radius: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
}

.severity-high {
  background: rgba(239, 68, 68, 0.1);
  color: var(--accent-error);
}

.severity-medium {
  background: rgba(245, 158, 11, 0.1);
  color: var(--accent-warning);
}

.severity-low {
  background: rgba(59, 130, 246, 0.1);
  color: var(--accent-primary);
}

.anomaly-info {
  flex: 1;
}

.anomaly-title {
  font-size: 14px;
  font-weight: 500;
  color: var(--text-primary);
  margin-bottom: 4px;
}

.anomaly-details {
  font-size: 12px;
  color: var(--text-muted);
  display: flex;
  align-items: center;
  gap: 8px;
}

/* 响应式 */
@media (max-width: 1200px) {
  .charts-grid {
    grid-template-columns: 1fr 1fr;
  }

  .chart-card.large {
    grid-column: span 2;
  }
}

@media (max-width: 768px) {
  .analyse-header {
    flex-direction: column;
    align-items: flex-start;
    gap: 16px;
  }

  .header-actions {
    width: 100%;
    flex-wrap: wrap;
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
