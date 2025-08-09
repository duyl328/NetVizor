<template>
  <div class="port-stats-table">
    <div class="table-container">
      <table class="stats-table">
        <thead>
          <tr>
            <th>端口</th>
            <th>协议</th>
            <th>连接数</th>
            <th>流量</th>
            <th>远程主机</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="(stat, index) in data" :key="`${stat.port}-${stat.protocol}`">
            <td>
              <span class="port-number">{{ stat.port }}</span>
            </td>
            <td>
              <span class="protocol-badge" :class="stat.protocol.toLowerCase()">
                {{ stat.protocol }}
              </span>
            </td>
            <td>
              <span class="connection-count">{{ stat.connectionCount }}</span>
            </td>
            <td>
              <span class="traffic-size">{{ formatBytes(stat.totalBytes) }}</span>
            </td>
            <td>
              <div class="remote-hosts">
                <span
                  v-for="(host, hostIndex) in stat.remoteHosts.slice(0, 3)"
                  :key="hostIndex"
                  class="host-badge"
                  :title="host"
                >
                  {{ truncateHost(host) }}
                </span>
                <span
                  v-if="stat.remoteHosts.length > 3"
                  class="host-more"
                  :title="stat.remoteHosts.slice(3).join(', ')"
                >
                  +{{ stat.remoteHosts.length - 3 }}
                </span>
              </div>
            </td>
          </tr>
        </tbody>
      </table>

      <!-- 空状态 -->
      <div v-if="data.length === 0" class="empty-state">
        <n-icon :component="GlobeOutline" size="32" />
        <p>暂无端口统计数据</p>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { NIcon } from 'naive-ui'
import { GlobeOutline } from '@vicons/ionicons5'

// 接口定义
interface PortStat {
  port: number
  protocol: string
  connectionCount: number
  totalBytes: number
  remoteHosts: string[]
}

// Props定义
const props = defineProps<{
  data: PortStat[]
}>()

// 格式化字节数
const formatBytes = (bytes: number): string => {
  if (bytes === 0) return '0 B'

  const k = 1024
  const sizes = ['B', 'KB', 'MB', 'GB', 'TB']
  const i = Math.floor(Math.log(bytes) / Math.log(k))

  return `${parseFloat((bytes / Math.pow(k, i)).toFixed(1))} ${sizes[i]}`
}

// 截断主机名
const truncateHost = (host: string): string => {
  if (host.length <= 16) return host
  return host.substring(0, 16) + '...'
}
</script>

<style scoped>
.port-stats-table {
  height: 100%;
  overflow: hidden;
}

.table-container {
  height: 100%;
  overflow-y: auto;
}

.stats-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 12px;
}

.stats-table th {
  background: var(--bg-tertiary);
  color: var(--text-secondary);
  font-weight: 600;
  padding: 8px 12px;
  text-align: left;
  border-bottom: 1px solid var(--border-secondary);
  position: sticky;
  top: 0;
  z-index: 1;
}

.stats-table td {
  padding: 8px 12px;
  border-bottom: 1px solid var(--border-tertiary);
  vertical-align: middle;
}

.stats-table tr:hover {
  background: var(--bg-hover);
}

.port-number {
  font-family: 'JetBrains Mono', 'Fira Code', monospace;
  font-weight: 600;
  color: var(--accent-primary);
}

.protocol-badge {
  display: inline-block;
  padding: 2px 6px;
  border-radius: 4px;
  font-size: 10px;
  font-weight: 600;
  text-transform: uppercase;
}

.protocol-badge.tcp {
  background: rgba(16, 185, 129, 0.1);
  color: #10b981;
}

.protocol-badge.udp {
  background: rgba(59, 130, 246, 0.1);
  color: #3b82f6;
}

.connection-count {
  font-weight: 600;
  color: var(--text-primary);
}

.traffic-size {
  font-family: 'JetBrains Mono', 'Fira Code', monospace;
  font-weight: 500;
  color: var(--text-primary);
}

.remote-hosts {
  display: flex;
  flex-wrap: wrap;
  gap: 4px;
  max-width: 200px;
}

.host-badge {
  display: inline-block;
  padding: 2px 6px;
  background: var(--bg-tertiary);
  border-radius: 3px;
  font-size: 10px;
  color: var(--text-secondary);
  white-space: nowrap;
  font-family: 'JetBrains Mono', 'Fira Code', monospace;
}

.host-more {
  display: inline-block;
  padding: 2px 6px;
  background: var(--accent-primary);
  color: white;
  border-radius: 3px;
  font-size: 10px;
  font-weight: 600;
  cursor: help;
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
