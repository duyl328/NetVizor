<template>
  <div class="firewall-view">
    <div class="firewall-container">
      <!-- 顶部工具栏 -->
      <div class="firewall-toolbar">
        <div class="toolbar-left">
          <h2 class="view-title">防火墙规则管理</h2>
          <p class="view-subtitle">配置和管理网络防护规则</p>
        </div>
        <div class="toolbar-right">
          <n-button type="primary" size="medium">
            <template #icon>
              <n-icon :component="AddOutline" />
            </template>
            添加规则
          </n-button>
          <n-button size="medium" ghost>导入规则</n-button>
          <n-button size="medium" ghost>导出规则</n-button>
        </div>
      </div>

      <!-- 规则统计卡片 -->
      <div class="stats-grid">
        <div class="stat-card">
          <div class="stat-icon-wrapper success">
            <n-icon :component="CheckmarkCircleOutline" size="24" />
          </div>
          <div class="stat-content">
            <div class="stat-number">{{ stats.active }}</div>
            <div class="stat-label">启用规则</div>
          </div>
        </div>

        <div class="stat-card">
          <div class="stat-icon-wrapper warning">
            <n-icon :component="PauseCircleOutline" size="24" />
          </div>
          <div class="stat-content">
            <div class="stat-number">{{ stats.inactive }}</div>
            <div class="stat-label">禁用规则</div>
          </div>
        </div>

        <div class="stat-card">
          <div class="stat-icon-wrapper error">
            <n-icon :component="CloseCircleOutline" size="24" />
          </div>
          <div class="stat-content">
            <div class="stat-number">{{ stats.blocked }}</div>
            <div class="stat-label">已拦截</div>
          </div>
        </div>

        <div class="stat-card">
          <div class="stat-icon-wrapper info">
            <n-icon :component="ShieldCheckmarkOutline" size="24" />
          </div>
          <div class="stat-content">
            <div class="stat-number">{{ stats.total }}</div>
            <div class="stat-label">总规则数</div>
          </div>
        </div>
      </div>

      <!-- 规则列表 -->
      <div class="rules-panel">
        <div class="panel-header">
          <h3 class="panel-title">防火墙规则列表</h3>
          <div class="panel-controls">
            <n-input
              v-model:value="searchQuery"
              placeholder="搜索规则..."
              size="small"
              clearable
            >
              <template #prefix>
                <n-icon :component="SearchOutline" />
              </template>
            </n-input>
            <n-select
              v-model:value="filterType"
              size="small"
              style="width: 120px"
              :options="filterOptions"
            />
          </div>
        </div>

        <div class="rules-list">
          <div v-for="rule in mockRules" :key="rule.id" class="rule-item">
            <div class="rule-status">
              <n-switch v-model:value="rule.enabled" size="small" />
            </div>

            <div class="rule-main">
              <div class="rule-header">
                <h4 class="rule-name">{{ rule.name }}</h4>
                <n-tag :type="rule.action === 'allow' ? 'success' : 'error'" size="small">
                  {{ rule.action === 'allow' ? '允许' : '拒绝' }}
                </n-tag>
              </div>
              <div class="rule-details">
                <span class="rule-detail">
                  <n-icon :component="LocationOutline" size="14" />
                  {{ rule.source }}
                </span>
                <span class="rule-arrow">→</span>
                <span class="rule-detail">
                  <n-icon :component="LocationOutline" size="14" />
                  {{ rule.destination }}
                </span>
                <span class="rule-detail">
                  <n-icon :component="LayersOutline" size="14" />
                  {{ rule.protocol }}
                </span>
                <span class="rule-detail">
                  <n-icon :component="ServerOutline" size="14" />
                  端口: {{ rule.port }}
                </span>
              </div>
            </div>

            <div class="rule-actions">
              <n-button size="tiny" circle quaternary>
                <template #icon>
                  <n-icon :component="CreateOutline" />
                </template>
              </n-button>
              <n-button size="tiny" circle quaternary>
                <template #icon>
                  <n-icon :component="CopyOutline" />
                </template>
              </n-button>
              <n-button size="tiny" circle quaternary type="error">
                <template #icon>
                  <n-icon :component="TrashOutline" />
                </template>
              </n-button>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { NButton, NIcon, NInput, NSelect, NSwitch, NTag } from 'naive-ui'
import {
  AddOutline,
  SearchOutline,
  CheckmarkCircleOutline,
  PauseCircleOutline,
  CloseCircleOutline,
  ShieldCheckmarkOutline,
  LocationOutline,
  LayersOutline,
  ServerOutline,
  CreateOutline,
  CopyOutline,
  TrashOutline,
} from '@vicons/ionicons5'

// 搜索和过滤
const searchQuery = ref('')
const filterType = ref('all')

const filterOptions = [
  { label: '全部规则', value: 'all' },
  { label: '已启用', value: 'active' },
  { label: '已禁用', value: 'inactive' },
  { label: '入站规则', value: 'inbound' },
  { label: '出站规则', value: 'outbound' },
]

// 统计数据
const stats = ref({
  active: 128,
  inactive: 24,
  blocked: 3456,
  total: 152,
})

// 模拟规则数据
const mockRules = ref([
  {
    id: 1,
    name: '阻止恶意IP访问',
    enabled: true,
    action: 'deny',
    source: '10.0.0.0/8',
    destination: '任意',
    protocol: 'TCP/UDP',
    port: '全部',
    priority: 100,
  },
  {
    id: 2,
    name: '允许HTTPS流量',
    enabled: true,
    action: 'allow',
    source: '任意',
    destination: '任意',
    protocol: 'TCP',
    port: '443',
    priority: 200,
  },
  {
    id: 3,
    name: '允许DNS查询',
    enabled: true,
    action: 'allow',
    source: '192.168.1.0/24',
    destination: '8.8.8.8',
    protocol: 'UDP',
    port: '53',
    priority: 300,
  },
  {
    id: 4,
    name: '阻止Telnet访问',
    enabled: false,
    action: 'deny',
    source: '任意',
    destination: '任意',
    protocol: 'TCP',
    port: '23',
    priority: 400,
  },
  {
    id: 5,
    name: '允许SSH管理',
    enabled: true,
    action: 'allow',
    source: '192.168.1.100',
    destination: '192.168.1.1',
    protocol: 'TCP',
    port: '22',
    priority: 500,
  },
])
</script>

<style scoped>
.firewall-view {
  height: 100%;
  overflow-y: auto;
  background: var(--bg-secondary);
}

.firewall-container {
  padding: 24px;
  max-width: 1400px;
  margin: 0 auto;
}

/* 工具栏 */
.firewall-toolbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 24px;
}

.toolbar-left {
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

.toolbar-right {
  display: flex;
  gap: 12px;
}

/* 统计卡片 */
.stats-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
  gap: 16px;
  margin-bottom: 24px;
}

.stat-card {
  background: var(--bg-card);
  backdrop-filter: var(--backdrop-blur);
  border: 1px solid var(--border-primary);
  border-radius: 12px;
  padding: 20px;
  display: flex;
  align-items: center;
  gap: 16px;
  transition: var(--transition);
}

.stat-card:hover {
  transform: translateY(-2px);
  box-shadow: var(--shadow-lg);
}

.stat-icon-wrapper {
  width: 48px;
  height: 48px;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
}

.stat-icon-wrapper.success {
  background: rgba(34, 197, 94, 0.1);
  color: var(--accent-success);
}

.stat-icon-wrapper.warning {
  background: rgba(245, 158, 11, 0.1);
  color: var(--accent-warning);
}

.stat-icon-wrapper.error {
  background: rgba(239, 68, 68, 0.1);
  color: var(--accent-error);
}

.stat-icon-wrapper.info {
  background: rgba(59, 130, 246, 0.1);
  color: var(--accent-primary);
}

.stat-content {
  flex: 1;
}

.stat-number {
  font-size: 28px;
  font-weight: 700;
  color: var(--text-primary);
  line-height: 1;
}

.stat-label {
  font-size: 14px;
  color: var(--text-muted);
  margin-top: 4px;
}

/* 规则面板 */
.rules-panel {
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

.panel-controls {
  display: flex;
  gap: 12px;
}

/* 规则列表 */
.rules-list {
  padding: 12px;
}

.rule-item {
  background: var(--bg-tertiary);
  border: 1px solid var(--border-secondary);
  border-radius: 8px;
  padding: 16px;
  margin-bottom: 8px;
  display: flex;
  align-items: center;
  gap: 16px;
  transition: var(--transition);
}

.rule-item:hover {
  background: var(--bg-hover);
  border-color: var(--border-hover);
}

.rule-item:last-child {
  margin-bottom: 0;
}

.rule-status {
  display: flex;
  align-items: center;
}

.rule-main {
  flex: 1;
}

.rule-header {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 8px;
}

.rule-name {
  font-size: 14px;
  font-weight: 600;
  color: var(--text-primary);
  margin: 0;
}

.rule-details {
  display: flex;
  align-items: center;
  gap: 12px;
  font-size: 12px;
  color: var(--text-muted);
}

.rule-detail {
  display: flex;
  align-items: center;
  gap: 4px;
}

.rule-arrow {
  color: var(--text-quaternary);
}

.rule-actions {
  display: flex;
  gap: 8px;
}

/* 响应式 */
@media (max-width: 768px) {
  .firewall-toolbar {
    flex-direction: column;
    align-items: flex-start;
    gap: 16px;
  }

  .toolbar-right {
    width: 100%;
    flex-wrap: wrap;
  }

  .stats-grid {
    grid-template-columns: 1fr 1fr;
  }

  .panel-header {
    flex-direction: column;
    align-items: flex-start;
    gap: 12px;
  }

  .panel-controls {
    width: 100%;
  }

  .rule-item {
    flex-direction: column;
    align-items: flex-start;
  }

  .rule-details {
    flex-wrap: wrap;
  }
}

/* Naive UI 样式覆盖 */
:deep(.n-input) {
  --n-border: 1px solid var(--border-tertiary);
  --n-border-hover: 1px solid var(--border-hover);
  --n-border-focus: 1px solid var(--accent-primary);
  --n-color: var(--bg-card);
  --n-text-color: var(--text-primary);
}

:deep(.n-select) {
  --n-border: 1px solid var(--border-tertiary);
  --n-border-hover: 1px solid var(--border-hover);
  --n-border-focus: 1px solid var(--accent-primary);
  --n-color: var(--bg-card);
  --n-text-color: var(--text-primary);
}

:deep(.n-switch) {
  --n-rail-color-active: var(--accent-success);
}

:deep(.n-tag) {
  font-weight: 500;
}
</style>
