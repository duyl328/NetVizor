<template>
  <div class="software-ranking-list">
    <div class="ranking-header">
      <div class="search-box">
        <n-input
          v-model:value="searchQuery"
          placeholder="搜索软件..."
          size="small"
          clearable
        >
          <template #prefix>
            <n-icon :component="SearchOutline" />
          </template>
        </n-input>
      </div>
    </div>
    
    <div class="ranking-list" ref="listContainer">
      <div 
        v-for="(item, index) in filteredData" 
        :key="item.processName"
        :class="['ranking-item', { 'selected': selectedSoftware?.processName === item.processName }]"
        @click="selectSoftware(item)"
      >
        <div class="rank-badge" :class="getRankClass(item.rank)">
          {{ item.rank }}
        </div>
        
        <div class="software-icon">
          <div class="icon-placeholder">
            {{ item.displayName.charAt(0).toUpperCase() }}
          </div>
        </div>
        
        <div class="software-info">
          <div class="software-name">{{ item.displayName }}</div>
          <div class="software-details">
            <span class="process-name">{{ item.processName }}</span>
            <span class="connection-count">{{ item.connectionCount }} 个连接</span>
          </div>
        </div>
        
        <div class="traffic-info">
          <div class="traffic-size">{{ formatBytes(item.totalBytes) }}</div>
          <div class="traffic-percentage">{{ item.percentage.toFixed(1) }}%</div>
          <div class="traffic-bar">
            <div 
              class="traffic-fill" 
              :style="{ width: Math.min(item.percentage, 100) + '%' }"
            ></div>
          </div>
        </div>
      </div>
      
      <!-- 加载更多指示器 -->
      <div v-if="hasMore" class="load-more" @click="loadMore">
        <n-button text size="small">
          加载更多...
        </n-button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, nextTick } from 'vue'
import { NInput, NIcon, NButton } from 'naive-ui'
import { SearchOutline } from '@vicons/ionicons5'

// 接口定义
interface SoftwareRankingItem {
  rank: number
  processName: string
  displayName: string
  processPath?: string
  totalBytes: number
  uploadBytes?: number
  downloadBytes?: number
  percentage: number
  connectionCount: number
}

// Props
const props = defineProps<{
  data: SoftwareRankingItem[]
  timeRange: string
  selectedSoftware?: SoftwareRankingItem | null
}>()

// Emits
const emit = defineEmits<{
  selectSoftware: [software: SoftwareRankingItem]
}>()

// 响应式数据
const searchQuery = ref('')
const listContainer = ref<HTMLElement>()
const displayCount = ref(20) // 初始显示20个
const hasMore = computed(() => displayCount.value < filteredData.value.length)

// 搜索过滤
const filteredData = computed(() => {
  let filtered = props.data
  
  if (searchQuery.value) {
    const query = searchQuery.value.toLowerCase()
    filtered = filtered.filter(item => 
      item.displayName.toLowerCase().includes(query) ||
      item.processName.toLowerCase().includes(query)
    )
  }
  
  return filtered.slice(0, displayCount.value)
})

// 选择软件
const selectSoftware = (software: SoftwareRankingItem) => {
  emit('selectSoftware', software)
}

// 加载更多
const loadMore = () => {
  displayCount.value += 20
}

// 获取排名样式类
const getRankClass = (rank: number): string => {
  if (rank <= 3) return 'top-three'
  if (rank <= 10) return 'top-ten'
  return 'normal'
}

// 格式化字节数
const formatBytes = (bytes: number): string => {
  if (bytes === 0) return '0 B'
  
  const k = 1024
  const sizes = ['B', 'KB', 'MB', 'GB', 'TB']
  const i = Math.floor(Math.log(bytes) / Math.log(k))
  
  return `${parseFloat((bytes / Math.pow(k, i)).toFixed(1))} ${sizes[i]}`
}

// 重置显示数量当时间范围改变时
watch(() => props.timeRange, () => {
  displayCount.value = 20
  nextTick(() => {
    if (listContainer.value) {
      listContainer.value.scrollTop = 0
    }
  })
})

// 重置显示数量当搜索查询改变时
watch(searchQuery, () => {
  displayCount.value = 20
})
</script>

<style scoped>
.software-ranking-list {
  height: 100%;
  display: flex;
  flex-direction: column;
}

.ranking-header {
  padding: 16px;
  border-bottom: 1px solid var(--border-secondary);
  flex-shrink: 0;
}

.search-box {
  width: 100%;
}

.ranking-list {
  flex: 1;
  overflow-y: auto;
  padding: 8px;
}

.ranking-item {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 12px;
  border-radius: 8px;
  border: 1px solid transparent;
  transition: all 0.2s ease;
  cursor: pointer;
  margin-bottom: 4px;
}

.ranking-item:hover {
  background: var(--bg-hover);
  border-color: var(--border-hover);
}

.ranking-item.selected {
  background: var(--bg-selected);
  border-color: var(--accent-primary);
}

.rank-badge {
  width: 28px;
  height: 28px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 12px;
  font-weight: 700;
  flex-shrink: 0;
}

.rank-badge.top-three {
  background: linear-gradient(135deg, #ffd700, #ffed4e);
  color: #92400e;
  box-shadow: 0 0 10px rgba(255, 215, 0, 0.3);
}

.rank-badge.top-ten {
  background: linear-gradient(135deg, #c0c0c0, #e5e7eb);
  color: #374151;
}

.rank-badge.normal {
  background: var(--bg-tertiary);
  color: var(--text-muted);
}

.software-icon {
  flex-shrink: 0;
}

.icon-placeholder {
  width: 36px;
  height: 36px;
  border-radius: 8px;
  background: linear-gradient(135deg, var(--accent-primary), var(--accent-secondary));
  display: flex;
  align-items: center;
  justify-content: center;
  color: white;
  font-weight: 600;
  font-size: 14px;
}

.software-info {
  flex: 1;
  min-width: 0;
}

.software-name {
  font-size: 14px;
  font-weight: 600;
  color: var(--text-primary);
  margin-bottom: 4px;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.software-details {
  display: flex;
  flex-direction: column;
  gap: 2px;
  font-size: 11px;
  color: var(--text-muted);
}

.process-name {
  font-family: 'JetBrains Mono', 'Fira Code', monospace;
}

.traffic-info {
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  gap: 4px;
  min-width: 80px;
}

.traffic-size {
  font-size: 13px;
  font-weight: 600;
  color: var(--text-primary);
  font-family: 'JetBrains Mono', 'Fira Code', monospace;
}

.traffic-percentage {
  font-size: 11px;
  color: var(--text-muted);
}

.traffic-bar {
  width: 60px;
  height: 4px;
  background: var(--bg-tertiary);
  border-radius: 2px;
  overflow: hidden;
}

.traffic-fill {
  height: 100%;
  background: linear-gradient(90deg, var(--accent-primary), var(--accent-secondary));
  border-radius: 2px;
  transition: width 0.3s ease;
}

.load-more {
  display: flex;
  justify-content: center;
  padding: 16px;
  border-top: 1px solid var(--border-secondary);
  margin-top: 8px;
}

/* 滚动条样式 */
.ranking-list::-webkit-scrollbar {
  width: 6px;
}

.ranking-list::-webkit-scrollbar-track {
  background: var(--bg-tertiary);
  border-radius: 3px;
}

.ranking-list::-webkit-scrollbar-thumb {
  background: var(--border-secondary);
  border-radius: 3px;
}

.ranking-list::-webkit-scrollbar-thumb:hover {
  background: var(--border-hover);
}
</style>