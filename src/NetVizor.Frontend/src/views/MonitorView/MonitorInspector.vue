<template>
  <div class="inspector scrollbar-purple scrollbar-glow" :style="{ width: width + 'px' }">
    <div class="inspector-content">
      <div class="inspector-header">
        <h3 class="inspector-title">
          <span class="title-icon">🔍</span>
          应用详情
        </h3>
      </div>

      <div v-if="isInspecting && inspectingAppDetails" class="inspector-body">
        <div class="detail-section">
          <div class="app-header">
            <div class="app-icon">
              <img
                v-if="inspectingAppDetails.iconBase64"
                :src="'data:image/png;base64,' + inspectingAppDetails.iconBase64"
                :alt="inspectingAppDetails.productName"
              />
              <div v-else class="app-icon-placeholder">?</div>
            </div>
            <div class="app-title-group">
              <h4 class="app-title">{{ inspectingAppDetails.productName || '未知应用' }}</h4>
              <p class="app-subtitle">{{ inspectingAppDetails.fileDescription }}</p>
            </div>
          </div>
        </div>

        <!-- 进程信息 -->
        <div class="detail-section">
          <h4 class="detail-section-title">进程信息</h4>
          <div class="detail-grid">
            <div class="detail-item">
              <span class="detail-label">进程名称</span>
              <span class="detail-value">{{ inspectingAppDetails.processName }}</span>
            </div>
            <div class="detail-item">
              <span class="detail-label">进程数</span>
              <span class="detail-value">{{ inspectingAppDetails.processIds.length }}</span>
            </div>
            <div class="detail-item">
              <span class="detail-label">内存占用</span>
              <span class="detail-value">{{ formatMemory(inspectingAppDetails.useMemory) }}</span>
            </div>
            <div class="detail-item">
              <span class="detail-label">线程数</span>
              <span class="detail-value">{{ inspectingAppDetails.threadCount }}</span>
            </div>
            <div class="detail-item full-width">
              <span class="detail-label">文件路径</span>
              <span class="detail-value code">{{ inspectingAppDetails.mainModulePath }}</span>
            </div>
          </div>
        </div>

        <!-- 文件详情 -->
        <div class="detail-section">
          <h4 class="detail-section-title">文件详情</h4>
          <div class="detail-grid">
            <div class="detail-item">
              <span class="detail-label">公司</span>
              <span class="detail-value">{{ inspectingAppDetails.companyName }}</span>
            </div>
            <div class="detail-item">
              <span class="detail-label">版本</span>
              <span class="detail-value">{{ inspectingAppDetails.version }}</span>
            </div>
            <div class="detail-item full-width">
              <span class="detail-label">版权</span>
              <span class="detail-value">{{ inspectingAppDetails.legalCopyright }}</span>
            </div>
          </div>
        </div>

        <!-- 流量统计 -->
        <div class="detail-section">
          <h4 class="detail-section-title">流量概览</h4>
          <TrafficChart :data="trafficHistory" :max-data-points="60" />
        </div>
      </div>

      <!-- 加载或空状态 -->
      <div v-else class="empty-state">
        <div v-if="selectedApp" class="empty-icon">⏳</div>
        <div v-else class="empty-icon">📊</div>
        <div class="empty-text">
          {{ selectedApp ? '正在加载应用详情...' : '从左侧选择一个应用以查看详情' }}
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, onUnmounted, watch } from 'vue'
import { storeToRefs } from 'pinia'
import TrafficChart from './components/TrafficChart.vue'
import { useApplicationStore } from '@/stores/application'
import { useWebSocketStore } from '@/stores/websocketStore'
import { useTrafficStore } from '@/stores/trafficStore'
import { httpClient } from '@/utils/http'
import { SubscriptionInfo } from '@/types/response'
import { convertFileSize } from '@/utils/fileUtil'
import { FILE_SIZE_UNIT_ENUM } from '@/constants/enums'

// Props
defineProps<{
  width: number
}>()

// Store
const applicationStore = useApplicationStore()
const webSocketStore = useWebSocketStore()
const trafficStore = useTrafficStore()
const { selectedApp, inspectingAppDetails, isInspecting } = storeToRefs(applicationStore)
const { isOpen } = storeToRefs(webSocketStore)
const { trafficHistory } = storeToRefs(trafficStore)

// 订阅应用详情
const subscribeToAppDetails = (appPath: string) => {
  if (!isOpen.value) return
  console.log(`Subscribing to app details for: ${appPath}`)
  const subInfo = {
    subscriptionType: 'AppDetailInfoSubscribe',
    interval: 1000,
    applicationPath: appPath,
  }
  httpClient.post('/subscribe-appinfo', JSON.stringify(subInfo)).catch((err) => {
    console.error('Failed to subscribe to app details:', err)
  })
}

// 取消订阅应用详情
const unsubscribeFromAppDetails = () => {
  if (!isOpen.value) return
  console.log('Unsubscribing from app details')
  const subInfo: SubscriptionInfo = {
    subscriptionType: 'AppDetailInfoSubscribe',
    interval: 0,
  }
  httpClient.post('/unsubscribe', JSON.stringify(subInfo)).catch((err) => {
    console.error('Failed to unsubscribe from app details:', err)
  })
}

// 监听选中的应用变化
watch(
  selectedApp,
  (newApp, oldApp) => {
    // 清空旧的详情
    applicationStore.setInspectingAppDetails(null)

    if (newApp) {
      // 订阅新的应用详情
      if (newApp.mainModulePath) {
        subscribeToAppDetails(newApp.mainModulePath)
      }
    } else if (oldApp) {
      // 如果没有新应用选中（例如列表清空），取消订阅
      unsubscribeFromAppDetails()
    }
  },
  { immediate: true },
)

onMounted(() => {
  // 组件挂载时，如果已有选中的应用，则触发一次订阅
  if (selectedApp.value && selectedApp.value.mainModulePath) {
    subscribeToAppDetails(selectedApp.value.mainModulePath)
  }
})

onUnmounted(() => {
  // 组件卸载时取消订阅
  unsubscribeFromAppDetails()
  // 清空详情
  applicationStore.setInspectingAppDetails(null)
})

// 格式化内存显示
const formatMemory = (memoryInBytes: number): string => {
  if (!memoryInBytes) return '0 B'
  const result = convertFileSize(memoryInBytes, FILE_SIZE_UNIT_ENUM.B)
  return result.size + result.unit
}
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
  margin-bottom: 28px;
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

/* 应用头部 */
.app-header {
  display: flex;
  align-items: center;
  gap: 16px;
  margin-bottom: 16px;
}

.app-icon {
  width: 48px;
  height: 48px;
  border-radius: 10px;
  background: var(--bg-tertiary);
  flex-shrink: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  overflow: hidden;
}

.app-icon img {
  width: 100%;
  height: 100%;
  object-fit: contain;
}

.app-icon-placeholder {
  font-size: 24px;
  font-weight: bold;
  color: var(--text-muted);
}

.app-title-group {
  min-width: 0;
}

.app-title {
  font-size: 18px;
  font-weight: 700;
  color: var(--text-primary);
  margin: 0;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.app-subtitle {
  font-size: 12px;
  color: var(--text-muted);
  margin: 4px 0 0 0;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
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
  min-width: 0;
}

.detail-item.full-width {
  grid-column: 1 / -1;
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
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.detail-value.code {
  font-family: 'JetBrains Mono', 'Fira Code', monospace;
  background: var(--bg-card);
  padding: 4px 8px;
  border-radius: 4px;
  border: 1px solid var(--border-tertiary);
  word-break: break-all;
  white-space: normal;
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

/* 空状态 */
.empty-state {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 48px 24px;
  color: var(--text-muted);
  text-align: center;
}

.empty-icon {
  font-size: 48px;
  margin-bottom: 16px;
  opacity: 0.5;
  animation: pulse 2s infinite ease-in-out;
}

@keyframes pulse {
  0%,
  100% {
    transform: scale(1);
    opacity: 0.5;
  }
  50% {
    transform: scale(1.1);
    opacity: 0.7;
  }
}

.empty-text {
  font-size: 14px;
  max-width: 200px;
}

/* 响应式 */
@media (max-width: 1200px) {
  .detail-grid {
    grid-template-columns: 1fr;
  }
}
</style>
