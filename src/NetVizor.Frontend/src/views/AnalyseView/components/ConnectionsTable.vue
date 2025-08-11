<template>
  <div class="connections-table">
    <div class="table-container">
      <table class="stats-table">
        <thead>
          <tr>
            <th>本地地址</th>
            <th>远程地址</th>
            <th>协议</th>
            <th>连接数</th>
            <th>上传</th>
            <th>下载</th>
            <th>总流量</th>
            <th>持续时间</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="(conn, index) in data" :key="`${conn.localIP}-${conn.localPort}-${conn.remoteIP}-${conn.remotePort}`">
            <td>
              <div class="address-info">
                <span class="ip">{{ conn.localIP }}</span>
                <span class="port">:{{ conn.localPort }}</span>
              </div>
            </td>
            <td>
              <div class="address-info">
                <span class="ip">{{ conn.remoteIP }}</span>
                <span class="port">:{{ conn.remotePort }}</span>
              </div>
            </td>
            <td>
              <span class="protocol-badge" :class="conn.protocol.toLowerCase()">
                {{ conn.protocol }}
              </span>
            </td>
            <td>
              <span class="connection-count">{{ conn.connectionCount }}</span>
            </td>
            <td>
              <span class="traffic-size upload">{{ formatBytes(conn.totalUpload) }}</span>
            </td>
            <td>
              <span class="traffic-size download">{{ formatBytes(conn.totalDownload) }}</span>
            </td>
            <td>
              <span class="traffic-size total">{{ formatBytes(conn.totalTraffic) }}</span>
              <div class="traffic-bar">
                <div
                  class="traffic-fill"
                  :style="{ width: getTrafficPercentage(conn.totalTraffic) + '%' }"
                ></div>
              </div>
            </td>
            <td>
              <span class="duration">{{ formatDuration(conn.firstSeen, conn.lastSeen) }}</span>
            </td>
          </tr>
        </tbody>
      </table>

      <!-- 空状态 -->
      <div v-if="data.length === 0" class="empty-state">
        <n-icon :component="GlobeOutline" size="32" />
        <p>暂无连接数据</p>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { NIcon } from 'naive-ui'
import { GlobeOutline } from '@vicons/ionicons5'

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

// Props定义
const props = defineProps<{
  data: TopConnection[]
}>()

// 计算最大流量用于百分比显示
const maxTraffic = computed(() => {
  if (props.data.length === 0) return 1
  return Math.max(...props.data.map(conn => conn.totalTraffic))
})

// 格式化字节数
const formatBytes = (bytes: number): string => {
  if (bytes === 0) return '0 B'

  const k = 1024
  const sizes = ['B', 'KB', 'MB', 'GB', 'TB']
  const i = Math.floor(Math.log(bytes) / Math.log(k))

  return `${parseFloat((bytes / Math.pow(k, i)).toFixed(1))} ${sizes[i]}`
}

// 获取流量百分比
const getTrafficPercentage = (traffic: number): number => {
  return Math.max(5, (traffic / maxTraffic.value) * 100)
}

// 格式化持续时间
const formatDuration = (firstSeen: string, lastSeen: string): string => {
  const start = new Date(firstSeen).getTime()
  const end = new Date(lastSeen).getTime()
  const duration = Math.abs(end - start)

  if (duration < 1000) {
    return '< 1秒'
  } else if (duration < 60000) {
    return `${Math.floor(duration / 1000)}秒`
  } else if (duration < 3600000) {
    return `${Math.floor(duration / 60000)}分钟`
  } else {
    return `${Math.floor(duration / 3600000)}小时`
  }
}
</script>

<style scoped>
.connections-table {
  height: 100%;
  overflow: hidden;
}

.table-container {
  height: 100%;
  overflow-y: auto;
  padding-bottom: 20px;
}

.stats-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 11px;
}

.stats-table th {
  background: var(--bg-tertiary);
  color: var(--text-secondary);
  font-weight: 600;
  padding: 8px 10px;
  text-align: left;
  border-bottom: 1px solid var(--border-secondary);
  position: sticky;
  top: 0;
  z-index: 1;
  white-space: nowrap;
}

.stats-table td {
  padding: 6px 10px;
  border-bottom: 1px solid var(--border-tertiary);
  vertical-align: middle;
}

.stats-table tr:hover {
  background: var(--bg-hover);
}

.address-info {
  display: flex;
  align-items: center;
  font-family: 'JetBrains Mono', 'Fira Code', monospace;
  font-size: 10px;
}

.ip {
  color: var(--text-primary);
  font-weight: 500;
}

.port {
  color: var(--accent-primary);
  font-weight: 600;
  margin-left: 2px;
}

.protocol-badge {
  display: inline-block;
  padding: 2px 6px;
  border-radius: 4px;
  font-size: 9px;
  font-weight: 600;
  text-transform: uppercase;
}

.protocol-badge.tcp {
  background: rgba(16, 185, 129, 0.15);
  color: #10b981;
  border: 1px solid rgba(16, 185, 129, 0.3);
}

.protocol-badge.udp {
  background: rgba(59, 130, 246, 0.15);
  color: #3b82f6;
  border: 1px solid rgba(59, 130, 246, 0.3);
}

.connection-count {
  font-weight: 600;
  color: var(--text-primary);
}

.traffic-size {
  font-family: 'JetBrains Mono', 'Fira Code', monospace;
  font-weight: 500;
  font-size: 10px;
}

.traffic-size.upload {
  color: #ef4444;
}

.traffic-size.download {
  color: #10b981;
}

.traffic-size.total {
  color: var(--text-primary);
  font-weight: 600;
}

.traffic-bar {
  width: 60px;
  height: 3px;
  background: var(--bg-tertiary);
  border-radius: 2px;
  margin-top: 2px;
  overflow: hidden;
}

.traffic-fill {
  height: 100%;
  background: linear-gradient(90deg, var(--accent-primary), var(--accent-secondary));
  border-radius: 2px;
  transition: width 0.3s ease;
}

.duration {
  font-size: 10px;
  color: var(--text-muted);
  white-space: nowrap;
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

/* 滚动条样式 */
.table-container::-webkit-scrollbar {
  width: 6px;
}

.table-container::-webkit-scrollbar-track {
  background: var(--bg-tertiary);
  border-radius: 3px;
}

.table-container::-webkit-scrollbar-thumb {
  background: var(--border-secondary);
  border-radius: 3px;
}

.table-container::-webkit-scrollbar-thumb:hover {
  background: var(--border-hover);
}
</style>
