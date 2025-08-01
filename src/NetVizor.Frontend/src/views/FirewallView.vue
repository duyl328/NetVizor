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
            <n-switch 
              v-model:value="firewallEnabled" 
              size="medium" 
              :loading="switchLoading"
              @update:value="handleFirewallToggle" 
            />
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
            <div class="stat-number">{{ stats.disableRules }}</div>
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
            <div class="stat-number">{{ stats.outboundRules }}</div>
            <div class="stat-label">出站规则</div>
          </div>
        </div>

        <div class="stat-card">
          <div class="stat-icon-wrapper error">
            <n-icon :component="CloseCircleOutline" size="24" />
          </div>
          <div class="stat-content">
            <div class="stat-number">{{ stats.inboundRules }}</div>
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
                style="padding: 3px"
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

          <!-- 数据调试信息 -->
          <div v-if="initialLoading" class="loading-info">
            <span>正在加载数据...</span>
          </div>
          <div v-else-if="firewallRule.length === 0" class="no-data">
            <span>暂无数据</span>
          </div>
          <div v-if="false" class="data-info">
            <span
              >总共 {{ totalCount }} 条数据（已加载 {{ loadedRanges.size * pageSize }} 条）</span
            >
          </div>

          <!-- 虚拟滚动列表 -->
          <RecycleScroller
            v-if="!initialLoading && firewallRule.length > 0"
            class="rules-scroller"
            :items="recyclerItems"
            :item-size="80"
            :buffer="200"
            key-field="id"
            style="height: 500px"
            @scroll="onScroll"
            @resize="onResize"
            @visible="onVisibleRangeUpdate"
            v-slot="{ item }"
          >
            <div :key="item.id" class="scroller-item-wrapper">
              <!-- 占位符项 -->
              <div
                v-if="item.isPlaceholder"
                class="rule-item loading-placeholder"
                :class="{ 'deleted-item': item.isDeleted }"
              >
                <div class="loading-content">
                  <span>{{ item.isDeleted ? '规则已删除' : '加载中...' }}</span>
                </div>
              </div>

              <!-- 已加载的数据项 -->
              <div v-else class="rule-item" :class="{ selected: isItemSelected(item) }">
                <div class="rule-cell checkbox-cell">
                  <n-checkbox
                    :checked="isItemSelected(item)"
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
            </div>
          </RecycleScroller>
        </div>
      </div>
    </div>

    <!-- 防火墙规则表单 -->
    <FirewallRuleForm
      ref="ruleFormRef"
      v-model="showRuleForm"
      :edit-rule="currentEditRule"
      @save="handleSaveRule"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, h, watch } from 'vue'
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
  useDialog,
} from 'naive-ui'
import { RecycleScroller, DynamicScroller } from 'vue-virtual-scroller'
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
import { httpClient } from '@/utils/http'
import type {
  FirewallStatus,
  FirewallRule,
  RuleDirection,
  RuleAction,
  ProtocolType,
} from '@/types/firewall'
import { ApiResponse } from '@/types/http'
import StringUtils from '@/utils/stringUtils'
import {
  FirewallRuleResponse,
  FirewallRuleShow,
  FirewallRulesParam,
  CreateFirewallRuleRequest,
  UpdateFirewallRuleRequest,
} from '@/types/firewallFrond'

const message = useMessage()
const dialog = useDialog()

// 界面状态
const showRuleForm = ref(false)
const currentEditRule = ref(null)
const ruleFormRef = ref() // 表单组件引用
const originalRuleName = ref<string>('') // 用于跟踪编辑时的原始规则名称
const firewallEnabled = ref(true)
const switchLoading = ref(false) // 开关加载状态
const checkedRowKeys = ref<string[]>([])

// 搜索与过滤
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
  //禁用规则
  disableRules: 0,
  //启动规则
  active: 0,
  // 出战规则
  outboundRules: 0,
  // 进站规则
  inboundRules: 0,
})

// 刷新防火墙状态数据
const refreshFirewallStatus = async () => {
  try {
    const res: ApiResponse<FirewallStatus> = await httpClient.get('/firewall/status')
    if (!res.success) {
      console.error('获取防火墙状态失败:', res.message)
      return
    }
    if (res.data === null || res.data === undefined) {
      console.error('获取数据为空!')
      return
    }
    
    console.log(res.data.isEnabled,'防火墙是否打开');
    // 同步防火墙开关状态和统计数据
    firewallEnabled.value = res.data.isEnabled
    stats.value.inboundRules = res.data.inboundRules
    stats.value.outboundRules = res.data.outboundRules
    stats.value.active = res.data.enabledRules
    stats.value.disableRules = res.data.totalRules - res.data.enabledRules
  } catch (error) {
    console.error('刷新防火墙状态失败:', error)
  }
}

// 请求统计数据
refreshFirewallStatus()

// 当前筛选条件下的总数量（从接口获取）
const totalCount = ref(0)
const pageSize = 50
// 已加载的页索引（防止重复加载）
const loadedPages = new Set<number>()
// 防火墙规则列表 - 预分配所有位置，包含null占位符
const firewallRule = ref<(FirewallRuleShow | null)[]>([])
const loadedRanges = new Set<string>() // 例如："100-149"
const deletedIndices = new Set<number>() // 跟踪已删除项目的索引

// 当前已加载的有效数据（非null非占位符）
const currentLoadedRules = computed(() => {
  return firewallRule.value.filter((rule) => rule !== null) as FirewallRuleShow[]
})

// 为RecycleScroller准备的数据，每个项目都有唯一的id
const recyclerItems = computed(() => {
  return firewallRule.value.map((item, index) => {
    if (item) {
      return item
    } else {
      // 为null项创建占位符对象，模拟FirewallRuleShow结构
      return {
        id: `placeholder-${index}`,
        name: '',
        description: '',
        enabled: false,
        direction: 'inbound' as const,
        action: 'allow' as const,
        program: '',
        protocol: '',
        port: '',
        profiles: [] as string[],
        isPlaceholder: true as const,
        isDeleted: deletedIndices.has(index), // 标记是否为删除的项目
        index: index,
      }
    }
  })
})

// 类型守卫函数
function isPlaceholder(item: unknown): item is { isPlaceholder: true } {
  return item && item.isPlaceholder === true
}

// 智能选中状态检查，避免虚拟滚动的选中状态闪烁
function isItemSelected(item: unknown): boolean {
  // 如果是占位符，永远不选中
  if (isPlaceholder(item)) {
    return false
  }

  // 确保item存在且有有效的id
  if (!item || !item.id || typeof item.id !== 'string') {
    return false
  }

  // 只有真实数据项且在选中列表中才返回true
  return checkedRowKeys.value.includes(item.id)
}

// 当前页和是否还有更多数据
const currentPage = ref(0)
const hasMore = ref(true)

// 数据加载状态
const loading = ref(false)
const initialLoading = ref(true)

// 数据转换函数
function convertFirewallRuleToShow(rule: FirewallRule): FirewallRuleShow {
  return {
    id: crypto.randomUUID(), // 生成唯一ID
    name: rule.name,
    description: rule.description || '',
    enabled: rule.enabled,
    direction: rule.direction === 1 ? 'inbound' : 'outbound',
    action: rule.action === 1 ? 'allow' : 'block',
    program: StringUtils.isBlank(rule.serviceName) ? rule.applicationName : rule.serviceName,
    protocol: getProtocolName(rule.protocol),
    port: rule.localPorts || '任意',
    profiles: parseProfiles(rule.profiles),
  }
}

// 将前端表单数据转换为API创建请求格式
function convertToCreateRequest(formRule: FirewallRuleShow): CreateFirewallRuleRequest {
  return {
    name: formRule.name,
    description: formRule.description || '',
    applicationName: formRule.program === '任意' ? '' : formRule.program,
    localAddresses: '*',
    remoteAddresses: '*',
    protocol: formRule.protocol === '任意' ? 'TCP' : formRule.protocol,
    icmpTypesAndCodes: '',
    localPorts: formRule.port === '任意' ? '*' : formRule.port,
    remotePorts: '*',
    direction: formRule.direction === 'inbound' ? 'Inbound' : 'Outbound',
    enabled: formRule.enabled,
    profiles: formRule.profiles
      .map((p) => {
        switch (p) {
          case '域':
            return 'Domain'
          case '专用':
            return 'Private'
          case '公用':
            return 'Public'
          default:
            return p
        }
      })
      .join(','),
    action: formRule.action === 'allow' ? 'Allow' : 'Block',
    grouping: '',
    interfaceTypes: 'All',
    edgeTraversal: false,
  }
}

// 将前端表单数据转换为API更新请求格式
function convertToUpdateRequest(
  formRule: FirewallRuleShow,
  originalName: string,
): UpdateFirewallRuleRequest {
  return {
    currentName: originalName,
    newName: formRule.name !== originalName ? formRule.name : undefined,
    description: formRule.description || '',
    enabled: formRule.enabled,
    applicationName: formRule.program === '任意' ? '' : formRule.program,
    protocol: formRule.protocol === '任意' ? 'TCP' : formRule.protocol,
    localPorts: formRule.port === '任意' ? '*' : formRule.port,
    remotePorts: '*',
    localAddresses: '*',
    remoteAddresses: '*',
    profiles: formRule.profiles
      .map((p) => {
        switch (p) {
          case '域':
            return 'Domain'
          case '专用':
            return 'Private'
          case '公用':
            return 'Public'
          default:
            return p
        }
      })
      .join(','),
    action: formRule.action === 'allow' ? 'Allow' : 'Block',
    grouping: '',
    edgeTraversal: false,
  }
}

// 协议名称转换
function getProtocolName(protocol: ProtocolType): string {
  switch (protocol) {
    case 6:
      return 'TCP'
    case 17:
      return 'UDP'
    case 1:
      return 'ICMPV4'
    case 256:
      return '任意'
    default:
      return String(protocol)
  }
}

function parseProfiles(profiles: number | string): string[] {
  const result: string[] = []

  // 如果是字符串，先转换为小写并去除多余空格
  if (typeof profiles === 'string') {
    const normalized = profiles.trim().toLowerCase()

    if (normalized === 'all') {
      return ['域', '专用', '公用']
    }

    const parts = normalized
      .split(',')
      .map(p => p.trim()) // 去除每个字段两边的空格

    for (const part of parts) {
      if (part === 'domain') result.push('域')
      else if (part === 'private') result.push('专用')
      else if (part === 'public') result.push('公用')
    }

    return result
  }

  // 数字类型按位判断
  if (profiles & 1) result.push('域')
  if (profiles & 2) result.push('专用')
  if (profiles & 4) result.push('公用')

  return result
}


/**
 * 获取防火墙信息
 * @param arg
 */
async function getFirewallRules(arg: FirewallRulesParam): Promise<FirewallRuleResponse | null> {
  // 请求规则数据
  const firewall: ApiResponse<FirewallRuleResponse> = await httpClient.get('/firewall/rules', arg)
  if (!firewall.success) {
    const errorMsg = StringUtils.isBlank(firewall.message)
      ? '数据请求出错，错误信息为空!'
      : firewall.message
    message.error(errorMsg)
    return null
  }
  return firewall.data
}

// 构建查询参数
function buildQueryParams(baseParams: Partial<FirewallRulesParam> = {}): FirewallRulesParam {
  const params: FirewallRulesParam = {
    ...baseParams,
  }

  // 添加搜索关键词
  if (searchQuery.value.trim()) {
    params.search = searchQuery.value.trim()
  }

  // 添加过滤条件
  if (filters.value.direction) {
    params.direction = filters.value.direction
  }

  if (filters.value.enabled !== null) {
    params.enabled = filters.value.enabled
  }

  if (filters.value.action) {
    params.action = filters.value.action === 'allow' ? 1 : 0
  }

  if (filters.value.protocol) {
    params.protocol = getProtocolValue(filters.value.protocol)
  }

  return params
}

// 协议值转换
function getProtocolValue(protocol: string): number {
  switch (protocol) {
    case 'TCP':
      return 6
    case 'UDP':
      return 17
    case 'ICMPV4':
      return 1
    case '任意':
      return 256
    default:
      return 256
  }
}

// 分段加载数据
async function loadRange(startIndex: number, limit = pageSize) {
  if (loading.value) {
    console.log('已在加载中，跳过请求')
    return
  }

  const rangeKey = `${startIndex}-${startIndex + limit - 1}`
  if (loadedRanges.has(rangeKey)) {
    //console.log('范围已加载，跳过:', rangeKey)
    return
  }
  //console.log('开始加载数据范围:', rangeKey)

  try {
    loading.value = true
    const params = buildQueryParams({
      start: startIndex,
      limit: limit,
    })

    //console.log('请求参数:', params)
    const res = await getFirewallRules(params)
    if (!res) {
      console.log('请求返回空结果')
      return
    }

    //console.log('收到数据:', res.rules.length, '条，总数:', res.totalCount)

    // 确保数组已预分配
    if (firewallRule.value.length === 0 && res.totalCount > 0) {
      console.log('预分配数组大小:', res.totalCount)
      firewallRule.value = Array(res.totalCount).fill(null)
      totalCount.value = res.totalCount
    }

    // 填充数据到指定位置
    for (let i = 0; i < res.rules.length; i++) {
      const index = startIndex + i
      if (index < firewallRule.value.length) {
        firewallRule.value[index] = convertFirewallRuleToShow(res.rules[i])
        //console.log(`填充数据到位置 ${index}:`, firewallRule.value[index]?.name)
      }
    }

    loadedRanges.add(rangeKey)
    //console.log('范围加载完成:', rangeKey, '已加载范围数:', loadedRanges.size)
  } catch (error) {
    console.error('加载数据失败:', error)
    message.error('数据加载失败')
  } finally {
    loading.value = false
  }
}

// 重置并预分配数组
async function resetAndLoadFirst() {
  try {
    console.log('开始重置并预分配数组')
    initialLoading.value = true
    firewallRule.value = []
    loadedRanges.clear()
    deletedIndices.clear() // 清理删除标记

    // 先获取总数
    const params = buildQueryParams({
      start: 0,
      limit: 1, // 只获取1条数据来获取总数
    })

    console.log('获取总数的请求参数:', params)
    const res = await getFirewallRules(params)
    if (!res) {
      console.log('获取总数失败')
      return
    }

    console.log('获取到总数:', res.totalCount)
    totalCount.value = res.totalCount

    // 预分配整个数组
    firewallRule.value = Array(res.totalCount).fill(null)
    console.log('预分配完成，数组长度:', firewallRule.value.length)

    // 加载首页数据
    await loadRange(0, pageSize)

    console.log('首页加载完成')
  } catch (error) {
    console.error('初始化失败:', error)
    message.error('数据加载失败')
  } finally {
    initialLoading.value = false
  }
}

/**
 * 滚动事件处理 - 检测是否需要加载更多数据
 */
function onVisibleRangeUpdate(event?: unknown) {
  console.log('可见范围更新触发:', event)

  // 检查事件参数格式
  if (!event) {
    console.log('事件参数为空，跳过处理')
    return
  }

  // 尝试不同的事件参数格式
  let startIndex = 0
  let endIndex = 0

  if (event.startIndex !== undefined && event.endIndex !== undefined) {
    // 标准格式
    startIndex = event.startIndex
    endIndex = event.endIndex
  } else if (event.start !== undefined && event.end !== undefined) {
    // 另一种可能的格式
    startIndex = event.start
    endIndex = event.end
  } else if (typeof event === 'object' && event.length) {
    // 如果是数组格式
    startIndex = event[0] || 0
    endIndex = event[1] || firewallRule.value.length - 1
  } else {
    console.log('无法解析事件参数格式，使用滚动位置检测')
    return
  }

  console.log('解析的可见范围:', { startIndex, endIndex })

  // 计算需要加载的数据段
  const alignedStart = Math.floor(startIndex / pageSize) * pageSize
  const alignedEnd = Math.ceil((endIndex + 1) / pageSize) * pageSize

  console.log('计算的加载范围:', alignedStart, alignedEnd)

  // 加载可见范围内的数据段
  for (let i = alignedStart; i < alignedEnd; i += pageSize) {
    if (i < totalCount.value) {
      console.log('尝试加载范围:', i, 'to', i + pageSize - 1)
      loadRange(i, pageSize)
    }
  }
}

/**
 * 普通滚动事件处理
 */
function onScroll(event: Event) {
  // console.log('滚动事件触发')

  // 尝试基于滚动位置估算可见范围
  const target = event.target as HTMLElement
  if (target) {
    const { scrollTop, scrollHeight, clientHeight } = target
    const scrollPercentage = (scrollTop + clientHeight) / scrollHeight

    // console.log('滚动位置信息:', {
    //   scrollTop,
    //   scrollHeight,
    //   clientHeight,
    //   scrollPercentage: Math.round(scrollPercentage * 100) + '%'
    // })

    // 估算当前可见的项目范围
    const itemHeight = 80 // item-size
    const visibleStartIndex = Math.floor(scrollTop / itemHeight)
    const visibleEndIndex = Math.ceil((scrollTop + clientHeight) / itemHeight)

    // console.log('估算的可见范围:', { visibleStartIndex, visibleEndIndex })

    // 计算需要加载的数据段
    const alignedStart = Math.floor(visibleStartIndex / pageSize) * pageSize
    const alignedEnd = Math.ceil((visibleEndIndex + 1) / pageSize) * pageSize

    // console.log('基于滚动位置的加载范围:', alignedStart, alignedEnd)

    // 加载可见范围内的数据段
    for (let i = alignedStart; i < alignedEnd; i += pageSize) {
      if (i < totalCount.value) {
        // console.log('滚动位置触发加载范围:', i, 'to', i + pageSize - 1)
        loadRange(i, pageSize)
      }
    }
  }
}

/**
 * 调整大小事件处理
 */
function onResize() {
  console.log('RecycleScroller调整大小')
}

// 初始化数据
resetAndLoadFirst()

// 监听搜索和过滤条件变化，重新加载数据
watch(
  [searchQuery, filters],
  () => {
    // 搜索条件变化时清理选中状态，避免状态不一致
    checkedRowKeys.value = []
    resetAndLoadFirst()
  },
  { deep: true },
)

// 全选相关计算属性
const allSelected = computed({
  get: () => {
    const currentValidRules = currentLoadedRules.value
    if (currentValidRules.length === 0) {
      return false
    }

    // 检查当前已加载数据中有多少被选中
    const selectedInCurrentData = currentValidRules.filter((rule) =>
      checkedRowKeys.value.includes(rule.id),
    )

    return selectedInCurrentData.length === currentValidRules.length
  },
  set: (value: boolean) => {
    if (value) {
      // 选中当前已加载的所有有效数据
      const currentValidRules = currentLoadedRules.value
      const currentIds = currentValidRules.map((rule) => rule.id)

      // 合并现有选中和当前页选中（去重）
      const mergedIds = [...new Set([...checkedRowKeys.value, ...currentIds])]
      checkedRowKeys.value = mergedIds
    } else {
      // 取消选中当前已加载的数据
      const currentValidRules = currentLoadedRules.value
      const currentIds = new Set(currentValidRules.map((rule) => rule.id))

      // 只保留不在当前页的选中项
      checkedRowKeys.value = checkedRowKeys.value.filter((id) => !currentIds.has(id))
    }
  },
})

const indeterminate = computed(() => {
  const currentValidRules = currentLoadedRules.value
  if (currentValidRules.length === 0) {
    return false
  }

  // 检查当前已加载数据中有多少被选中
  const selectedInCurrentData = currentValidRules.filter((rule) =>
    checkedRowKeys.value.includes(rule.id),
  )

  return selectedInCurrentData.length > 0 && selectedInCurrentData.length < currentValidRules.length
})

// 方法
const openRuleForm = (rule: unknown = null) => {
  currentEditRule.value = rule
  showRuleForm.value = true
}

const editRule = (rule: unknown) => {
  const ruleToEdit = rule as FirewallRuleShow
  currentEditRule.value = { ...ruleToEdit }
  originalRuleName.value = ruleToEdit.name // 记录原始名称用于API调用
  showRuleForm.value = true
}

const deleteRule = async (id: string) => {
  // 从本地数据中找到对应的规则
  const ruleToDelete = firewallRule.value.find((rule) => rule?.id === id)
  if (!ruleToDelete) {
    message.error('找不到要删除的规则')
    return
  }

  // 显示确认对话框
  dialog.warning({
    title: '确认删除',
    content: `您确定要删除防火墙规则"${ruleToDelete.name}"吗？此操作不可撤销。`,
    positiveText: '确认删除',
    negativeText: '取消',
    onPositiveClick: async () => {
      try {
        console.log('删除规则:', ruleToDelete.name)

        // 调用删除API
        const res: ApiResponse<null> = await httpClient.delete(
          `/firewall/rules?name=${encodeURIComponent(ruleToDelete.name)}`,
        )

        if (!res.success) {
          const errorMsg = StringUtils.isBlank(res.message) ? '删除规则失败！' : res.message
          message.error(errorMsg)
          return
        }

        // 从本地数据中移除
        const index = firewallRule.value.findIndex((rule) => rule?.id === id)
        if (index !== -1) {
          firewallRule.value[index] = null // 设为null而不是删除，保持索引不变
          deletedIndices.add(index) // 标记这个索引为已删除
          totalCount.value = Math.max(0, totalCount.value - 1)
          // 从选中项中移除
          checkedRowKeys.value = checkedRowKeys.value.filter((key) => key !== id)
          message.success('规则删除成功')
        }
      } catch (error) {
        console.error('删除规则失败:', error)
        message.error('删除规则时发生错误')
      }
    },
  })
}

const handleRuleCheck = (id: string, checked: boolean) => {
  // 防止处理无效的id
  if (!id || typeof id !== 'string') {
    return
  }

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

// 处理防火墙开关切换
const handleFirewallToggle = async (enabled: boolean) => {
  // 由于开关的双向绑定，需要先恢复原状态，然后显示确认对话框
  const originalState = !enabled
  firewallEnabled.value = originalState

  // 显示确认对话框
  const actionText = enabled ? '启用' : '禁用'
  const warningText = enabled 
    ? '启用防火墙将开始过滤网络流量，可能会阻止某些网络连接。' 
    : '禁用防火墙将停止所有网络保护，您的计算机将面临安全风险。'

  dialog.warning({
    title: `确认${actionText}防火墙`,
    content: `${warningText}\n\n您确定要${actionText}防火墙吗？`,
    positiveText: `确认${actionText}`,
    negativeText: '取消',
    onPositiveClick: async () => {
      try {
        switchLoading.value = true
        console.log('切换防火墙状态:', enabled)

        // 调用防火墙开关API，影响所有配置文件
        const res: ApiResponse<null> = await httpClient.post(`/firewall/switch?enabled=${enabled}&profile=all`)
        
        if (!res.success) {
          const errorMsg = StringUtils.isBlank(res.message) ? '切换防火墙状态失败！' : res.message
          message.error(errorMsg)
          return
        }

        message.success(`防火墙已${enabled ? '启用' : '禁用'}`)
        
        // 刷新防火墙状态数据
        await refreshFirewallStatus()
      } catch (error) {
        console.error('切换防火墙状态失败:', error)
        message.error('切换防火墙状态时发生错误')
      } finally {
        switchLoading.value = false
      }
    },
    onNegativeClick: () => {
      // 用户取消，保持原状态
      console.log('用户取消防火墙状态切换')
    }
  })
}

const handleSaveRule = async (rule: unknown) => {
  try {
    const formRule = rule as FirewallRuleShow
    console.log('保存规则:', formRule)

    if (formRule.id && originalRuleName.value) {
      // 编辑现有规则
      console.log('更新规则:', originalRuleName.value, '→', formRule.name)

      const updateRequest = convertToUpdateRequest(formRule, originalRuleName.value)
      const res: ApiResponse<null> = await httpClient.put('/firewall/rules', updateRequest)

      if (!res.success) {
        const errorMsg = StringUtils.isBlank(res.message) ? '更新规则失败！' : res.message
        // 调用表单组件的错误处理方法，不关闭弹窗
        ruleFormRef.value?.handleSaveError(errorMsg)
        return
      }

      // 更新本地数据
      const index = firewallRule.value.findIndex((r) => r?.id === formRule.id)
      if (index !== -1) {
        firewallRule.value[index] = formRule
      }

      // 调用表单组件的成功处理方法，关闭弹窗
      ruleFormRef.value?.handleSaveSuccess()
    } else {
      // 创建新规则
      console.log('创建新规则:', formRule.name)

      const createRequest = convertToCreateRequest(formRule)
      const res: ApiResponse<null> = await httpClient.post('/firewall/rules', createRequest)

      if (!res.success) {
        const errorMsg = StringUtils.isBlank(res.message) ? '创建规则失败！' : res.message
        // 调用表单组件的错误处理方法，不关闭弹窗
        ruleFormRef.value?.handleSaveError(errorMsg)
        return
      }

      // 调用表单组件的成功处理方法，关闭弹窗
      ruleFormRef.value?.handleSaveSuccess()
      // 重新加载数据以获取新的总数和数据
      resetAndLoadFirst()
    }

    // 重置状态
    currentEditRule.value = null
    originalRuleName.value = ''
  } catch (error) {
    console.error('保存规则失败:', error)
    // 调用表单组件的错误处理方法，不关闭弹窗
    ruleFormRef.value?.handleSaveError('保存规则时发生错误')
  }
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
  .checkbox-cell {
    width: 60px;
  }

  .name-cell {
    width: 250px;
  }

  .status-cell {
    width: 100px;
  }

  .direction-cell {
    width: 100px;
  }

  .action-cell {
    width: 100px;
  }

  .program-cell {
    width: 320px;
    flex-shrink: 1;
  }

  .protocol-cell {
    width: 100px;
  }

  .port-cell {
    width: 120px;
  }

  .profiles-cell {
    width: 180px;
    flex-shrink: 1;
  }

  .actions-cell {
    width: 120px;
  }
}

/* 极大屏幕 >2000px */
@media (min-width: 2001px) {
  .checkbox-cell {
    width: 70px;
  }

  .name-cell {
    width: 280px;
  }

  .status-cell {
    width: 110px;
  }

  .direction-cell {
    width: 110px;
  }

  .action-cell {
    width: 110px;
  }

  .program-cell {
    width: 380px;
    flex-shrink: 1;
  }

  .protocol-cell {
    width: 110px;
  }

  .port-cell {
    width: 140px;
  }

  .profiles-cell {
    width: 200px;
    flex-shrink: 1;
  }

  .actions-cell {
    width: 140px;
  }
}

/* 超大屏幕 1601px-1800px */
@media (min-width: 1601px) and (max-width: 1800px) {
  .checkbox-cell {
    width: 55px;
  }

  .name-cell {
    width: 230px;
  }

  .status-cell {
    width: 90px;
  }

  .direction-cell {
    width: 90px;
  }

  .action-cell {
    width: 90px;
  }

  .program-cell {
    width: 280px;
    flex-shrink: 1;
  }

  .protocol-cell {
    width: 90px;
  }

  .port-cell {
    width: 110px;
  }

  .profiles-cell {
    width: 160px;
    flex-shrink: 1;
  }

  .actions-cell {
    width: 110px;
  }
}

/* 基础列宽 - 针对超大屏幕（1401px-1600px） */
@media (min-width: 1401px) and (max-width: 1600px) {
  .checkbox-cell {
    width: 50px;
  }

  .name-cell {
    width: 200px;
  }

  .status-cell {
    width: 80px;
  }

  .direction-cell {
    width: 80px;
  }

  .action-cell {
    width: 80px;
  }

  .program-cell {
    width: 250px;
    flex-shrink: 1;
  }

  .protocol-cell {
    width: 80px;
  }

  .port-cell {
    width: 100px;
  }

  .profiles-cell {
    width: 150px;
    flex-shrink: 1;
  }

  .actions-cell {
    width: 100px;
  }
}

/* 大屏幕 1201px-1400px */
@media (min-width: 1201px) and (max-width: 1400px) {
  .checkbox-cell {
    width: 45px;
  }

  .name-cell {
    width: 180px;
  }

  .status-cell {
    width: 70px;
  }

  .direction-cell {
    width: 70px;
  }

  .action-cell {
    width: 70px;
  }

  .program-cell {
    width: 220px;
    flex-shrink: 1;
  }

  .protocol-cell {
    width: 70px;
  }

  .port-cell {
    width: 90px;
  }

  .profiles-cell {
    width: 130px;
    flex-shrink: 1;
  }

  .actions-cell {
    width: 90px;
  }
}

/* 中等屏幕 1025px-1200px */
@media (min-width: 1025px) and (max-width: 1200px) {
  .checkbox-cell {
    width: 40px;
  }

  .name-cell {
    width: 160px;
  }

  .status-cell {
    width: 65px;
  }

  .direction-cell {
    width: 65px;
  }

  .action-cell {
    width: 65px;
  }

  .program-cell {
    width: 200px;
    flex-shrink: 1;
  }

  .protocol-cell {
    width: 65px;
  }

  .port-cell {
    width: 80px;
  }

  .profiles-cell {
    width: 120px;
    flex-shrink: 1;
  }

  .actions-cell {
    width: 85px;
  }
}

/* 小屏幕 769px-1024px */
@media (min-width: 769px) and (max-width: 1024px) {
  .checkbox-cell {
    width: 40px;
  }

  .name-cell {
    width: 140px;
  }

  .status-cell {
    width: 60px;
  }

  .direction-cell {
    width: 60px;
  }

  .action-cell {
    width: 60px;
  }

  .program-cell {
    width: 180px;
    flex-shrink: 1;
  }

  .protocol-cell {
    width: 60px;
  }

  .port-cell {
    width: 80px;
  }

  .profiles-cell {
    width: 100px;
    flex-shrink: 1;
  }

  .actions-cell {
    width: 80px;
  }
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

  .checkbox-cell {
    width: 35px;
  }

  .name-cell {
    width: 120px;
  }

  .status-cell {
    width: 50px;
  }

  .direction-cell {
    width: 50px;
  }

  .action-cell {
    width: 50px;
  }

  .protocol-cell {
    width: 50px;
  }

  .port-cell {
    width: 60px;
  }

  .actions-cell {
    width: 70px;
  }
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

/* 调试信息样式 */
.loading-info,
.no-data,
.data-info {
  padding: 20px;
  text-align: center;
  color: var(--text-secondary);
  background: var(--bg-tertiary);
  border-radius: 8px;
  margin: 10px 0;
}

.data-info {
  background: var(--bg-card);
  border: 1px solid var(--border-primary);
  font-weight: 500;
}

/* 加载占位符样式 */
.loading-placeholder {
  display: flex !important;
  align-items: center;
  justify-content: center;
  background: var(--bg-tertiary);
  opacity: 0.6;
}

.loading-placeholder.deleted-item {
  background: rgba(239, 68, 68, 0.1);
  border-left: 3px solid var(--accent-error);
}

.loading-placeholder.deleted-item .loading-content {
  color: var(--accent-error);
  font-weight: 500;
}

.loading-content {
  display: flex;
  align-items: center;
  gap: 8px;
  color: var(--text-muted);
  font-size: 14px;
}

/* vue-virtual-scroller 样式 */
:deep(.vue-recycle-scroller) {
  height: 100%;
}

:deep(.vue-recycle-scroller__item-wrapper) {
  overflow: visible;
}

/* 自定义滚动项包装器 */
.scroller-item-wrapper {
  width: 100%;
  height: 100%;
}
</style>
