<template>
  <div class="sidebar scrollbar-primary scrollbar-thin" :style="{ width: width + 'px' }">
    <div class="sidebar-content">
      <div class="sidebar-header">
        <h3 class="sidebar-title">系统概览</h3>
        <div class="sidebar-badge">{{ data.processCount }}</div>
      </div>

      <div class="sidebar-stats">
        <div class="stat-card">
          <div class="stat-icon">🔗</div>
          <div class="stat-info">
            <div class="stat-value">{{ data.activeConnections }}</div>
            <div class="stat-label">活跃连接</div>
          </div>
        </div>

        <div class="stat-card">
          <div class="stat-icon">⚡</div>
          <div class="stat-info">
            <div class="stat-value">{{ data.networkSpeed }}</div>
            <div class="stat-label">网络速度</div>
          </div>
        </div>

        <div class="stat-card">
          <div class="stat-icon">🛡️</div>
          <div class="stat-info">
            <div class="stat-value">{{ data.ruleCount }}</div>
            <div class="stat-label">防护规则</div>
          </div>
        </div>
      </div>

      <div class="sidebar-section">
        <h4 class="section-title">快速操作</h4>
        <div class="quick-actions">
          <div class="action-item" @click="handleAction('scan')">扫描威胁</div>
          <div class="action-item" @click="handleAction('update')">更新规则</div>
          <div class="action-item" @click="handleAction('export')">导出日志</div>
        </div>
      </div>

      <div class="sidebar-section">
        <h4 class="section-title">系统状态</h4>
        <div class="system-status">
          <div class="status-item">
            <span class="status-dot active"></span>
            <span class="status-label">防护已启用</span>
          </div>
          <div class="status-item">
            <span class="status-dot active"></span>
            <span class="status-label">实时监控中</span>
          </div>
          <div class="status-item">
            <span class="status-dot warning"></span>
            <span class="status-label">待处理警告: 3</span>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { defineProps, defineEmits } from 'vue'

// Props
const props = defineProps<{
  width: number
  data: {
    activeConnections: number
    processCount: number
    networkSpeed: string
    ruleCount: number
  }
}>()

// Emits
const emit = defineEmits<{
  action: [type: string]
}>()

// 处理快速操作
const handleAction = (type: string) => {
  emit('action', type)
  console.log(`执行操作: ${type}`)
}
</script>

<style scoped>
/* 侧边栏容器 */
.sidebar {
  background: var(--bg-glass);
  backdrop-filter: var(--backdrop-blur);
  border-right: 1px solid var(--border-primary);
  overflow: hidden;
  display: flex;
  flex-direction: column;
  flex-shrink: 0;
}

.sidebar-content {
  padding: 24px;
  overflow-y: auto;
  flex: 1;
  min-height: 0;
}

/* 头部 */
.sidebar-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 20px;
}

.sidebar-title {
  font-size: 16px;
  font-weight: 600;
  color: var(--text-secondary);
  margin: 0;
}

.sidebar-badge {
  background: linear-gradient(135deg, var(--accent-primary) 0%, #1d4ed8 100%);
  color: white;
  padding: 4px 8px;
  border-radius: 12px;
  font-size: 12px;
  font-weight: 600;
}

/* 统计卡片 */
.sidebar-stats {
  display: flex;
  flex-direction: column;
  gap: 12px;
  margin-bottom: 24px;
}

.stat-card {
  background: var(--bg-card);
  border: 1px solid var(--border-tertiary);
  border-radius: 12px;
  padding: 16px;
  display: flex;
  align-items: center;
  gap: 12px;
  transition: var(--transition);
}

.stat-card:hover {
  background: var(--bg-hover);
  border-color: var(--border-hover);
  transform: translateY(-1px);
}

.stat-icon {
  font-size: 20px;
  width: 32px;
  text-align: center;
}

.stat-info {
  flex: 1;
}

.stat-value {
  font-size: 18px;
  font-weight: 700;
  color: var(--text-secondary);
  line-height: 1;
}

.stat-label {
  font-size: 12px;
  color: var(--text-muted);
  margin-top: 2px;
}

/* 侧边栏区块 */
.sidebar-section {
  margin-top: 24px;
}

.section-title {
  font-size: 14px;
  font-weight: 600;
  color: var(--text-quaternary);
  margin-bottom: 12px;
}

/* 快速操作 */
.quick-actions {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.action-item {
  padding: 8px 12px;
  background: var(--bg-tertiary);
  border-radius: 8px;
  font-size: 13px;
  color: var(--text-quaternary);
  cursor: pointer;
  transition: var(--transition);
}

.action-item:hover {
  background: var(--bg-hover);
  color: var(--text-secondary);
}

/* 系统状态 */
.system-status {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.status-item {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 13px;
  color: var(--text-tertiary);
}

.status-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: var(--text-disabled);
}

.status-dot.active {
  background: var(--accent-success);
  box-shadow: 0 0 0 2px rgba(34, 197, 94, 0.2);
}

.status-dot.warning {
  background: var(--accent-warning);
  box-shadow: 0 0 0 2px rgba(251, 146, 60, 0.2);
}

.status-label {
  flex: 1;
}
</style>
