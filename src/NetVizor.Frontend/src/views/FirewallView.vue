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
          <!-- 防火墙总开关 -->
          <div class="firewall-status">
            <span class="status-label">防火墙状态</span>
            <n-switch v-model:value="firewallEnabled" size="medium" />
          </div>
          <n-button type="primary" size="medium" @click="openRuleForm()">
            <template #icon>
              <n-icon :component="AddOutline" />
            </template>
            <span class="button-text">新建规则</span>
          </n-button>
        </div>
      </div>

      <!-- 规则统计卡片 - 响应式隐藏 -->
      <div class="stats-grid stats-hidden-on-small">
        <div class="stat-card">
          <div class="stat-icon-wrapper primary">
            <n-icon :component="ListOutline" size="24" />
          </div>
          <div class="stat-content">
            <div class="stat-number">{{ stats.total }}</div>
            <div class="stat-label">禁用规则</div>
          </div>
        </div>

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
            <div class="stat-label">出站规则</div>
          </div>
        </div>

        <div class="stat-card">
          <div class="stat-icon-wrapper error">
            <n-icon :component="CloseCircleOutline" size="24" />
          </div>
          <div class="stat-content">
            <div class="stat-number">{{ stats.blocked }}</div>
            <div class="stat-label">入站规则</div>
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
              placeholder="搜索规则名称..."
              clearable
              style="width: 280px"
            >
              <template #prefix>
                <n-icon :component="SearchOutline" />
              </template>
            </n-input>
          </div>
        </div>

        <!-- 优化后的筛选区域 -->
        <div class="filter-section">
          <div class="filter-group">
            <span class="filter-label">方向</span>
            <n-select
              v-model:value="filters.direction"
              size="small"
              style="width: 120px"
              placeholder="全部"
              clearable
              :options="directionOptions"
            />
          </div>
          <div class="filter-group">
            <span class="filter-label">操作</span>
            <n-select
              v-model:value="filters.action"
              size="small"
              style="width: 120px"
              placeholder="全部"
              clearable
              :options="actionOptions"
            />
          </div>
          <div class="filter-group">
            <span class="filter-label">状态</span>
            <n-select
              v-model:value="filters.enabled"
              size="small"
              style="width: 120px"
              placeholder="全部"
              clearable
              :options="enabledOptions"
            />
          </div>
          <div class="filter-group">
            <span class="filter-label">协议</span>
            <n-select
              v-model:value="filters.protocol"
              size="small"
              style="width: 120px"
              placeholder="全部"
              clearable
              :options="protocolOptions"
            />
          </div>
          <div class="filter-group">
            <n-button size="small" quaternary @click="clearFilters">
              <template #icon>
                <n-icon :component="RefreshOutline" />
              </template>
              重置筛选
            </n-button>
          </div>
        </div>

        <!-- 虚拟滚动规则列表 -->
        <div class="rules-list-container">
          <!-- 列表头部 -->
          <div class="list-header">
            <div class="header-cell checkbox-cell">
              <n-checkbox
                v-model:checked="allSelected"
                :indeterminate="indeterminate"
                @update:checked="handleSelectAll"
              />
            </div>
            <div class="header-cell name-cell">规则名称</div>
            <div class="header-cell status-cell">状态</div>
            <div class="header-cell direction-cell">方向</div>
            <div class="header-cell action-cell">操作</div>
            <div class="header-cell program-cell">程序/服务</div>
            <div class="header-cell protocol-cell">协议</div>
            <div class="header-cell port-cell">端口</div>
            <div class="header-cell profiles-cell">配置文件</div>
            <div class="header-cell actions-cell">操作</div>
          </div>

          <!-- 虚拟滚动列表 -->
          <RecycleScroller
            class="rules-scroller"
            :items="filteredRules"
            :item-size="80"
            key-field="id"
            v-slot="{ item }"
          >
            <div class="rule-item" :class="{ selected: checkedRowKeys.includes(item.id) }">
              <div class="rule-cell checkbox-cell">
                <n-checkbox
                  :checked="checkedRowKeys.includes(item.id)"
                  @update:checked="(checked) => handleRuleCheck(item.id, checked)"
                />
              </div>

              <div class="rule-cell name-cell">
                <div class="rule-name">{{ item.name }}</div>
                <div class="rule-description">{{ item.description }}</div>
              </div>

              <div class="rule-cell status-cell">
                <n-tag :type="item.enabled ? 'success' : 'default'" size="small">
                  {{ item.enabled ? '启用' : '禁用' }}
                </n-tag>
              </div>

              <div class="rule-cell direction-cell">
                <div class="direction-content">
                  <n-icon
                    :component="item.direction === 'inbound' ? ArrowDownOutline : ArrowUpOutline"
                    size="16"
                  />
                  <span>{{ item.direction === 'inbound' ? '入站' : '出站' }}</span>
                </div>
              </div>

              <div class="rule-cell action-cell">
                <n-tag :type="item.action === 'allow' ? 'success' : 'error'" size="small">
                  {{ item.action === 'allow' ? '允许' : '阻止' }}
                </n-tag>
              </div>

              <div class="rule-cell program-cell">
                <div class="program-text" :title="item.program">{{ item.program }}</div>
              </div>

              <div class="rule-cell protocol-cell">
                <span>{{ item.protocol }}</span>
              </div>

              <div class="rule-cell port-cell">
                <span>{{ item.port }}</span>
              </div>

              <div class="rule-cell profiles-cell">
                <div class="profiles-container">
                  <n-tag
                    v-for="profile in item.profiles"
                    :key="profile"
                    type="info"
                    size="small"
                    class="profile-tag"
                  >
                    {{ profile }}
                  </n-tag>
                </div>
              </div>

              <div class="rule-cell actions-cell">
                <div class="action-buttons">
                  <n-button size="small" quaternary @click="editRule(item)">
                    <template #icon>
                      <n-icon :component="CreateOutline" size="14" />
                    </template>
                  </n-button>
                  <n-button size="small" quaternary type="error" @click="deleteRule(item.id)">
                    <template #icon>
                      <n-icon :component="TrashOutline" size="14" />
                    </template>
                  </n-button>
                </div>
              </div>
            </div>
          </RecycleScroller>
        </div>
      </div>
    </div>

    <!-- 防火墙规则表单 -->
    <FirewallRuleForm v-model="showRuleForm" :edit-rule="currentEditRule" @save="handleSaveRule" />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, h } from 'vue'
import {
  NButton,
  NIcon,
  NInput,
  NSwitch,
  NTag,
  NDropdown,
  NCheckbox,
  NSelect,
  useMessage,
} from 'naive-ui'
import { RecycleScroller } from 'vue-virtual-scroller'
import FirewallRuleForm from '@/components/FirewallRuleForm.vue'
import {
  AddOutline,
  SearchOutline,
  CheckmarkCircleOutline,
  PauseCircleOutline,
  CloseCircleOutline,
  ListOutline,
  ChevronDownOutline,
  CreateOutline,
  TrashOutline,
  ArrowUpOutline,
  ArrowDownOutline,
  RefreshOutline,
} from '@vicons/ionicons5'

const message = useMessage()

// 界面状态
const showRuleForm = ref(false)
const currentEditRule = ref(null)
const firewallEnabled = ref(true)
const checkedRowKeys = ref<string[]>([])

// 搜索和过滤
const searchQuery = ref('')
const filters = ref({
  direction: null as string | null,
  action: null as string | null,
  enabled: null as boolean | null,
  protocol: null as string | null,
})

// 过滤选项
const directionOptions = [
  { label: '入站', value: 'inbound' },
  { label: '出站', value: 'outbound' },
]

const actionOptions = [
  { label: '允许', value: 'allow' },
  { label: '阻止', value: 'block' },
]

const enabledOptions = [
  { label: '已启用', value: true },
  { label: '已禁用', value: false },
]

const protocolOptions = [
  { label: 'TCP', value: 'TCP' },
  { label: 'UDP', value: 'UDP' },
  { label: '任意', value: '任意' },
]

// 批量操作选项
const batchOptions = [
  {
    label: '启用选中',
    key: 'enable',
    icon: () => h(NIcon, { component: CheckmarkCircleOutline }),
  },
  {
    label: '禁用选中',
    key: 'disable',
    icon: () => h(NIcon, { component: PauseCircleOutline }),
  },
  {
    label: '删除选中',
    key: 'delete',
    icon: () => h(NIcon, { component: TrashOutline }),
  },
]

// 统计数据
const stats = ref({
  total: 127,
  active: 89,
  inactive: 38,
  blocked: 1247,
})

// 模拟规则数据
const mockRules = ref([
  {
    id: '1',
    name: 'Windows 远程桌面',
    description: '允许远程桌面连接',
    enabled: true,
    direction: 'inbound',
    action: 'allow',
    program: '系统服务',
    protocol: 'TCP',
    port: '3389',
    profiles: ['域', '专用'],
    priority: 100,
  },
  {
    id: '2',
    name: 'Microsoft Edge',
    description: '允许浏览器访问网络',
    enabled: true,
    direction: 'outbound',
    action: 'allow',
    program: 'C:\\Program Files\\Microsoft\\Edge\\msedge.exe',
    protocol: '任意',
    port: '任意',
    profiles: ['域', '专用', '公用'],
    priority: 200,
  },
  {
    id: '3',
    name: '阻止 Telnet',
    description: '安全策略：阻止 Telnet 连接',
    enabled: true,
    direction: 'inbound',
    action: 'block',
    program: '任意',
    protocol: 'TCP',
    port: '23',
    profiles: ['域', '专用', '公用'],
    priority: 300,
  },
  {
    id: '4',
    name: '文件和打印机共享',
    description: '允许局域网内文件共享',
    enabled: false,
    direction: 'inbound',
    action: 'allow',
    program: '系统服务',
    protocol: 'TCP',
    port: '445, 139',
    profiles: ['专用'],
    priority: 400,
  },
  {
    id: '5',
    name: 'HTTP 服务器',
    description: '允许 Web 服务器接收请求',
    enabled: true,
    direction: 'inbound',
    action: 'allow',
    program: '任意',
    protocol: 'TCP',
    port: '80, 443',
    profiles: ['域', '专用', '公用'],
    priority: 500,
  },
  // 添加更多模拟数据以测试虚拟滚动
  ...Array.from({ length: 50 }, (_, i) => ({
    id: `${i + 6}`,
    name: `规则 ${i + 6}`,
    description: `这是第 ${i + 6} 个测试规则`,
    enabled: Math.random() > 0.5,
    direction: Math.random() > 0.5 ? 'inbound' : 'outbound',
    action: Math.random() > 0.5 ? 'allow' : 'block',
    program: `程序 ${i + 6}`,
    protocol: ['TCP', 'UDP', '任意'][Math.floor(Math.random() * 3)],
    port: Math.floor(Math.random() * 65535).toString(),
    profiles: ['域', '专用', '公用'].slice(0, Math.floor(Math.random() * 3) + 1),
    priority: (i + 6) * 100,
  })),
])

// 计算属性
const filteredRules = computed(() => {
  let rules = mockRules.value

  // 搜索过滤
  if (searchQuery.value) {
    const query = searchQuery.value.toLowerCase()
    rules = rules.filter(
      (rule) =>
        rule.name.toLowerCase().includes(query) || rule.description.toLowerCase().includes(query),
    )
  }

  // 方向过滤
  if (filters.value.direction) {
    rules = rules.filter((rule) => rule.direction === filters.value.direction)
  }

  // 操作过滤
  if (filters.value.action) {
    rules = rules.filter((rule) => rule.action === filters.value.action)
  }

  // 状态过滤
  if (filters.value.enabled !== null) {
    rules = rules.filter((rule) => rule.enabled === filters.value.enabled)
  }

  // 协议过滤
  if (filters.value.protocol) {
    rules = rules.filter((rule) => rule.protocol === filters.value.protocol)
  }

  return rules
})

// 全选相关计算属性
const allSelected = computed({
  get: () => {
    return (
      filteredRules.value.length > 0 && checkedRowKeys.value.length === filteredRules.value.length
    )
  },
  set: (value: boolean) => {
    if (value) {
      checkedRowKeys.value = filteredRules.value.map((rule) => rule.id)
    } else {
      checkedRowKeys.value = []
    }
  },
})

const indeterminate = computed(() => {
  return checkedRowKeys.value.length > 0 && checkedRowKeys.value.length < filteredRules.value.length
})

// 方法
const openRuleForm = (rule: unknown = null) => {
  currentEditRule.value = rule
  showRuleForm.value = true
}

const editRule = (rule: unknown) => {
  currentEditRule.value = { ...rule }
  showRuleForm.value = true
}

const deleteRule = (id: string) => {
  const index = mockRules.value.findIndex((rule) => rule.id === id)
  if (index !== -1) {
    mockRules.value.splice(index, 1)
    // 从选中项中移除
    checkedRowKeys.value = checkedRowKeys.value.filter((key) => key !== id)
    message.success('规则删除成功')
  }
}

const handleRuleCheck = (id: string, checked: boolean) => {
  if (checked) {
    if (!checkedRowKeys.value.includes(id)) {
      checkedRowKeys.value.push(id)
    }
  } else {
    checkedRowKeys.value = checkedRowKeys.value.filter((key) => key !== id)
  }
}

const handleSelectAll = (checked: boolean) => {
  allSelected.value = checked
}

const clearFilters = () => {
  filters.value = {
    direction: null,
    action: null,
    enabled: null,
    protocol: null,
  }
}

const handleSaveRule = (rule: unknown) => {
  if (rule.id) {
    // 编辑现有规则
    const index = mockRules.value.findIndex((r) => r.id === rule.id)
    if (index !== -1) {
      mockRules.value[index] = rule
      message.success('规则更新成功')
    }
  } else {
    // 添加新规则
    rule.id = Date.now().toString()
    mockRules.value.unshift(rule)
    message.success('规则创建成功')
  }

  // 重置状态
  currentEditRule.value = null
}
</script>

<style scoped>
.firewall-view {
  height: 100vh;
  display: flex;
  flex-direction: column;
  background: var(--bg-secondary);
  overflow: hidden;
}

.firewall-container {
  padding: 24px;
  max-width: 2000px;
  margin: 0 auto;
  height: 100%;
  display: flex;
  flex-direction: column;
  min-width: 0; /* 允许容器缩小 */
}

/* 工具栏 */
.firewall-toolbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 8px;
  gap: 16px;
  flex-shrink: 0;
  min-width: 0; /* 允许子元素缩小 */
}

.toolbar-left {
  flex: 1;
  min-width: 0; /* 允许内容缩小 */
}

.view-title {
  font-size: 24px;
  font-weight: 700;
  color: var(--text-primary);
  margin: 0;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.view-subtitle {
  font-size: 14px;
  color: var(--text-muted);
  margin: 4px 0 0 0;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.toolbar-right {
  display: flex;
  align-items: center;
  gap: 12px;
  flex-shrink: 0;
  min-width: 0; /* 允许内容缩小 */
}

.firewall-status {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 8px 16px;
  background: var(--bg-card);
  border: 1px solid var(--border-primary);
  border-radius: 8px;
  white-space: nowrap;
  flex-shrink: 0;
}

.status-label {
  font-size: 14px;
  color: var(--text-secondary);
  white-space: nowrap;
}

.button-text {
  white-space: nowrap;
}

/* 统计卡片 */
.stats-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
  gap: 16px;
  margin-bottom: 8px;
  flex-shrink: 0;
}

.stat-card {
  background: var(--bg-card);
  backdrop-filter: var(--backdrop-blur);
  border: 1px solid var(--border-primary);
  border-radius: 12px;
  padding: 16px;
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
  flex-shrink: 0;
}

.stat-icon-wrapper.primary {
  background: rgba(59, 130, 246, 0.1);
  color: var(--accent-primary);
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

.stat-content {
  flex: 1;
  min-width: 0;
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
  flex: 1;
  display: flex;
  flex-direction: column;
  min-height: 0;
  margin-bottom: 50px;
  min-width: 0; /* 允许面板缩小 */
}

.panel-header {
  padding: 10px 16px;
  border-bottom: 1px solid var(--border-primary);
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-shrink: 0;
  min-width: 0;
}

.panel-title {
  font-size: 18px;
  font-weight: 600;
  color: var(--text-primary);
  margin: 0;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.panel-controls {
  display: flex;
  align-items: center;
  gap: 12px;
  flex-shrink: 0;
}

/* 优化后的筛选区域 */
.filter-section {
  padding: 16px 24px;
  border-bottom: 1px solid var(--border-primary);
  display: flex;
  align-items: center;
  gap: 20px;
  background: var(--bg-tertiary);
  flex-shrink: 0;
  flex-wrap: wrap;
  overflow-x: auto;
}

.filter-group {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-shrink: 0;
}

.filter-label {
  font-size: 14px;
  font-weight: 500;
  color: var(--text-secondary);
  white-space: nowrap;
  min-width: 40px;
}

/* 规则列表容器 */
.rules-list-container {
  flex: 1;
  display: flex;
  flex-direction: column;
  min-height: 0;
  min-width: 0; /* 允许容器缩小 */
  overflow: hidden;
}

/* 列表头部 */
.list-header {
  display: flex;
  align-items: center;
  padding: 0 16px;
  height: 40px;
  background: var(--bg-secondary);
  border-bottom: 1px solid var(--border-primary);
  font-size: 13px;
  font-weight: 600;
  color: var(--text-secondary);
  flex-shrink: 0;
  min-width: 0; /* 允许头部缩小 */
  overflow: hidden;
}

.header-cell {
  display: flex;
  align-items: center;
  padding: 0 8px;
  flex-shrink: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
/* 超超大屏幕 1801px-2000px */
@media (min-width: 1801px) and (max-width: 2000px) {
  .checkbox-cell { width: 60px; }
  .name-cell { width: 250px; }
  .status-cell { width: 100px; }
  .direction-cell { width: 100px; }
  .action-cell { width: 100px; }
  .program-cell { width: 320px; flex-shrink: 1; }
  .protocol-cell { width: 100px; }
  .port-cell { width: 120px; }
  .profiles-cell { width: 180px; flex-shrink: 1; }
  .actions-cell { width: 120px; }
}

/* 极大屏幕 >2000px */
@media (min-width: 2001px) {
  .checkbox-cell { width: 70px; }
  .name-cell { width: 280px; }
  .status-cell { width: 110px; }
  .direction-cell { width: 110px; }
  .action-cell { width: 110px; }
  .program-cell { width: 380px; flex-shrink: 1; }
  .protocol-cell { width: 110px; }
  .port-cell { width: 140px; }
  .profiles-cell { width: 200px; flex-shrink: 1; }
  .actions-cell { width: 140px; }
}

/* 超大屏幕 1601px-1800px */
@media (min-width: 1601px) and (max-width: 1800px) {
  .checkbox-cell { width: 55px; }
  .name-cell { width: 230px; }
  .status-cell { width: 90px; }
  .direction-cell { width: 90px; }
  .action-cell { width: 90px; }
  .program-cell { width: 280px; flex-shrink: 1; }
  .protocol-cell { width: 90px; }
  .port-cell { width: 110px; }
  .profiles-cell { width: 160px; flex-shrink: 1; }
  .actions-cell { width: 110px; }
}

/* 基础列宽 - 针对超大屏幕（1401px-1600px） */
@media (min-width: 1401px) and (max-width: 1600px) {
  .checkbox-cell { width: 50px; }
  .name-cell { width: 200px; }
  .status-cell { width: 80px; }
  .direction-cell { width: 80px; }
  .action-cell { width: 80px; }
  .program-cell { width: 250px; flex-shrink: 1; }
  .protocol-cell { width: 80px; }
  .port-cell { width: 100px; }
  .profiles-cell { width: 150px; flex-shrink: 1; }
  .actions-cell { width: 100px; }
}

/* 大屏幕 1201px-1400px */
@media (min-width: 1201px) and (max-width: 1400px) {
  .checkbox-cell { width: 45px; }
  .name-cell { width: 180px; }
  .status-cell { width: 70px; }
  .direction-cell { width: 70px; }
  .action-cell { width: 70px; }
  .program-cell { width: 220px; flex-shrink: 1; }
  .protocol-cell { width: 70px; }
  .port-cell { width: 90px; }
  .profiles-cell { width: 130px; flex-shrink: 1; }
  .actions-cell { width: 90px; }
}

/* 中等屏幕 1025px-1200px */
@media (min-width: 1025px) and (max-width: 1200px) {
  .checkbox-cell { width: 40px; }
  .name-cell { width: 160px; }
  .status-cell { width: 65px; }
  .direction-cell { width: 65px; }
  .action-cell { width: 65px; }
  .program-cell { width: 200px; flex-shrink: 1; }
  .protocol-cell { width: 65px; }
  .port-cell { width: 80px; }
  .profiles-cell { width: 120px; flex-shrink: 1; }
  .actions-cell { width: 85px; }
}

/* 小屏幕 769px-1024px */
@media (min-width: 769px) and (max-width: 1024px) {
  .checkbox-cell { width: 40px; }
  .name-cell { width: 140px; }
  .status-cell { width: 60px; }
  .direction-cell { width: 60px; }
  .action-cell { width: 60px; }
  .program-cell { width: 180px; flex-shrink: 1; }
  .protocol-cell { width: 60px; }
  .port-cell { width: 80px; }
  .profiles-cell { width: 100px; flex-shrink: 1; }
  .actions-cell { width: 80px; }
}

/* 虚拟滚动列表 */
.rules-scroller {
  flex: 1;
  min-height: 0;
  height: 100%;
  overflow: auto;
}

.rule-item {
  display: flex;
  align-items: center;
  padding: 0 16px;
  height: 80px;
  border-bottom: 1px solid var(--border-secondary);
  transition: var(--transition);
  background: var(--bg-card);
  min-width: 0; /* 允许项目缩小 */
}

.rule-item:hover {
  background: var(--bg-tertiary);
}

.rule-item.selected {
  background: rgba(59, 130, 246, 0.05);
  border-left: 3px solid var(--accent-primary);
}

/* 斑马纹效果 */
.rule-item:nth-child(even) {
  background: rgba(0, 0, 0, 0.02);
}

.rule-item:nth-child(odd) {
  background: var(--bg-card);
}

/* 暗色模式下的斑马纹效果 */
@media (prefers-color-scheme: dark) {
  .rule-item:nth-child(even) {
    background: rgba(255, 255, 255, 0.03);
  }
}

.rule-item:hover {
  background: var(--bg-tertiary) !important;
}

.rule-item.selected {
  background: rgba(59, 130, 246, 0.05) !important;
  border-left: 3px solid var(--accent-primary);
}

.rule-cell {
  display: flex;
  align-items: center;
  padding: 0 8px;
  min-height: 60px;
  font-size: 13px;
  flex-shrink: 0;
  overflow: hidden;
  min-width: 0; /* 允许单元格缩小 */
}

.rule-cell.name-cell {
  flex-direction: column;
  align-items: flex-start;
  justify-content: center;
  gap: 4px;
}

.rule-name {
  font-weight: 600;
  color: var(--text-primary);
  font-size: 14px;
  line-height: 1.2;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  width: 100%;
}

.rule-description {
  font-size: 12px;
  color: var(--text-muted);
  line-height: 1.3;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  width: 100%;
}

.direction-content {
  display: flex;
  align-items: center;
  gap: 6px;
  color: var(--text-secondary);
  min-width: 0;
}

.direction-content span {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.program-text {
  max-width: 100%;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  color: var(--text-primary);
}

.profiles-container {
  display: flex;
  flex-wrap: wrap;
  gap: 4px;
  max-width: 100%;
  overflow: hidden;
}

.profile-tag {
  font-size: 11px;
  padding: 2px 6px;
  flex-shrink: 0;
}

.action-buttons {
  display: flex;
  gap: 6px;
  flex-shrink: 0;
}

/* 响应式设计 */

/* 在小屏幕上隐藏统计卡片 */
@media (max-width: 1024px), (max-height: 700px) {
  .stats-hidden-on-small {
    display: none !important;
  }
}

/* 顶部工具栏响应式 */
@media (max-width: 900px) {
  .firewall-toolbar {
    flex-direction: column;
    align-items: stretch;
    gap: 12px;
  }

  .toolbar-right {
    justify-content: space-between;
  }

  .view-subtitle {
    display: none;
  }
}

@media (max-width: 600px) {
  .status-label {
    display: none;
  }

  .button-text {
    display: none;
  }

  .firewall-status {
    padding: 8px 12px;
  }
}

/* 超小屏幕特殊处理 */
@media (max-width: 768px) {
  .firewall-container {
    padding: 16px;
  }

  .panel-controls {
    display: none;
  }

  .filter-section {
    padding: 12px 16px;
    gap: 12px;
  }

  .filter-group {
    gap: 6px;
  }

  .filter-label {
    font-size: 12px;
    min-width: 35px;
  }

  /* 隐藏部分列 */
  .program-cell,
  .profiles-cell {
    display: none;
  }

  .checkbox-cell { width: 35px; }
  .name-cell { width: 120px; }
  .status-cell { width: 50px; }
  .direction-cell { width: 50px; }
  .action-cell { width: 50px; }
  .protocol-cell { width: 50px; }
  .port-cell { width: 60px; }
  .actions-cell { width: 70px; }
}

/* 高度不足时的优化 */
@media (max-height: 600px) {
  .firewall-container {
    padding: 16px;
  }

  .view-title {
    font-size: 18px;
  }

  .view-subtitle {
    display: none;
  }

  .firewall-toolbar {
    margin-bottom: 4px;
  }

  .rules-panel {
    margin-bottom: 20px;
  }

  .panel-header {
    padding: 8px 16px;
  }

  .filter-section {
    padding: 8px 16px;
  }
}

/* Naive UI 组件样式覆盖 */
:deep(.n-input) {
  --n-border: 1px solid var(--border-tertiary);
  --n-border-hover: 1px solid var(--border-hover);
  --n-border-focus: 1px solid var(--accent-primary);
  --n-color: var(--bg-card);
  --n-text-color: var(--text-primary);
}

:deep(.n-select) {
  --n-color: var(--bg-card);
  --n-border: 1px solid var(--border-tertiary);
  --n-border-hover: 1px solid var(--border-hover);
  --n-border-focus: 1px solid var(--accent-primary);
  --n-text-color: var(--text-primary);
}

:deep(.n-dropdown-menu) {
  --n-color: var(--bg-card);
  --n-option-text-color: var(--text-primary);
}

:deep(.n-switch) {
  --n-rail-color-active: var(--accent-success);
}

:deep(.n-tag) {
  font-weight: 500;
  font-size: 12px;
}

:deep(.n-checkbox) {
  font-size: 14px;
}

/* vue-virtual-scroller 样式 */
:deep(.vue-recycle-scroller) {
  height: 100%;
}

:deep(.vue-recycle-scroller__item-wrapper) {
  overflow: visible;
}
</style>
