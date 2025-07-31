<template>
  <div class="rule-item" :class="{ selected: checked }" v-if="rule">
    <div class="rule-cell checkbox-cell">
      <n-checkbox
        :checked="checked"
        @update:checked="(checked) => $emit('check', virtualItem.id, checked)"
      />
    </div>

    <div class="rule-cell name-cell">
      <div 
        class="rule-name expandable-text" 
        :class="{ expanded: hoveredText === 'name' }"
        @mouseenter="hoveredText = 'name'" 
        @mouseleave="hoveredText = null"
      >
        {{ rule.name }}
      </div>
      <div 
        class="rule-description expandable-text" 
        :class="{ expanded: hoveredText === 'description' }"
        @mouseenter="hoveredText = 'description'" 
        @mouseleave="hoveredText = null"
        v-if="rule.description"
      >
        {{ rule.description }}
      </div>
    </div>

    <div class="rule-cell status-cell">
      <n-tag :type="rule.enabled ? 'success' : 'default'" size="small">
        {{ rule.enabled ? '启用' : '禁用' }}
      </n-tag>
    </div>

    <div class="rule-cell direction-cell">
      <div class="direction-content">
        <n-icon
          :component="rule.direction === 'inbound' ? ArrowDownOutline : ArrowUpOutline"
          size="16"
        />
        <span>{{ rule.direction === 'inbound' ? '入站' : '出站' }}</span>
      </div>
    </div>

    <div class="rule-cell action-cell">
      <n-tag :type="rule.action === 'allow' ? 'success' : 'error'" size="small">
        {{ rule.action === 'allow' ? '允许' : '阻止' }}
      </n-tag>
    </div>

    <div class="rule-cell program-cell">
      <div 
        class="program-text expandable-text" 
        :class="{ expanded: hoveredText === 'program' }"
        @mouseenter="hoveredText = 'program'" 
        @mouseleave="hoveredText = null"
      >
        {{ rule.program }}
      </div>
    </div>

    <div class="rule-cell protocol-cell">
      <span>{{ rule.protocol }}</span>
    </div>

    <div class="rule-cell port-cell">
      <span>{{ rule.port }}</span>
    </div>

    <div class="rule-cell profiles-cell">
      <div class="profiles-container">
        <n-tag
          v-for="profile in rule.profiles"
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
        <n-button size="small" quaternary @click="$emit('edit', rule)">
          <template #icon>
            <n-icon :component="CreateOutline" size="14" />
          </template>
        </n-button>
        <n-button size="small" quaternary type="error" @click="$emit('delete', rule.name)">
          <template #icon>
            <n-icon :component="TrashOutline" size="14" />
          </template>
        </n-button>
      </div>
    </div>
  </div>
  
  <!-- 加载状态 -->
  <div v-else class="rule-item loading-item">
    <div class="rule-cell checkbox-cell">
      <n-skeleton width="16px" height="16px" />
    </div>
    <div class="rule-cell name-cell">
      <n-skeleton text :repeat="2" />
    </div>
    <div class="rule-cell status-cell">
      <n-skeleton width="40px" height="20px" />
    </div>
    <div class="rule-cell direction-cell">
      <n-skeleton width="40px" height="20px" />
    </div>
    <div class="rule-cell action-cell">
      <n-skeleton width="40px" height="20px" />
    </div>
    <div class="rule-cell program-cell">
      <n-skeleton text />
    </div>
    <div class="rule-cell protocol-cell">
      <n-skeleton width="30px" height="16px" />
    </div>
    <div class="rule-cell port-cell">
      <n-skeleton width="30px" height="16px" />
    </div>
    <div class="rule-cell profiles-cell">
      <n-skeleton width="60px" height="20px" />
    </div>
    <div class="rule-cell actions-cell">
      <n-skeleton width="60px" height="24px" />
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, watch } from 'vue'
import {
  NButton,
  NIcon,
  NTag,
  NCheckbox,
  NSkeleton,
} from 'naive-ui'
import {
  CreateOutline,
  TrashOutline,
  ArrowUpOutline,
  ArrowDownOutline,
} from '@vicons/ionicons5'
import type { DisplayRule } from '@/types/firewall'

interface VirtualItem {
  id: string
  index: number
  rule?: DisplayRule
  loading?: boolean
}

interface Props {
  virtualItem: VirtualItem
  getRule: (index: number) => DisplayRule | null
  checked: boolean
  loadRulesInRange: (startIndex: number, endIndex: number) => Promise<void>
}

const props = defineProps<Props>()
const emit = defineEmits<{
  check: [id: string, checked: boolean]
  edit: [rule: DisplayRule]
  delete: [name: string]
}>()

const rule = ref<DisplayRule | null>(null)
const loading = ref(false)
const hoveredText = ref<string | null>(null)

// 异步加载规则数据
const loadRule = async () => {
  if (rule.value) return
  
  loading.value = true
  try {
    // 首先尝试从缓存获取
    const cachedRule = props.getRule(props.virtualItem.index)
    if (cachedRule) {
      rule.value = cachedRule
      return
    }

    // 如果没有缓存，触发更保守的批量加载
    const batchSize = 10 // 减小批量大小
    const startIndex = Math.max(0, props.virtualItem.index - 2) // 只预加载前后2条
    const endIndex = Math.min(props.virtualItem.index + batchSize, props.virtualItem.index + 7) // 向后预加载7条
    
    await props.loadRulesInRange(startIndex, endIndex)
    
    // 再次尝试获取
    const loadedRule = props.getRule(props.virtualItem.index)
    if (loadedRule) {
      rule.value = loadedRule
    }
  } catch (error) {
    // 忽略取消错误
    if (error.name !== 'AbortError' && !error.message?.includes('Cancel')) {
      console.error('加载规则失败:', error)
    }
  } finally {
    loading.value = false
  }
}

// 监听 virtualItem 变化，重新加载规则
watch(
  () => props.virtualItem.index,
  () => {
    rule.value = null
    loadRule()
  },
  { immediate: true }
)

onMounted(() => {
  loadRule()
})
</script>

<style scoped>
.rule-item {
  display: flex;
  align-items: center;
  padding: 0 16px;
  height: 80px;
  border-bottom: 1px solid var(--border-secondary);
  transition: var(--transition);
  background: var(--bg-card);
  min-width: 0;
}

.rule-item:hover {
  background: var(--bg-tertiary);
}

.rule-item.selected {
  background: rgba(59, 130, 246, 0.05);
  border-left: 3px solid var(--accent-primary);
}

.rule-item:nth-child(even) {
  background: rgba(0, 0, 0, 0.02);
}

.rule-item:nth-child(odd) {
  background: var(--bg-card);
}

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

.loading-item {
  opacity: 0.7;
}

.rule-cell {
  display: flex;
  align-items: center;
  padding: 0 8px;
  min-height: 60px;
  font-size: 13px;
  flex-shrink: 0;
  overflow: hidden;
  min-width: 0;
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

.program-text {
  max-width: 100%;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  color: var(--text-primary);
}

/* 可展开文本的样式 */
.expandable-text {
  position: relative;
  cursor: pointer;
  transition: all 0.3s ease-in-out;
  z-index: 1;
}

.expandable-text.expanded {
  position: absolute; /* 绝对定位，脱离文档流 */
  z-index: 999; /* 置于最高层 */
  background: var(--bg-card);
  border: 1px solid var(--border-primary);
  border-radius: 6px;
  padding: 6px 10px;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
  white-space: normal;
  word-wrap: break-word;
  max-width: 400px; /* 展开后的最大宽度 */
  min-width: 200px; /* 展开后的最小宽度 */
  transform: scale(1.02); /* 轻微放大效果 */
  animation: expandIn 0.2s ease-out;
}

@keyframes expandIn {
  from {
    opacity: 0;
    transform: scale(0.95);
  }
  to {
    opacity: 1;
    transform: scale(1.02);
  }
}

/* 规则名称展开样式 */
.rule-name.expanded {
  font-weight: 600;
  background: linear-gradient(135deg, var(--bg-card) 0%, rgba(59, 130, 246, 0.05) 100%);
  border-color: var(--accent-primary);
  color: var(--text-primary);
  top: -2px;
  left: -4px;
}

/* 规则描述展开样式 */
.rule-description.expanded {
  background: linear-gradient(135deg, var(--bg-secondary) 0%, rgba(156, 163, 175, 0.05) 100%);
  border-color: var(--border-hover);
  color: var(--text-secondary);
  top: 2px;
  left: -4px;
}

/* 程序文本展开样式 */
.program-text.expanded {
  background: linear-gradient(135deg, var(--bg-card) 0%, rgba(59, 130, 246, 0.03) 100%);
  border-color: var(--accent-info);
  color: var(--text-primary);
  font-family: 'Consolas', 'Monaco', 'Courier New', monospace; /* 程序路径使用等宽字体 */
  font-size: 13px;
  top: -2px;
  left: -4px;
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

/* 响应式调整 */
@media (max-width: 1200px) {
  .expandable-text.expanded {
    max-width: 300px;
  }
}

@media (max-width: 768px) {
  .expandable-text.expanded {
    max-width: 250px;
    font-size: 12px;
  }
  
  .program-text.expanded {
    font-size: 11px;
  }
}

/* 暗色模式支持 */
@media (prefers-color-scheme: dark) {
  .expandable-text.expanded {
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.3);
  }
  
  .rule-name.expanded {
    background: linear-gradient(135deg, var(--bg-card) 0%, rgba(59, 130, 246, 0.08) 100%);
  }
  
  .rule-description.expanded {
    background: linear-gradient(135deg, var(--bg-secondary) 0%, rgba(156, 163, 175, 0.08) 100%);
  }
  
  .program-text.expanded {
    background: linear-gradient(135deg, var(--bg-card) 0%, rgba(59, 130, 246, 0.05) 100%);
  }
}
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

@media (max-width: 768px) {
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
</style>