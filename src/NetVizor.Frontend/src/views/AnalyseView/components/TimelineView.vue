<template>
  <div class="timeline-view">
    <div class="timeline-container">
      <!-- 时间线 -->
      <div class="timeline" v-if="data.length > 0">
        <div 
          v-for="(item, index) in sortedData" 
          :key="`${item.timestamp}-${index}`"
          class="timeline-item"
          :class="{ 'active': item.connections > averageConnections }"
        >
          <div class="timeline-dot">
            <div class="dot-inner"></div>
          </div>
          
          <div class="timeline-content">
            <div class="timeline-header">
              <h5 class="timeline-time">{{ item.timeStr }}</h5>
              <span class="timeline-connections">{{ item.connections }} 连接</span>
            </div>
            
            <div class="timeline-stats">
              <div class="stat-item upload">
                <span class="stat-label">上传:</span>
                <span class="stat-value">{{ formatBytes(item.upload) }}</span>
              </div>
              <div class="stat-item download">
                <span class="stat-label">下载:</span>
                <span class="stat-value">{{ formatBytes(item.download) }}</span>
              </div>
            </div>
            
            <div class="timeline-bar">
              <div class="bar-container">
                <div 
                  class="bar-fill upload-bar"
                  :style="{ width: getPercentage(item.upload, maxUpload) + '%' }"
                ></div>
                <div 
                  class="bar-fill download-bar"
                  :style="{ width: getPercentage(item.download, maxDownload) + '%' }"
                ></div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- 空状态 -->
      <div v-else class="empty-state">
        <n-icon :component="TimeOutline" size="32" />
        <p>暂无时间线数据</p>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { NIcon } from 'naive-ui'
import { TimeOutline } from '@vicons/ionicons5'

// 接口定义
interface TimeTrend {
  timestamp: number
  timeStr: string
  upload: number
  download: number
  connections: number
}

// Props定义
const props = defineProps<{
  data: TimeTrend[]
}>()

// 计算属性
const sortedData = computed(() => {
  return [...props.data].sort((a, b) => b.timestamp - a.timestamp) // 按时间倒序，最新的在顶部
})

const maxUpload = computed(() => {
  if (props.data.length === 0) return 1
  return Math.max(...props.data.map(item => item.upload))
})

const maxDownload = computed(() => {
  if (props.data.length === 0) return 1
  return Math.max(...props.data.map(item => item.download))
})

const averageConnections = computed(() => {
  if (props.data.length === 0) return 0
  const totalConnections = props.data.reduce((sum, item) => sum + item.connections, 0)
  return totalConnections / props.data.length
})

// 工具函数
const formatBytes = (bytes: number): string => {
  if (bytes === 0) return '0 B'

  const k = 1024
  const sizes = ['B', 'KB', 'MB', 'GB', 'TB']
  const i = Math.floor(Math.log(bytes) / Math.log(k))

  return `${parseFloat((bytes / Math.pow(k, i)).toFixed(1))} ${sizes[i]}`
}

const getPercentage = (value: number, max: number): number => {
  if (max === 0) return 0
  return Math.max(5, (value / max) * 100) // 最小5%确保可见
}
</script>

<style scoped>
.timeline-view {
  height: 100%;
  overflow: hidden;
}

.timeline-container {
  height: 100%;
  overflow-y: auto;
  padding: 16px 20px;
}

.timeline {
  position: relative;
  padding-left: 30px;
}

.timeline::before {
  content: '';
  position: absolute;
  left: 15px;
  top: 0;
  bottom: 0;
  width: 2px;
  background: linear-gradient(to bottom, var(--accent-primary), var(--accent-secondary));
  border-radius: 1px;
}

.timeline-item {
  position: relative;
  margin-bottom: 24px;
  padding-bottom: 16px;
}

.timeline-item:last-child {
  margin-bottom: 0;
}

.timeline-dot {
  position: absolute;
  left: -22px;
  top: 4px;
  width: 16px;
  height: 16px;
  background: var(--bg-card);
  border: 3px solid var(--accent-primary);
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 2;
}

.timeline-item.active .timeline-dot {
  border-color: var(--accent-secondary);
  box-shadow: 0 0 0 4px rgba(59, 130, 246, 0.2);
  animation: pulse 2s infinite;
}

.dot-inner {
  width: 6px;
  height: 6px;
  background: var(--accent-primary);
  border-radius: 50%;
}

.timeline-item.active .dot-inner {
  background: var(--accent-secondary);
}

.timeline-content {
  background: var(--bg-card);
  border: 1px solid var(--border-secondary);
  border-radius: 8px;
  padding: 16px;
  transition: var(--transition);
}

.timeline-item.active .timeline-content {
  border-color: var(--accent-primary);
  box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1);
  background: linear-gradient(135deg, var(--bg-card) 0%, rgba(59, 130, 246, 0.02) 100%);
}

.timeline-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 12px;
}

.timeline-time {
  font-size: 14px;
  font-weight: 600;
  color: var(--text-primary);
  margin: 0;
  font-family: 'JetBrains Mono', 'Fira Code', monospace;
}

.timeline-connections {
  font-size: 12px;
  color: var(--text-muted);
  background: var(--bg-tertiary);
  padding: 2px 8px;
  border-radius: 12px;
  font-weight: 500;
}

.timeline-item.active .timeline-connections {
  background: var(--accent-primary);
  color: white;
}

.timeline-stats {
  display: flex;
  gap: 24px;
  margin-bottom: 12px;
}

.stat-item {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 12px;
}

.stat-label {
  color: var(--text-muted);
  font-weight: 500;
}

.stat-value {
  font-family: 'JetBrains Mono', 'Fira Code', monospace;
  font-weight: 600;
}

.stat-item.upload .stat-value {
  color: #ef4444;
}

.stat-item.download .stat-value {
  color: #10b981;
}

.timeline-bar {
  margin-top: 8px;
}

.bar-container {
  position: relative;
  height: 6px;
  background: var(--bg-tertiary);
  border-radius: 3px;
  overflow: hidden;
}

.bar-fill {
  position: absolute;
  top: 0;
  left: 0;
  height: 100%;
  border-radius: 3px;
  transition: width 0.5s ease;
}

.upload-bar {
  background: linear-gradient(90deg, rgba(239, 68, 68, 0.8), rgba(239, 68, 68, 0.4));
  z-index: 1;
}

.download-bar {
  background: linear-gradient(90deg, rgba(16, 185, 129, 0.8), rgba(16, 185, 129, 0.4));
  top: 50%;
  height: 50%;
  z-index: 2;
}

.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  height: 120px;
  color: var(--text-muted);
  gap: 12px;
}

.empty-state p {
  margin: 0;
  font-size: 13px;
}

/* 动画 */
@keyframes pulse {
  0% {
    box-shadow: 0 0 0 0 rgba(59, 130, 246, 0.4);
  }
  70% {
    box-shadow: 0 0 0 8px rgba(59, 130, 246, 0);
  }
  100% {
    box-shadow: 0 0 0 0 rgba(59, 130, 246, 0);
  }
}

/* 滚动条样式 */
.timeline-container::-webkit-scrollbar {
  width: 6px;
}

.timeline-container::-webkit-scrollbar-track {
  background: var(--bg-tertiary);
  border-radius: 3px;
}

.timeline-container::-webkit-scrollbar-thumb {
  background: var(--border-secondary);
  border-radius: 3px;
}

.timeline-container::-webkit-scrollbar-thumb:hover {
  background: var(--border-hover);
}

/* 响应式设计 */
@media (max-width: 768px) {
  .timeline-stats {
    flex-direction: column;
    gap: 8px;
  }
  
  .timeline-header {
    flex-direction: column;
    align-items: flex-start;
    gap: 8px;
  }
  
  .timeline {
    padding-left: 25px;
  }
  
  .timeline-dot {
    left: -18px;
    width: 12px;
    height: 12px;
  }
  
  .dot-inner {
    width: 4px;
    height: 4px;
  }
  
  .timeline::before {
    left: 12px;
  }
}
</style>