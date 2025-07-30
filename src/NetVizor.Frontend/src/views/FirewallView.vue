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
          <div class="stat-icon-wrapper info">
            <n-icon :component="ArrowDownOutline" size="24" />
          </div>
          <div class="stat-content">
            <div class="stat-number">{{ stats.inbound }}</div>
            <div class="stat-label">入站规则</div>
          </div>
        </div>

        <div class="stat-card">
          <div class="stat-icon-wrapper info">
            <n-icon :component="ArrowUpOutline" size="24" />
          </div>
          <div class="stat-content">
            <div class="stat-number">{{ stats.outbound }}</div>
            <div class="stat-label">出站规则</div>
          </div>
        </div>
      </div>

      <!-- 规则列表 -->
      <div class="rules-panel">
        <div class="panel-header">
          <h3 class="panel-title">防火墙规则列表</h3>
          <div class="panel-controls">
            <!-- 批量操作按钮 -->
            <n-dropdown
              v-if="persistentSelectedRules.size > 0"
              :options="batchOptions"
              @select="handleBatchOperation"
            >
              <n-button size="small" type="primary">
                批量操作 ({{ persistentSelectedRules.size }})
                <template #icon>
                  <n-icon :component="ChevronDownOutline" />
                </template>
              </n-button>
            </n-dropdown>
            
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

          <!-- 加载状态 -->
          <div v-if="loading && totalCount === 0" class="loading-container">
            <n-spin size="large">
              <div class="loading-text">正在加载防火墙规则...</div>
            </n-spin>
          </div>

          <!-- 虚拟滚动列表 -->
          <RecycleScroller
            v-else
            class="rules-scroller"
            :items="filteredRules"
            :item-size="70"
            key-field="id"
            v-slot="{ item }"
            @scroll="handleScroll"
          >
            <RuleItem 
              :virtual-item="item" 
              :get-rule="getRuleForItem"
              :load-rules-in-range="loadRulesInRange" 
              @check="handleRuleCheck" 
              @edit="editRule" 
              @delete="deleteRule" 
              :checked="isRuleSelected(item.id)" 
            />
          </RecycleScroller>

          <!-- 底部状态 -->
          <div v-if="totalCount > 0" class="list-footer">
            <span>共 {{ totalCount }} 条规则</span>
          </div>

          <!-- 空状态 -->
          <div v-if="!loading && totalCount === 0" class="empty-state">
            <n-icon :component="ListOutline" size="48" />
            <div class="empty-text">暂无防火墙规则</div>
            <n-button type="primary" @click="openRuleForm()">
              <template #icon>
                <n-icon :component="AddOutline" />
              </template>
              创建第一个规则
            </n-button>
          </div>
        </div>
      </div>
    </div>

    <!-- 防火墙规则表单 -->
    <FirewallRuleForm v-model="showRuleForm" :edit-rule="currentEditRule" @save="handleSaveRule" />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, h, onMounted, watch, nextTick } from 'vue'
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
  NSpin,
} from 'naive-ui'
import { RecycleScroller } from 'vue-virtual-scroller'
import FirewallRuleForm from '@/components/FirewallRuleForm.vue'
import RuleItem from '@/components/RuleItem.vue'
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
import { firewallApi } from '@/api/firewall'
import type { DisplayRule, RuleFilter, FirewallStatistics } from '@/types/firewall'

const message = useMessage()

// 界面状态
const showRuleForm = ref(false)
const currentEditRule = ref<DisplayRule | null>(null)
const firewallEnabled = ref(true)
const checkedRowKeys = ref<string[]>([])
const persistentSelectedRules = ref<Set<string>>(new Set()) // 持久化选中的规则ID
const loading = ref(false)
const totalCount = ref(0)

// 虚拟滚动项类型
interface VirtualItem {
  id: string
  index: number
  rule?: DisplayRule
  loading?: boolean
}

// 搜索和过滤
const searchQuery = ref('')
const filters = ref({
  direction: null as string | null,
  action: null as string | null,
  enabled: null as boolean | null,
  protocol: null as string | null,
})

// 分页状态
const pagination = ref({
  start: 0,
  limit: 50,
  totalCount: 0,
  hasMore: false
})

// 数据状态 
const rules = ref<DisplayRule[]>([])
const virtualItems = ref<VirtualItem[]>([]) // 虚拟滚动项
const loadedRules = ref<Map<number, DisplayRule>>(new Map()) // 缓存已加载的规则
const loadingRanges = ref<Set<string>>(new Set()) // 正在加载的范围，防止重复请求

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
  total: 0,
  active: 0,
  inactive: 0,
  inbound: 0,
  outbound: 0,
})

// API方法
/**
 * 获取防火墙规则总数
 */
const getTotalCount = async () => {
  try {
    // 构建筛选条件（不包含分页参数）
    const filter: RuleFilter = {}

    // 搜索条件
    if (searchQuery.value.trim()) {
      filter.name = searchQuery.value.trim()
    }

    // 筛选条件
    if (filters.value.direction) {
      filter.direction = filters.value.direction
    }
    if (filters.value.action) {
      filter.action = filters.value.action
    }
    if (filters.value.enabled !== null) {
      filter.enabled = filters.value.enabled
    }
    if (filters.value.protocol) {
      filter.protocol = filters.value.protocol
    }

    // 只获取第一页来得到总数
    filter.start = 0
    filter.limit = 1

    const response = await firewallApi.getRules(filter)
    totalCount.value = response.totalCount
    
    // 创建虚拟项
    createVirtualItems()
    
  } catch (error) {
    console.error('获取防火墙规则总数失败:', error)
    message.error('获取防火墙规则总数失败')
  }
}

/**
 * 创建虚拟滚动项
 */
const createVirtualItems = () => {
  virtualItems.value = Array.from({ length: totalCount.value }, (_, index) => ({
    id: `rule-${index}`,
    index: index
  }))
  loadedRules.value.clear()
  
  // 根据持久化选中状态重新构建当前页面的选中状态
  // 注意：这里不直接清空 checkedRowKeys，而是在实际渲染时动态检查
  // 延迟执行以确保规则已加载
  nextTick(() => {
    updateCurrentPageSelection()
  })
}

/**
 * 按需加载指定索引范围的规则
 */
const loadRulesInRange = async (startIndex: number, endIndex: number) => {
  // 创建范围标识符，防止重复请求
  const rangeKey = `${startIndex}-${endIndex}`
  if (loadingRanges.value.has(rangeKey)) {
    return // 已在加载中，直接返回
  }

  const needToLoad: number[] = []
  
  // 找出需要加载的索引
  for (let i = startIndex; i <= endIndex; i++) {
    if (!loadedRules.value.has(i)) {
      needToLoad.push(i)
    }
  }

  if (needToLoad.length === 0) return

  // 标记为加载中
  loadingRanges.value.add(rangeKey)

  try {
    // 优化：将连续的索引合并为一个请求
    const continuousRanges = getContinuousRanges(needToLoad)
    
    const requests = continuousRanges.map(async (range) => {
      // 构建筛选条件
      const filter: RuleFilter = {
        start: range.start,
        limit: range.end - range.start + 1
      }

      // 应用搜索和筛选条件
      if (searchQuery.value.trim()) {
        filter.name = searchQuery.value.trim()
      }
      if (filters.value.direction) {
        filter.direction = filters.value.direction
      }
      if (filters.value.action) {
        filter.action = filters.value.action
      }
      if (filters.value.enabled !== null) {
        filter.enabled = filters.value.enabled
      }
      if (filters.value.protocol) {
        filter.protocol = filters.value.protocol
      }

      const response = await firewallApi.getRules(filter)
      const displayRules = response.rules.map(rule => firewallApi.convertToDisplayRule(rule))

      // 缓存规则数据
      displayRules.forEach((rule, index) => {
        const actualIndex = range.start + index
        loadedRules.value.set(actualIndex, rule)
      })
    })

    // 等待所有请求完成
    await Promise.all(requests)

  } catch (error) {
    console.error('加载防火墙规则失败:', error)
    // 只有非取消的错误才显示给用户
    if (error.name !== 'AbortError' && !error.message?.includes('Cancel')) {
      message.error('加载防火墙规则失败')
    }
  } finally {
    // 移除加载标记
    loadingRanges.value.delete(rangeKey)
  }
}

/**
 * 将离散的索引数组合并为连续范围
 */
const getContinuousRanges = (indices: number[]) => {
  if (indices.length === 0) return []
  
  indices.sort((a, b) => a - b)
  const ranges: { start: number; end: number }[] = []
  let currentStart = indices[0]
  let currentEnd = indices[0]
  
  for (let i = 1; i < indices.length; i++) {
    if (indices[i] === currentEnd + 1) {
      // 连续的索引
      currentEnd = indices[i]
    } else {
      // 不连续，保存当前范围
      ranges.push({ start: currentStart, end: currentEnd })
      currentStart = indices[i]
      currentEnd = indices[i]
    }
  }
  
  // 添加最后一个范围
  ranges.push({ start: currentStart, end: currentEnd })
  
  return ranges
}

/**
 * 初始加载第一屏数据（仅在初始化时调用）
 */
const loadInitialData = async () => {
  // 只加载第一屏的数据，大约20条
  const initialBatchSize = 20
  if (totalCount.value > 0) {
    await loadRulesInRange(0, Math.min(initialBatchSize - 1, totalCount.value - 1))
  }
}

/**
 * 获取指定索引的规则数据
 */
const getRuleByIndex = (index: number): DisplayRule | null => {
  return loadedRules.value.get(index) || null
}

/**
 * 加载统计信息
 */
const loadStatistics = async () => {
  try {
    const statistics = await firewallApi.getStatistics()
    stats.value = {
      total: statistics.totalRules,
      active: statistics.enabledRules,
      inactive: statistics.disabledRules,
      inbound: statistics.rulesByDirection?.['1'] || 0, // Inbound = 1
      outbound: statistics.rulesByDirection?.['2'] || 0 // Outbound = 2
    }
  } catch (error) {
    console.error('加载统计信息失败:', error)
  }
}

/**
 * 刷新数据
 */
const refreshData = async () => {
  loading.value = true
  try {
    // 清空缓存和加载标记
    loadedRules.value.clear()
    loadingRanges.value.clear()
    
    await getTotalCount()
    await loadStatistics()
    
    // 初始加载第一屏数据
    await loadInitialData()
  } finally {
    loading.value = false
  }
}

// 计算属性
const filteredRules = computed(() => {
  return virtualItems.value
})

// 全选相关计算属性
const allSelected = computed({
  get: () => {
    return totalCount.value > 0 && persistentSelectedRules.value.size === totalCount.value
  },
  set: (value: boolean) => {
    if (value) {
      // 全选：需要加载所有规则才能获取所有规则名称
      // 这里简化处理，实际应用中可能需要更复杂的逻辑
      message.warning('全选功能需要先加载所有规则数据')
    } else {
      // 取消全选
      persistentSelectedRules.value.clear()
      checkedRowKeys.value = []
    }
  },
})

const indeterminate = computed(() => {
  return persistentSelectedRules.value.size > 0 && persistentSelectedRules.value.size < totalCount.value
})

// 方法
/**
 * 获取虚拟项的实际规则数据
 */
const getItemRule = async (item: VirtualItem): Promise<DisplayRule | null> => {
  const cachedRule = getRuleByIndex(item.index)
  if (cachedRule) {
    return cachedRule
  }

  // 预加载周围的数据（批量加载提高效率）
  const batchSize = 20
  const startIndex = Math.max(0, item.index - batchSize / 2)
  const endIndex = Math.min(totalCount.value - 1, item.index + batchSize / 2)
  
  await loadRulesInRange(startIndex, endIndex)
  
  return getRuleByIndex(item.index)
}

// 检查规则是否被选中（基于规则名称）
const isRuleSelected = (itemId: string): boolean => {
  const itemIndex = parseInt(itemId.replace('rule-', ''))
  const rule = getRuleByIndex(itemIndex)
  return rule ? persistentSelectedRules.value.has(rule.name) : false
}

// 同步更新 checkedRowKeys 以反映当前筛选结果中选中的项
const updateCurrentPageSelection = () => {
  const currentPageSelected: string[] = []
  
  // 遍历当前虚拟项，检查是否在持久化选中列表中
  virtualItems.value.forEach((item) => {
    if (isRuleSelected(item.id)) {
      currentPageSelected.push(item.id)
    }
  })
  
  checkedRowKeys.value = currentPageSelected
}
const getRuleForItem = (index: number): DisplayRule | null => {
  return getRuleByIndex(index)
}

const handleRuleCheck = (itemId: string, checked: boolean) => {
  // 从虚拟项ID获取对应的规则
  const itemIndex = parseInt(itemId.replace('rule-', ''))
  const rule = getRuleByIndex(itemIndex)
  
  if (!rule) return
  
  const ruleId = rule.name // 使用规则名称作为唯一ID
  
  if (checked) {
    // 添加到持久化选中集合
    persistentSelectedRules.value.add(ruleId)
    // 添加到当前页面选中列表（如果不存在）
    if (!checkedRowKeys.value.includes(itemId)) {
      checkedRowKeys.value.push(itemId)
    }
  } else {
    // 从持久化选中集合移除
    persistentSelectedRules.value.delete(ruleId)
    // 从当前页面选中列表移除
    checkedRowKeys.value = checkedRowKeys.value.filter((key) => key !== itemId)
  }
}

const handleSelectAll = (checked: boolean) => {
  allSelected.value = checked
}

const clearFilters = async () => {
  filters.value = {
    direction: null,
    action: null,
    enabled: null,
    protocol: null,
  }
  searchQuery.value = ''
  await refreshData()
}

// 处理规则编辑
const editRule = (rule: DisplayRule) => {
  currentEditRule.value = { ...rule }
  showRuleForm.value = true
}

const openRuleForm = (rule: DisplayRule | null = null) => {
  currentEditRule.value = rule
  showRuleForm.value = true
}

const handleSaveRule = async (rule: DisplayRule) => {
  try {
    if (currentEditRule.value?.name) {
      // 编辑现有规则
      const updateRequest = firewallApi.convertToCreateRequest(rule)
      await firewallApi.updateRule({
        currentName: currentEditRule.value.name,
        newName: rule.name !== currentEditRule.value.name ? rule.name : undefined,
        description: rule.description,
        enabled: rule.enabled,
        action: rule.action === 'allow' ? 1 : 0,
        // 其他需要的字段...
      })
      message.success('规则更新成功')
    } else {
      // 添加新规则
      const createRequest = firewallApi.convertToCreateRequest(rule)
      await firewallApi.createRule(createRequest)
      message.success('规则创建成功')
    }

    // 重新加载数据
    await refreshData()

    // 重置状态
    currentEditRule.value = null
  } catch (error) {
    console.error('保存规则失败:', error)
    message.error('保存规则失败')
  }
}

// 批量操作
const handleBatchOperation = async (operation: string) => {
  if (persistentSelectedRules.value.size === 0) {
    message.warning('请先选择要操作的规则')
    return
  }

  try {
    const ruleNames = Array.from(persistentSelectedRules.value)
    switch (operation) {
      case 'enable':
        await firewallApi.batchUpdateRules(ruleNames, 'enable')
        message.success('批量启用成功')
        break
      case 'disable':
        await firewallApi.batchUpdateRules(ruleNames, 'disable')
        message.success('批量禁用成功')
        break
      case 'delete':
        await firewallApi.batchUpdateRules(ruleNames, 'delete')
        message.success('批量删除成功')
        break
    }

    // 清空选中状态
    persistentSelectedRules.value.clear()
    checkedRowKeys.value = []
    await refreshData()
  } catch (error) {
    console.error('批量操作失败:', error)
    message.error('批量操作失败')
  }
}

const deleteRule = async (ruleName: string) => {
  try {
    await firewallApi.deleteRule(ruleName)
    // 从持久化选中状态和当前页面选中状态中移除
    persistentSelectedRules.value.delete(ruleName)
    checkedRowKeys.value = checkedRowKeys.value.filter(key => {
      const itemIndex = parseInt(key.replace('rule-', ''))
      const rule = getRuleByIndex(itemIndex)
      return rule?.name !== ruleName
    })
    message.success('规则删除成功')

    // 重新获取总数并刷新
    await refreshData()
  } catch (error) {
    console.error('删除规则失败:', error)
    message.error('删除规则失败')
  }
}

// 虚拟滚动相关 - 处理可见区域变化
const handleScroll = () => {
  // RecycleScroller 会自动处理可见区域，我们只需要在项目渲染时按需加载
}

// 监听筛选条件变化
watch([searchQuery, filters], async () => {
  await refreshData()
}, { deep: true })

// 组件挂载时加载数据
onMounted(async () => {
  await refreshData()
})
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

.stat-icon-wrapper.info {
  background: rgba(59, 130, 246, 0.1);
  color: var(--accent-info);
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

/* 列表底部状态 */
.list-footer {
  display: flex;
  justify-content: center;
  align-items: center;
  padding: 16px;
  background: var(--bg-tertiary);
  border-top: 1px solid var(--border-secondary);
  font-size: 13px;
  color: var(--text-muted);
  flex-shrink: 0;
}

/* 加载状态样式 */
.loading-container {
  display: flex;
  justify-content: center;
  align-items: center;
  height: 200px;
  flex-direction: column;
  gap: 16px;
}

.loading-text {
  font-size: 14px;
  color: var(--text-secondary);
}

.load-more-indicator {
  display: flex;
  justify-content: center;
  align-items: center;
  gap: 8px;
  padding: 16px;
  background: var(--bg-card);
  border-top: 1px solid var(--border-secondary);
  font-size: 14px;
  color: var(--text-secondary);
}

.no-more-data {
  display: flex;
  justify-content: center;
  align-items: center;
  padding: 16px;
  background: var(--bg-tertiary);
  border-top: 1px solid var(--border-secondary);
  font-size: 13px;
  color: var(--text-muted);
}

/* 空状态样式 */
.empty-state {
  display: flex;
  flex-direction: column;
  justify-content: center;
  align-items: center;
  height: 300px;
  gap: 16px;
  color: var(--text-muted);
}

.empty-text {
  font-size: 16px;
  font-weight: 500;
  color: var(--text-secondary);
}
</style>
