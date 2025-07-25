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
            新建规则
          </n-button>
          <n-dropdown :options="batchOptions" placement="bottom-end">
            <n-button size="medium" ghost>
              批量操作
              <template #icon>
                <n-icon :component="ChevronDownOutline" />
              </template>
            </n-button>
          </n-dropdown>
        </div>
      </div>

      <!-- 规则统计卡片 -->
      <div class="stats-grid">
        <div class="stat-card">
          <div class="stat-icon-wrapper primary">
            <n-icon :component="ListOutline" size="24" />
          </div>
          <div class="stat-content">
            <div class="stat-number">{{ stats.total }}</div>
            <div class="stat-label">总规则数</div>
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
            <div class="stat-label">禁用规则</div>
          </div>
        </div>

        <div class="stat-card">
          <div class="stat-icon-wrapper error">
            <n-icon :component="CloseCircleOutline" size="24" />
          </div>
          <div class="stat-content">
            <div class="stat-number">{{ stats.blocked }}</div>
            <div class="stat-label">阻止连接</div>
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
              size="small"
              clearable
              style="width: 280px"
            >
              <template #prefix>
                <n-icon :component="SearchOutline" />
              </template>
            </n-input>
            <n-dropdown :options="filterDropdownOptions" placement="bottom-end">
              <n-button size="small" ghost>
                <template #icon>
                  <n-icon :component="FilterOutline" />
                </template>
                筛选
              </n-button>
            </n-dropdown>
          </div>
        </div>

        <div class="table-container">
          <n-data-table
            :columns="columns"
            :data="filteredRules"
            :pagination="false"
            :scroll-x="1200"
            :virtual-scroll="true"
            :max-height="600"
            size="small"
            striped
            :row-key="(row) => row.id"
            :checked-row-keys="checkedRowKeys"
            @update:checked-row-keys="handleCheck"
          />
        </div>
      </div>
    </div>

    <!-- 防火墙规则表单 -->
    <FirewallRuleForm
      v-model="showRuleForm"
      :edit-rule="currentEditRule"
      @save="handleSaveRule"
    />
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
  NDataTable, 
  NDropdown,
  useMessage,
  type DataTableColumns 
} from 'naive-ui'
import FirewallRuleForm from '@/components/FirewallRuleForm.vue'
import {
  AddOutline,
  SearchOutline,
  CheckmarkCircleOutline,
  PauseCircleOutline,
  CloseCircleOutline,
  ListOutline,
  ChevronDownOutline,
  FilterOutline,
  CreateOutline,
  CopyOutline,
  TrashOutline,
  ArrowUpOutline,
  ArrowDownOutline,
} from '@vicons/ionicons5'

const message = useMessage()

// 界面状态
const showRuleForm = ref(false)
const currentEditRule = ref(null)
const firewallEnabled = ref(true)
const checkedRowKeys = ref<string[]>([])

// 搜索和过滤
const searchQuery = ref('')

// 批量操作选项
const batchOptions = [
  {
    label: '启用选中',
    key: 'enable',
    icon: () => h(NIcon, { component: CheckmarkCircleOutline })
  },
  {
    label: '禁用选中', 
    key: 'disable',
    icon: () => h(NIcon, { component: PauseCircleOutline })
  },
  {
    label: '删除选中',
    key: 'delete',
    icon: () => h(NIcon, { component: TrashOutline })
  }
]

// 筛选下拉选项
const filterDropdownOptions = [
  {
    label: '方向',
    key: 'direction-header',
    type: 'group',
    children: [
      { label: '入站规则', key: 'inbound' },
      { label: '出站规则', key: 'outbound' }
    ]
  },
  {
    label: '操作',
    key: 'action-header', 
    type: 'group',
    children: [
      { label: '允许', key: 'allow' },
      { label: '阻止', key: 'block' }
    ]
  },
  {
    label: '状态',
    key: 'status-header',
    type: 'group', 
    children: [
      { label: '已启用', key: 'enabled' },
      { label: '已禁用', key: 'disabled' }
    ]
  }
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
])

// 表格列定义
const columns: DataTableColumns = [
  {
    type: 'selection',
    width: 40
  },
  {
    title: '规则名称',
    key: 'name',
    width: 200,
    render(row: any) {
      return h('div', { class: 'rule-name-cell' }, [
        h('div', { class: 'rule-name' }, row.name),
        h('div', { class: 'rule-description' }, row.description)
      ])
    }
  },
  {
    title: '状态',
    key: 'enabled',
    width: 80,
    render(row: any) {
      return h(NTag, {
        type: row.enabled ? 'success' : 'default',
        size: 'small'
      }, {
        default: () => row.enabled ? '启用' : '禁用'
      })
    }
  },
  {
    title: '方向',
    key: 'direction',
    width: 80,
    render(row: any) {
      return h('div', { class: 'direction-cell' }, [
        h(NIcon, { 
          component: row.direction === 'inbound' ? ArrowDownOutline : ArrowUpOutline,
          size: 16,
          style: { marginRight: '4px' }
        }),
        row.direction === 'inbound' ? '入站' : '出站'
      ])
    }
  },
  {
    title: '操作',
    key: 'action',
    width: 80,
    render(row: any) {
      return h(NTag, {
        type: row.action === 'allow' ? 'success' : 'error',
        size: 'small'
      }, {
        default: () => row.action === 'allow' ? '允许' : '阻止'
      })
    }
  },
  {
    title: '程序/服务',
    key: 'program',
    width: 250,
    ellipsis: {
      tooltip: true
    }
  },
  {
    title: '协议',
    key: 'protocol',
    width: 80
  },
  {
    title: '端口',
    key: 'port',
    width: 100
  },
  {
    title: '配置文件',
    key: 'profiles',
    width: 150,
    render(row: any) {
      return h('div', { class: 'profiles-cell' }, 
        row.profiles.map((profile: string) => 
          h(NTag, {
            key: profile,
            type: 'info',
            size: 'small',
            style: { marginRight: '4px', marginBottom: '2px' }
          }, {
            default: () => profile
          })
        )
      )
    }
  },
  {
    title: '操作',
    key: 'actions',
    width: 120,
    render(row: any) {
      return h('div', { class: 'action-buttons' }, [
        h(NButton, {
          size: 'small',
          quaternary: true,
          onClick: () => editRule(row)
        }, {
          default: () => '编辑'
        }),
        h(NButton, {
          size: 'small',
          quaternary: true,
          type: 'error',
          onClick: () => deleteRule(row.id)
        }, {
          default: () => '删除'
        })
      ])
    }
  }
]

// 计算属性
const filteredRules = computed(() => {
  let rules = mockRules.value
  
  // 搜索过滤
  if (searchQuery.value) {
    const query = searchQuery.value.toLowerCase()
    rules = rules.filter(rule => 
      rule.name.toLowerCase().includes(query) ||
      rule.description.toLowerCase().includes(query)
    )
  }
  
  return rules
})

// 方法
const openRuleForm = (rule: any = null) => {
  currentEditRule.value = rule
  showRuleForm.value = true
}

const editRule = (rule: any) => {
  currentEditRule.value = { ...rule }
  showRuleForm.value = true
}

const deleteRule = (id: string) => {
  const index = mockRules.value.findIndex(rule => rule.id === id)
  if (index !== -1) {
    mockRules.value.splice(index, 1)
    message.success('规则删除成功')
  }
}

const handleCheck = (keys: string[]) => {
  checkedRowKeys.value = keys
}

const handleSaveRule = (rule: any) => {
  if (rule.id) {
    // 编辑现有规则
    const index = mockRules.value.findIndex(r => r.id === rule.id)
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
  gap: 24px;
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
  align-items: center;
  gap: 12px;
}

.firewall-status {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 8px 16px;
  background: var(--bg-card);
  border: 1px solid var(--border-primary);
  border-radius: 8px;
}

.status-label {
  font-size: 14px;
  color: var(--text-secondary);
  white-space: nowrap;
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
  align-items: center;
  gap: 12px;
}

/* 表格容器 */
.table-container {
  padding: 16px 24px 24px;
}

/* 表格单元格样式 */
.rule-name-cell {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.rule-name {
  font-weight: 600;
  color: var(--text-primary);
  font-size: 14px;
}

.rule-description {
  font-size: 12px;
  color: var(--text-muted);
  line-height: 1.3;
}

.direction-cell {
  display: flex;
  align-items: center;
  font-size: 13px;
  color: var(--text-secondary);
}

.profiles-cell {
  display: flex;
  flex-wrap: wrap;
  gap: 4px;
}

.action-buttons {
  display: flex;
  gap: 8px;
}

/* 响应式 */
@media (max-width: 1200px) {
  .firewall-toolbar {
    flex-direction: column;
    align-items: stretch;
    gap: 16px;
  }

  .toolbar-right {
    width: 100%;
    justify-content: space-between;
    flex-wrap: wrap;
  }
}

@media (max-width: 768px) {
  .firewall-container {
    padding: 16px;
  }

  .stats-grid {
    grid-template-columns: 1fr 1fr;
    gap: 12px;
  }

  .stat-card {
    padding: 16px;
  }

  .stat-number {
    font-size: 24px;
  }

  .panel-header {
    flex-direction: column;
    align-items: stretch;
    gap: 12px;
  }

  .panel-controls {
    width: 100%;
    justify-content: space-between;
  }

  .toolbar-right {
    flex-direction: column;
    gap: 8px;
  }

  .firewall-status {
    width: 100%;
    justify-content: space-between;
  }
}

/* Naive UI 数据表格样式覆盖 */
:deep(.n-data-table) {
  --n-td-color: var(--bg-card);
  --n-td-color-striped: var(--bg-tertiary);
  --n-th-color: var(--bg-secondary);
  --n-border-color: var(--border-secondary);
  --n-th-text-color: var(--text-secondary);
  --n-td-text-color: var(--text-primary);
  --n-font-size: 13px;
}

:deep(.n-data-table-th) {
  font-weight: 600;
  background-color: var(--bg-secondary);
}

:deep(.n-data-table-td) {
  padding: 12px 16px;
}

:deep(.n-data-table-th) {
  padding: 12px 16px;
}

:deep(.n-input) {
  --n-border: 1px solid var(--border-tertiary);
  --n-border-hover: 1px solid var(--border-hover);
  --n-border-focus: 1px solid var(--accent-primary);
  --n-color: var(--bg-card);
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
</style>
