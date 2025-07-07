<template>
  <div class="inspector scrollbar-purple scrollbar-glow" :style="{ width: width + 'px' }">
    <div class="inspector-content">
      <div class="inspector-header">
        <h3 class="inspector-title">
          <span class="title-icon">🔍</span>
          连接详情
        </h3>
      </div>

      <div v-if="selectedConnection" class="inspector-body">
        <!-- 基本信息 -->
        <div class="detail-section">
          <h4 class="detail-section-title">基本信息</h4>
          <div class="detail-grid">
            <div class="detail-item">
              <span class="detail-label">进程</span>
              <span class="detail-value">{{ selectedConnection.process }}</span>
            </div>
            <div class="detail-item">
              <span class="detail-label">PID</span>
              <span class="detail-value">{{ selectedConnection.pid || '12345' }}</span>
            </div>
            <div class="detail-item">
              <span class="detail-label">协议</span>
              <span class="detail-value">{{ selectedConnection.protocol || 'TCP' }}</span>
            </div>
            <div class="detail-item">
              <span class="detail-label">状态</span>
              <span class="detail-value" :class="`status-${selectedConnection.status}`">
                {{ getStatusText(selectedConnection.status) }}
              </span>
            </div>
          </div>
        </div>

        <!-- 网络信息 -->
        <div class="detail-section">
          <h4 class="detail-section-title">网络信息</h4>
          <div class="network-info">
            <div class="network-item">
              <div class="network-label">本地地址</div>
              <div class="network-value">{{ selectedConnection.localAddress }}:{{ selectedConnection.localPort || '54321' }}</div>
            </div>
            <div class="network-item">
              <div class="network-label">远程地址</div>
              <div class="network-value">{{ selectedConnection.remoteIp || '142.250.191.14' }}:{{ selectedConnection.remotePort || '443' }}</div>
            </div>
            <div class="network-item">
              <div class="network-label">域名</div>
              <div class="network-value">{{ selectedConnection.remoteAddress }}</div>
            </div>
          </div>
        </div>

        <!-- 流量统计 -->
        <div class="detail-section">
          <h4 class="detail-section-title">流量统计</h4>
          <div class="traffic-stats">
            <div class="traffic-item">
              <span class="traffic-label">上传</span>
              <span class="traffic-value upload">{{ trafficData.upload }}</span>
            </div>
            <div class="traffic-item">
              <span class="traffic-label">下载</span>
              <span class="traffic-value download">{{ trafficData.download }}</span>
            </div>
            <div class="traffic-item">
              <span class="traffic-label">总计</span>
              <span class="traffic-value total">{{ trafficData.total }}</span>
            </div>
          </div>

          <TrafficChart :data="chartData" />
        </div>

        <!-- 安全信息 -->
        <div class="detail-section">
          <h4 class="detail-section-title">安全信息</h4>
          <div class="security-info">
            <div class="security-item safe">
              <div class="security-icon">✅</div>
              <div class="security-text">
                <div class="security-title">连接安全</div>
                <div class="security-desc">HTTPS 加密连接</div>
              </div>
            </div>
            <div class="security-item safe">
              <div class="security-icon">🛡️</div>
              <div class="security-text">
                <div class="security-title">证书有效</div>
                <div class="security-desc">由 Google Trust Services 签发</div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- 空状态 -->
      <div v-else class="empty-state">
        <div class="empty-icon">📊</div>
        <div class="empty-text">选择一个连接以查看详情</div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { storeToRefs } from 'pinia'
import TrafficChart from './components/TrafficChart.vue'
// 假设我们有一个连接状态的store，将来可以添加
// import { useConnectionStore } from '@/stores/connection'

// Props - 只保留布局相关的
const props = defineProps<{
  width: number
}>()

// 从store获取选中的连接 - 将来可以从pinia获取
// const connectionStore = useConnectionStore()
// const { selectedConnection } = storeToRefs(connectionStore)

// 临时使用ref，将来替换为store
const selectedConnection = ref<any>(null)

// 状态文本映射
const getStatusText = (status: string) => {
  const statusMap: Record<string, string> = {
    established: '已建立',
    listening: '监听中',
    close_wait: '等待关闭',
    time_wait: '时间等待',
    closed: '已关闭'
  }
  return statusMap[status] || status
}

// 流量数据
const trafficData = computed(() => {
  if (!selectedConnection.value) {
    return { upload: '0 B', download: '0 B', total: '0 B' }
  }

  // 模拟数据 - 将来可以从store或API获取真实数据
  return {
    upload: '1.2 MB',
    download: '5.8 MB',
    total: '7.0 MB'
  }
})

// 图表数据
const chartData = computed(() => {
  // 生成模拟的流量图表数据 - 将来可以从store获取真实数据
  return Array.from({ length: 20 }, () => Math.random() * 100)
})

// 提供一个方法让外部组件设置选中的连接（过渡期使用）
// 将来可以移除，直接通过store管理
const setSelectedConnection = (connection: any) => {
  selectedConnection.value = connection
}

// 暴露方法给父组件（如果需要的话）
defineExpose({
  setSelectedConnection
})
</script>

<style scoped>
/* 检查器容器 */
.inspector {
  background: var(--bg-glass);
  backdrop-filter: var(--backdrop-blur);
  border-left: 1px solid var(--border-primary);
  overflow: hidden;
  flex-shrink: 0;
}

.inspector-content {
  height: 100%;
  overflow-y: auto;
  display: flex;
  flex-direction: column;
}

/* 头部 */
.inspector-header {
  padding: 16px 24px;
  border-bottom: 1px solid var(--border-secondary);
  background: var(--bg-card);
  flex-shrink: 0;
}

.inspector-title {
  font-size: 16px;
  font-weight: 600;
  color: var(--text-secondary);
  margin: 0;
  display: flex;
  align-items: center;
  gap: 8px;
}

.title-icon {
  font-size: 16px;
}

/* 主体内容 */
.inspector-body {
  flex: 1;
  padding: 24px;
  min-height: 0;
  overflow-y: auto;
}

/* 详情区块 */
.detail-section {
  margin-bottom: 24px;
}

.detail-section:last-child {
  margin-bottom: 0;
}

.detail-section-title {
  font-size: 14px;
  font-weight: 600;
  color: var(--text-quaternary);
  margin: 0 0 16px 0;
  padding-bottom: 8px;
  border-bottom: 1px solid var(--border-tertiary);
}

/* 基本信息网格 */
.detail-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 12px 16px;
}

.detail-item {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.detail-label {
  font-size: 11px;
  color: var(--text-muted);
  text-transform: uppercase;
  letter-spacing: 0.5px;
  font-weight: 600;
}

.detail-value {
  font-size: 13px;
  color: var(--text-secondary);
  font-weight: 500;
}

.detail-value.status-established {
  color: var(--accent-success);
}

/* 网络信息 */
.network-info {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.network-item {
  padding: 12px;
  background: var(--bg-card);
  border-radius: 8px;
  border: 1px solid var(--border-tertiary);
}

.network-label {
  font-size: 11px;
  color: var(--text-muted);
  text-transform: uppercase;
  letter-spacing: 0.5px;
  font-weight: 600;
  margin-bottom: 4px;
}

.network-value {
  font-size: 13px;
  color: var(--text-secondary);
  font-family: 'JetBrains Mono', 'Fira Code', monospace;
  font-weight: 500;
}

/* 流量统计 */
.traffic-stats {
  display: flex;
  flex-direction: column;
  gap: 8px;
  margin-bottom: 16px;
}

.traffic-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 8px 12px;
  background: var(--bg-card);
  border-radius: 6px;
}

.traffic-label {
  font-size: 12px;
  color: var(--text-muted);
  font-weight: 500;
}

.traffic-value {
  font-size: 13px;
  font-weight: 600;
  font-family: 'JetBrains Mono', 'Fira Code', monospace;
}

.traffic-value.upload {
  color: var(--accent-warning);
}

.traffic-value.download {
  color: var(--accent-secondary);
}

.traffic-value.total {
  color: var(--accent-purple);
}

/* 安全信息 */
.security-info {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.security-item {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 12px;
  border-radius: 8px;
  border: 1px solid var(--border-tertiary);
}

.security-item.safe {
  background: rgba(34, 197, 94, 0.1);
  border-color: rgba(34, 197, 94, 0.2);
}

.security-icon {
  font-size: 16px;
  width: 24px;
  text-align: center;
}

.security-text {
  flex: 1;
}

.security-title {
  font-size: 13px;
  font-weight: 600;
  color: var(--text-secondary);
  line-height: 1;
}

.security-desc {
  font-size: 11px;
  color: var(--text-muted);
  margin-top: 2px;
}

/* 空状态 */
.empty-state {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 48px 24px;
  color: var(--text-muted);
}

.empty-icon {
  font-size: 48px;
  margin-bottom: 16px;
  opacity: 0.5;
}

.empty-text {
  font-size: 14px;
  text-align: center;
}

/* 响应式 */
@media (max-width: 1200px) {
  .detail-grid {
    grid-template-columns: 1fr;
  }
}
</style>
