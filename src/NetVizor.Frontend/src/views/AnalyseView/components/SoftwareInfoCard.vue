<template>
  <div class="software-info-card">
    <div class="info-grid">
      <div class="info-item">
        <label>进程名称</label>
        <span class="process-name">{{ softwareInfo?.processName || '-' }}</span>
      </div>
      
      <div class="info-item">
        <label>显示名称</label>
        <span>{{ softwareInfo?.displayName || '-' }}</span>
      </div>
      
      <div class="info-item">
        <label>版本</label>
        <span>{{ softwareInfo?.version || '-' }}</span>
      </div>
      
      <div class="info-item">
        <label>公司</label>
        <span>{{ softwareInfo?.company || '-' }}</span>
      </div>
      
      <div class="info-item full-width">
        <label>路径</label>
        <span class="file-path" :title="softwareInfo?.processPath">
          {{ softwareInfo?.processPath || '-' }}
        </span>
      </div>
      
      <div class="info-item">
        <label>文件大小</label>
        <span>{{ formatBytes(softwareInfo?.fileSize || 0) }}</span>
      </div>
      
      <div class="info-item">
        <label>创建时间</label>
        <span>{{ formatDate(softwareInfo?.createdTime) }}</span>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
// Props定义
interface SoftwareInfo {
  processName: string
  displayName: string
  version?: string
  company?: string
  processPath?: string
  fileSize?: number
  createdTime?: string
  modifiedTime?: string
}

const props = defineProps<{
  softwareInfo: SoftwareInfo | null
}>()

// 格式化字节数
const formatBytes = (bytes: number): string => {
  if (bytes === 0) return '0 B'
  
  const k = 1024
  const sizes = ['B', 'KB', 'MB', 'GB', 'TB']
  const i = Math.floor(Math.log(bytes) / Math.log(k))
  
  return `${parseFloat((bytes / Math.pow(k, i)).toFixed(1))} ${sizes[i]}`
}

// 格式化日期
const formatDate = (dateString?: string): string => {
  if (!dateString) return '-'
  
  const date = new Date(dateString)
  return date.toLocaleString('zh-CN', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit'
  })
}
</script>

<style scoped>
.software-info-card {
  background: var(--bg-tertiary);
  border-radius: 8px;
  padding: 16px;
  border: 1px solid var(--border-secondary);
}

.info-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 12px 16px;
}

.info-item {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.info-item.full-width {
  grid-column: span 2;
}

.info-item label {
  font-size: 11px;
  font-weight: 600;
  color: var(--text-muted);
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.info-item span {
  font-size: 13px;
  color: var(--text-primary);
  word-break: break-word;
}

.process-name {
  font-family: 'JetBrains Mono', 'Fira Code', monospace;
  font-weight: 500;
  color: var(--accent-primary);
}

.file-path {
  font-family: 'JetBrains Mono', 'Fira Code', monospace;
  font-size: 12px;
  color: var(--text-secondary);
  word-break: break-all;
  line-height: 1.4;
}
</style>