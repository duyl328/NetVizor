<template>
  <div class="software-ranking-list">
    <div class="ranking-header">
      <div class="header-top">
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
        <div class="sort-controls">
          <n-select
            v-model:value="sortBy"
            size="small"
            :options="sortOptions"
            placeholder="排序方式"
            style="width: 120px"
          />
          <n-button
            size="small"
            :type="sortOrder === 'desc' ? 'primary' : 'default'"
            @click="toggleSortOrder"
          >
            <template #icon>
              <n-icon :component="sortOrder === 'desc' ? ArrowDownOutline : ArrowUpOutline" />
            </template>
          </n-button>
        </div>
      </div>

      <!-- 列表头部标题 -->
      <div class="list-headers">
        <!-- 左侧排名和图标区域 -->
        <div class="header-left">
          <div class="rank-header-cell">#</div>
          <div class="icon-header-cell"></div>
        </div>
        
        <!-- 中间软件信息区域 -->
        <div class="header-middle">
          <div class="app-header-cell">软件信息</div>
        </div>
        
        <!-- 右侧统计数据区域 -->
        <div class="header-right">
          <div class="stats-headers">
            <div class="stat-header-cell">总流量</div>
            <div class="stat-header-cell">上传</div>
            <div class="stat-header-cell">连接数</div>
            <div class="stat-header-cell">占比</div>
          </div>
        </div>
      </div>
    </div>

    <div class="list-container" ref="listContainer" :style="{ height: containerHeight }">
      <div class="software-list">
        <div
          v-for="item in filteredAndSortedData"
          :key="item.appId"
          class="software-item"
          @click="selectSoftware(item)"
        >
          <!-- 左侧排名和图标 -->
          <div class="item-left">
            <div class="rank-badge" :class="getRankClass(item.rank)">
              {{ item.rank }}
            </div>
            <div class="app-icon-container">
              <img
                v-if="item.icon"
                :src="`data:image/png;base64,${item.icon}`"
                class="app-icon"
                :alt="item.displayName"
              />
              <div
                v-else
                class="app-icon-placeholder"
                :style="getGradientColor(item.displayName.charAt(0).toUpperCase())"
              >
                {{ item.displayName.charAt(0).toUpperCase() }}
              </div>
            </div>
          </div>

          <!-- 中间软件信息 -->
          <div class="item-middle">
            <div class="app-main-info">
              <div class="app-name-line">
                <span class="app-name">{{ item.displayName }}</span>
                <span v-if="item.version" class="version">v{{ item.version }}</span>
              </div>
              <div class="app-meta-line">
                <span class="process-name">{{ item.processName }}</span>
                <span v-if="item.company" class="company">{{ item.company }}</span>
              </div>
            </div>
            <div class="app-path-line" v-if="item.processPath">
              <span class="path-label">路径:</span>
              <span class="path-value">{{ item.processPath }}</span>
            </div>
          </div>

          <!-- 右侧统计数据 -->
          <div class="item-right">
            <div class="stats-grid">
              <div class="stat-item">
                <div class="stat-value primary">{{ formatBytes(item.totalBytes) }}</div>
                <div class="stat-label">总流量</div>
              </div>
              <div class="stat-item">
                <div class="stat-value">{{ formatBytes(item.uploadBytes || 0) }}</div>
                <div class="stat-label">上传</div>
              </div>
              <div class="stat-item">
                <div class="stat-value">{{ item.connectionCount }}</div>
                <div class="stat-label">连接数</div>
              </div>
              <div class="stat-item">
                <div class="stat-value percent">{{ item.percentage.toFixed(1) }}%</div>
                <div class="stat-label">占比</div>
                <div class="percentage-bar">
                  <div
                    class="percentage-fill"
                    :style="{ width: `${Math.min(item.percentage, 100)}%` }"
                  ></div>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- 空状态 -->
        <div v-if="filteredAndSortedData.length === 0" class="empty-state">
          <div class="empty-icon">📊</div>
          <div class="empty-text">{{ searchQuery ? '未找到匹配的软件' : '暂无数据' }}</div>
        </div>
      </div>

      <!-- 数据统计信息 -->
      <div class="data-info">
        <span>共 {{ filteredAndSortedData.length }} 条数据</span>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, nextTick } from 'vue'
import { NInput, NIcon, NButton, NSelect } from 'naive-ui'
import { SearchOutline, ArrowUpOutline, ArrowDownOutline } from '@vicons/ionicons5'
import { getGradientColor } from '@/utils/colorUtils'

// 接口定义
interface SoftwareRankingItem {
  rank: number
  appId: string
  processName: string
  displayName: string
  processPath?: string
  icon?: string
  version?: string
  company?: string
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
}>()

// Emits
const emit = defineEmits<{
  selectSoftware: [software: SoftwareRankingItem]
}>()

// 响应式数据
const searchQuery = ref('')
const listContainer = ref<HTMLElement>()
const sortBy = ref('totalBytes')
const sortOrder = ref<'asc' | 'desc'>('desc')

// 排序选项
const sortOptions = [
  { label: '总流量', value: 'totalBytes' },
  { label: '上传流量', value: 'uploadBytes' },
  { label: '连接数', value: 'connectionCount' },
  { label: '占比', value: 'percentage' },
  { label: '排名', value: 'rank' }
]

// 计算表格容器高度
const containerHeight = computed(() => {
  if (typeof window === 'undefined') return '400px'
  const windowHeight = window.innerHeight
  // 大屏幕下使用更多高度，小屏幕下保持400px
  if (windowHeight >= 900) {
    return Math.max(windowHeight - 500, 400) + 'px'
  }
  return '400px'
})

// 切换排序顺序
const toggleSortOrder = () => {
  sortOrder.value = sortOrder.value === 'desc' ? 'asc' : 'desc'
}

// 搜索和排序
const filteredAndSortedData = computed(() => {
  let filtered = props.data

  // 搜索过滤
  if (searchQuery.value) {
    const query = searchQuery.value.toLowerCase()
    filtered = filtered.filter(item =>
      item.displayName.toLowerCase().includes(query) ||
      item.processName.toLowerCase().includes(query) ||
      (item.company && item.company.toLowerCase().includes(query))
    )
  }

  // 排序
  const sorted = [...filtered].sort((a, b) => {
    let aValue = a[sortBy.value as keyof SoftwareRankingItem] as number
    let bValue = b[sortBy.value as keyof SoftwareRankingItem] as number

    // 处理可能为 undefined 的值
    if (sortBy.value === 'uploadBytes') {
      aValue = a.uploadBytes || 0
      bValue = b.uploadBytes || 0
    }

    if (sortOrder.value === 'desc') {
      return bValue - aValue
    } else {
      return aValue - bValue
    }
  })

  return sorted
})

// 选择软件
const selectSoftware = (software: SoftwareRankingItem) => {
  emit('selectSoftware', software)
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

// 重置滚动位置当时间范围改变时
watch(() => props.timeRange, () => {
  nextTick(() => {
    if (listContainer.value) {
      listContainer.value.scrollTop = 0
    }
  })
})
</script>

<style scoped>
.software-ranking-list {
  height: 100%;
  display: flex;
  flex-direction: column;
  background: var(--bg-card);
}

.ranking-header {
  padding: 16px;
  border-bottom: 1px solid var(--border-secondary);
  flex-shrink: 0;
  background: var(--bg-card);
}

.header-top {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 16px;
  gap: 16px;
}

.search-box {
  flex: 1;
  min-width: 200px;
}

.sort-controls {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-shrink: 0;
}

.list-headers {
  display: flex;
  align-items: center;
  padding: 8px 20px;
  background: var(--bg-tertiary);
  border-radius: 6px;
  font-size: 11px;
  font-weight: 600;
  color: var(--text-muted);
  text-transform: uppercase;
  letter-spacing: 0.3px;
  margin-bottom: 8px;
}

.header-left {
  display: flex;
  align-items: center;
  gap: 12px;
  flex-shrink: 0;
}

.rank-header-cell {
  width: 28px;
  text-align: center;
}

.icon-header-cell {
  width: 32px;
}

.header-middle {
  flex: 1;
  display: flex;
  align-items: center;
  margin-left: 12px;
  padding-right: 12px;
}

.app-header-cell {
  color: var(--text-secondary);
}

.header-right {
  flex-shrink: 0;
  margin-left: 8px;
}

.stats-headers {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 12px;
  align-items: center;
}

.stat-header-cell {
  display: flex;
  align-items: center;
  justify-content: center;
  min-width: 55px;
  text-align: center;
}

.list-container {
  flex: 1;
  overflow-y: auto;
  padding: 8px;
}

.software-list {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.software-item {
  display: flex;
  align-items: center;
  padding: 12px 20px;
  background: var(--bg-card);
  border: 1px solid var(--border-primary);
  border-radius: 8px;
  cursor: pointer;
  transition: all 0.2s ease;
  position: relative;
  overflow: hidden;
  min-height: 80px;
}

.software-item:hover {
  background: var(--bg-hover);
  border-color: var(--border-hover);
  transform: translateY(-1px);
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
}

.item-left {
  display: flex;
  align-items: center;
  gap: 12px;
  flex-shrink: 0;
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
  box-shadow: 0 2px 12px rgba(255, 215, 0, 0.4);
}

.rank-badge.top-ten {
  background: linear-gradient(135deg, #c0c0c0, #e5e7eb);
  color: #374151;
  box-shadow: 0 2px 8px rgba(192, 192, 192, 0.3);
}

.rank-badge.normal {
  background: var(--bg-tertiary);
  color: var(--text-muted);
  border: 1px solid var(--border-secondary);
}

.app-icon-container {
  flex-shrink: 0;
}

.app-icon {
  width: 32px;
  height: 32px;
  border-radius: 6px;
  object-fit: contain;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.app-icon-placeholder {
  width: 32px;
  height: 32px;
  border-radius: 6px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 14px;
  font-weight: 700;
  color: white;
  text-shadow: 0 1px 2px rgba(0, 0, 0, 0.2);
  box-shadow: 0 2px 8px rgba(59, 130, 246, 0.3);
}

.item-middle {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 6px;
  min-width: 0;
  margin-left: 12px;
  padding-right: 12px;
}

.app-main-info {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.app-name-line {
  display: flex;
  align-items: center;
  gap: 8px;
}

.app-name {
  font-size: 15px;
  font-weight: 600;
  color: var(--text-primary);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  flex: 1;
  min-width: 0;
}

.version {
  color: var(--text-muted);
  background: var(--bg-quaternary);
  padding: 2px 6px;
  border-radius: 4px;
  font-size: 10px;
  font-weight: 500;
  flex-shrink: 0;
}

.app-meta-line {
  display: flex;
  align-items: center;
  gap: 12px;
  font-size: 12px;
}

.process-name {
  color: var(--text-secondary);
  font-weight: 500;
  font-family: 'Consolas', monospace;
}

.company {
  color: var(--text-muted);
  font-weight: 400;
}

.app-path-line {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 11px;
  line-height: 1.3;
}

.path-label {
  color: var(--text-muted);
  font-weight: 500;
  flex-shrink: 0;
}

.path-value {
  color: var(--text-muted);
  font-family: 'Consolas', monospace;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  flex: 1;
  min-width: 0;
}

.item-right {
  flex-shrink: 0;
  margin-left: 8px;
}

.stats-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 12px;
  align-items: center;
}

.stat-item {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 2px;
  min-width: 55px;
}

.stat-value {
  font-size: 13px;
  font-weight: 600;
  color: var(--text-primary);
  white-space: nowrap;
}

.stat-value.primary {
  color: var(--accent-primary);
  font-weight: 700;
  font-size: 14px;
}

.stat-value.percent {
  color: var(--accent-success);
}

.stat-label {
  font-size: 9px;
  color: var(--text-muted);
  text-align: center;
  text-transform: uppercase;
  letter-spacing: 0.3px;
  line-height: 1.2;
}

.percentage-bar {
  width: 36px;
  height: 3px;
  background: var(--bg-tertiary);
  border-radius: 2px;
  overflow: hidden;
  margin-top: 1px;
}

.percentage-fill {
  height: 100%;
  background: linear-gradient(90deg, var(--accent-success), #10b981);
  border-radius: 2px;
  transition: width 0.5s ease;
}

.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 60px 20px;
  color: var(--text-muted);
}

.empty-icon {
  font-size: 48px;
  margin-bottom: 16px;
  opacity: 0.6;
}

.empty-text {
  font-size: 14px;
  color: var(--text-secondary);
}

.data-info {
  display: flex;
  justify-content: center;
  padding: 12px;
  border-top: 1px solid var(--border-secondary);
  margin-top: 8px;
  font-size: 12px;
  color: var(--text-muted);
  background: var(--bg-tertiary);
  border-radius: 6px;
}

/* 响应式设计 */
@media (max-width: 1024px) {
  .stats-grid {
    grid-template-columns: repeat(2, 1fr);
    gap: 8px;
  }
  
  .stats-headers {
    grid-template-columns: repeat(2, 1fr);
    gap: 8px;
  }

  .item-middle {
    margin-left: 10px;
    padding-right: 8px;
  }
  
  .header-middle {
    margin-left: 10px;
    padding-right: 8px;
  }

  .item-right {
    margin-left: 6px;
  }
  
  .header-right {
    margin-left: 6px;
  }

  .stat-item {
    min-width: 45px;
  }
  
  .stat-header-cell {
    min-width: 45px;
  }
}

@media (max-width: 768px) {
  .header-top {
    flex-direction: column;
    align-items: stretch;
    gap: 12px;
  }

  .sort-controls {
    justify-content: flex-end;
  }

  .list-headers {
    display: none;
  }

  .software-item {
    flex-direction: column;
    align-items: stretch;
    gap: 12px;
    padding: 12px;
    min-height: auto;
  }

  .item-left {
    align-self: flex-start;
  }

  .item-middle {
    margin-left: 0;
    padding-right: 0;
  }

  .item-right {
    margin-left: 0;
  }

  .stats-grid {
    grid-template-columns: repeat(4, 1fr);
    gap: 8px;
  }

  .stat-item {
    min-width: auto;
  }

  .app-path-line {
    font-size: 10px;
  }
}
</style>
