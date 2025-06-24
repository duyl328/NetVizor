<template>
  <div class="connections-area">
    <div class="connections-header">
      <h3 class="connections-title">
        <span class="title-icon">📊</span>
        连接列表
        <span class="connection-count">{{ connections.length }} 个活跃连接</span>
      </h3>
      <div class="connections-controls">
        <n-button
          size="small"
          :type="isMonitoring ? 'warning' : 'info'"
          ghost
          @click="$emit('toggleMonitoring')"
        >
          {{ isMonitoring ? '暂停' : '继续' }}监控
        </n-button>
      </div>
    </div>

    <div class="connections-content">
      <div class="table-container">
        <div class="table-header">
          <div class="table-column" @click="handleSort('process')">
            进程
            <span v-if="sortColumn === 'process'" class="sort-icon">
              {{ sortOrder === 'asc' ? '↑' : '↓' }}
            </span>
          </div>
          <div class="table-column" @click="handleSort('localAddress')">
            本地地址
            <span v-if="sortColumn === 'localAddress'" class="sort-icon">
              {{ sortOrder === 'asc' ? '↑' : '↓' }}
            </span>
          </div>
          <div class="table-column" @click="handleSort('remoteAddress')">
            远程地址
            <span v-if="sortColumn === 'remoteAddress'" class="sort-icon">
              {{ sortOrder === 'asc' ? '↑' : '↓' }}
            </span>
          </div>
          <div class="table-column" @click="handleSort('status')">
            状态
            <span v-if="sortColumn === 'status'" class="sort-icon">
              {{ sortOrder === 'asc' ? '↑' : '↓' }}
            </span>
          </div>
          <div class="table-column" @click="handleSort('traffic')">
            流量
            <span v-if="sortColumn === 'traffic'" class="sort-icon">
              {{ sortOrder === 'asc' ? '↑' : '↓' }}
            </span>
          </div>
        </div>

        <div class="table-body scrollbar-success scrollbar-glow">
          <div
            v-for="connection in sortedConnections"
            :key="connection.id"
            class="table-row"
            :class="{ 'row-selected': selectedId === connection.id }"
            @click="handleSelect(connection)"
          >
            <div class="table-cell">
              <div class="process-info">
                <div class="process-icon">{{ getProcessIcon(connection.process) }}</div>
                <span>{{ connection.process }}</span>
              </div>
            </div>
            <div class="table-cell">{{ connection.localAddress }}</div>
            <div class="table-cell">{{ connection.remoteAddress }}</div>
            <div class="table-cell">
              <span class="status-badge" :class="`status-${connection.status}`">
                {{ getStatusText(connection.status) }}
              </span>
            </div>
            <div class="table-cell">
              <span class="traffic-info">{{ connection.traffic }}</span>
            </div>
          </div>

          <!-- 空状态 -->
          <div v-if="connections.length === 0" class="empty-table">
            <div class="empty-icon">🔍</div>
            <div class="empty-text">暂无连接数据</div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { NButton } from 'naive-ui'

// Props
const props = defineProps<{
  connections: Array<{
    id: number
    process: string
    localAddress: string
    remoteAddress: string
    status: string
    traffic: string
  }>
  isMonitoring: boolean
}>()

// Emits
const emit = defineEmits<{
  select: [connection: any]
  toggleMonitoring: []
}>()

// Local state
const selectedId = ref<number | null>(null)
const sortColumn = ref<string>('')
const sortOrder = ref<'asc' | 'desc'>('asc')

// 排序后的连接列表
const sortedConnections = computed(() => {
  if (!sortColumn.value) return props.connections

  return [...props.connections].sort((a, b) => {
    const aVal = a[sortColumn.value as keyof typeof a]
    const bVal = b[sortColumn.value as keyof typeof b]

    if (sortOrder.value === 'asc') {
      return aVal > bVal ? 1 : -1
    } else {
      return aVal < bVal ? 1 : -1
    }
  })
})

// 处理排序
const handleSort = (column: string) => {
  if (sortColumn.value === column) {
    sortOrder.value = sortOrder.value === 'asc' ? 'desc' : 'asc'
  } else {
    sortColumn.value = column
    sortOrder.value = 'asc'
  }
}

// 处理选中
const handleSelect = (connection: any) => {
  selectedId.value = connection.id
  emit('select', connection)
}

// 获取进程图标
const getProcessIcon = (process: string) => {
  const iconMap: Record<string, string> = {
    'chrome.exe': '🌐',
    'firefox.exe': '🦊',
    'edge.exe': '🌊',
    'node.exe': '💚',
    'python.exe': '🐍',
    default: '📦'
  }
  return iconMap[process.toLowerCase()] || iconMap.default
}

// 获取状态文本
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
</script>

<style scoped>
/* 连接区域容器 */
.connections-area {
  flex: 1;
  background: var(--bg-glass);
  overflow: hidden;
  display: flex;
  flex-direction: column;
  min-height: 0;
}

/* 头部 */
.connections-header {
  padding: 16px 24px;
  border-bottom: 1px solid var(--border-secondary);
  display: flex;
  align-items: center;
  justify-content: space-between;
  background: var(--bg-card);
  flex-shrink: 0;
}

.connections-title {
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

.connection-count {
  font-size: 12px;
  color: var(--text-muted);
  font-weight: 400;
  margin-left: 8px;
}

/* 内容区域 */
.connections-content {
  flex: 1;
  overflow: hidden;
  padding: 0;
  min-height: 0;
}

.table-container {
  height: 100%;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

/* 表头 */
.table-header {
  display: flex;
  padding: 12px 24px;
  background: var(--bg-card);
  border-bottom: 1px solid var(--border-tertiary);
  font-size: 12px;
  font-weight: 600;
  color: var(--text-quaternary);
  text-transform: uppercase;
  letter-spacing: 0.5px;
  flex-shrink: 0;
}

.table-column {
  flex: 1;
  padding: 0 8px;
  cursor: pointer;
  user-select: none;
  display: flex;
  align-items: center;
  gap: 4px;
  transition: color 0.2s;
}

.table-column:hover {
  color: var(--text-secondary);
}

.table-column:first-child {
  flex: 1.5;
}

.sort-icon {
  font-size: 10px;
  opacity: 0.7;
}

/* 表体 */
.table-body {
  flex: 1;
  overflow-y: auto;
  min-height: 0;
}

.table-row {
  display: flex;
  padding: 12px 24px;
  border-bottom: 1px solid var(--border-secondary);
  transition: var(--transition);
  cursor: pointer;
}

.table-row:hover {
  background: var(--bg-hover);
}

.table-row.row-selected {
  background: var(--bg-selected);
  border-color: var(--border-hover);
}

.table-cell {
  flex: 1;
  padding: 0 8px;
  display: flex;
  align-items: center;
  font-size: 13px;
  color: var(--text-tertiary);
}

.table-cell:first-child {
  flex: 1.5;
}

/* 进程信息 */
.process-info {
  display: flex;
  align-items: center;
  gap: 8px;
}

.process-icon {
  font-size: 14px;
}

/* 状态徽章 */
.status-badge {
  padding: 2px 8px;
  border-radius: 12px;
  font-size: 11px;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.status-badge.status-established {
  background: rgba(34, 197, 94, 0.2);
  color: var(--accent-success);
  border: 1px solid rgba(34, 197, 94, 0.3);
}

.status-badge.status-listening {
  background: rgba(59, 130, 246, 0.2);
  color: var(--accent-primary);
  border: 1px solid rgba(59, 130, 246, 0.3);
}

.status-badge.status-close_wait,
.status-badge.status-time_wait {
  background: rgba(251, 146, 60, 0.2);
  color: var(--accent-warning);
  border: 1px solid rgba(251, 146, 60, 0.3);
}

.status-badge.status-closed {
  background: rgba(148, 163, 184, 0.2);
  color: var(--text-muted);
  border: 1px solid rgba(148, 163, 184, 0.3);
}

/* 流量信息 */
.traffic-info {
  color: var(--accent-secondary);
  font-weight: 500;
}

/* 空状态 */
.empty-table {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 64px 24px;
  color: var(--text-muted);
}

.empty-icon {
  font-size: 48px;
  margin-bottom: 16px;
  opacity: 0.5;
}

.empty-text {
  font-size: 14px;
}
</style>
