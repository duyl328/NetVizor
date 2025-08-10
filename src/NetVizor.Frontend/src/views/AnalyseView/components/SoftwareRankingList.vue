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
    
    <div class="table-container" ref="listContainer">
      <n-data-table
        :columns="columns"
        :data="filteredData"
        :row-key="(row: SoftwareRankingItem) => `${row.processName}_${row.rank}_${row.totalBytes}`"
        :row-class-name="(row: SoftwareRankingItem) => selectedSoftware?.processName === row.processName ? 'selected-row' : ''"
        :on-update:checked-row-keys="() => {}"
        @row-click="selectSoftware"
        striped
        size="small"
        :scroll-x="800"
        :max-height="containerHeight"
      />
      
      <!-- 数据统计信息 -->
      <div class="data-info">
        <span>共 {{ filteredData.length }} 条数据</span>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, nextTick, h } from 'vue'
import { NInput, NIcon, NButton, NDataTable, NAvatar, NTag, NTooltip } from 'naive-ui'
import { SearchOutline } from '@vicons/ionicons5'
import type { DataTableColumns } from 'naive-ui'
import { getGradientColor } from '@/utils/colorUtils'

// 接口定义
interface SoftwareRankingItem {
  rank: number
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
  selectedSoftware?: SoftwareRankingItem | null
}>()

// Emits
const emit = defineEmits<{
  selectSoftware: [software: SoftwareRankingItem]
}>()

// 响应式数据
const searchQuery = ref('')
const listContainer = ref<HTMLElement>()

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

// 表格列定义
const columns: DataTableColumns<SoftwareRankingItem> = [
  {
    title: '排名',
    key: 'rank',
    width: 60,
    render: (row) => {
      const rankClass = row.rank <= 3 ? 'top-three' : row.rank <= 10 ? 'top-ten' : 'normal'
      return h('div', { class: `rank-badge ${rankClass}` }, row.rank.toString())
    }
  },
  {
    title: '图标',
    key: 'icon',
    width: 60,
    render: (row) => {
      return h('div', { class: 'icon-column' }, [
        row.icon 
          ? h('img', { 
              src: `data:image/png;base64,${row.icon}`, 
              class: 'app-icon',
              alt: row.displayName 
            })
          : h('div', { 
              class: 'app-icon-span',
              style: getGradientColor(row.displayName.charAt(0).toUpperCase())
            }, [
              h('span', row.displayName.charAt(0).toUpperCase())
            ])
      ])
    }
  },
  {
    title: '软件名称',
    key: 'displayName',
    width: 150,
    render: (row) => {
      return h('div', { class: 'software-name-cell' }, row.displayName)
    }
  },
  {
    title: '公司',
    key: 'company',
    width: 120,
    render: (row) => row.company || '-'
  },
  {
    title: '版本',
    key: 'version', 
    width: 100,
    render: (row) => row.version || '-'
  },
  {
    title: '路径',
    key: 'processPath',
    width: 200,
    ellipsis: {
      tooltip: true
    },
    render: (row) => row.processPath || '-'
  },
  {
    title: '总流量',
    key: 'totalBytes',
    width: 100,
    render: (row) => formatBytes(row.totalBytes),
    sorter: {
      compare: (a, b) => Number(a.totalBytes) - Number(b.totalBytes),
      multiple: 1
    }
  },
  {
    title: '上传',
    key: 'uploadBytes', 
    width: 100,
    render: (row) => formatBytes(row.uploadBytes || 0),
    sorter: {
      compare: (a, b) => Number(a.uploadBytes || 0) - Number(b.uploadBytes || 0),
      multiple: 1
    }
  },
  {
    title: '连接数',
    key: 'connectionCount',
    width: 80,
    render: (row) => row.connectionCount.toString(),
    sorter: {
      compare: (a, b) => Number(a.connectionCount) - Number(b.connectionCount),
      multiple: 1
    }
  },
  {
    title: '占比',
    key: 'percentage',
    width: 80,
    render: (row) => {
      return h('div', { class: 'percentage-cell' }, [
        h('div', { class: 'percentage-text' }, `${row.percentage.toFixed(1)}%`),
        h('div', { class: 'percentage-bar' }, [
          h('div', { 
            class: 'percentage-fill',
            style: { width: `${Math.min(row.percentage, 100)}%` }
          })
        ])
      ])
    },
    sorter: {
      compare: (a, b) => Number(a.percentage) - Number(b.percentage),
      multiple: 1
    }
  }
]

// 搜索过滤
const filteredData = computed(() => {
  let filtered = props.data
  
  if (searchQuery.value) {
    const query = searchQuery.value.toLowerCase()
    filtered = filtered.filter(item => 
      item.displayName.toLowerCase().includes(query) ||
      item.processName.toLowerCase().includes(query) ||
      (item.company && item.company.toLowerCase().includes(query))
    )
  }
  
  // 不再限制显示数量，显示所有数据
  return filtered
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
}

.ranking-header {
  padding: 16px;
  border-bottom: 1px solid var(--border-secondary);
  flex-shrink: 0;
}

.search-box {
  width: 100%;
}

.table-container {
  flex: 1;
  padding: 8px;
}

.data-info {
  display: flex;
  justify-content: center;
  padding: 8px;
  border-top: 1px solid var(--border-secondary);
  margin-top: 8px;
  font-size: 12px;
  color: var(--text-muted);
}

/* 表格内容样式 */
:deep(.n-data-table-td) {
  padding: 8px 12px !important;
}

:deep(.selected-row) {
  background-color: var(--bg-selected) !important;
}

.rank-badge {
  width: 24px;
  height: 24px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 11px;
  font-weight: 700;
  flex-shrink: 0;
}

.rank-badge.top-three {
  background: linear-gradient(135deg, #ffd700, #ffed4e);
  color: #92400e;
  box-shadow: 0 0 8px rgba(255, 215, 0, 0.3);
}

.rank-badge.top-ten {
  background: linear-gradient(135deg, #c0c0c0, #e5e7eb);
  color: #374151;
}

.rank-badge.normal {
  background: var(--bg-tertiary);
  color: var(--text-muted);
}

.icon-column {
  display: flex;
  justify-content: center;
  align-items: center;
}

.app-icon {
  width: 28px;
  height: 28px;
  border-radius: 6px;
  object-fit: contain;
}

.app-icon-span {
  width: 28px;
  height: 28px;
  background: linear-gradient(135deg, var(--accent-primary) 0%, #1d4ed8 100%);
  border-radius: 6px;
  display: flex;
  align-items: center;
  justify-content: center;
  box-shadow: 0 2px 8px rgba(59, 130, 246, 0.3);
}

.app-icon-span span {
  font-size: 12px;
  font-weight: 700;
  color: white;
  text-shadow: 0 1px 2px rgba(0, 0, 0, 0.2);
}

.software-name-cell {
  font-size: 13px;
  font-weight: 600;
  color: var(--text-primary);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.percentage-cell {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.percentage-text {
  font-size: 11px;
  font-weight: 600;
  color: var(--text-primary);
  text-align: center;
}

.percentage-bar {
  width: 40px;
  height: 3px;
  background: var(--bg-tertiary);
  border-radius: 2px;
  overflow: hidden;
  margin: 0 auto;
}

.percentage-fill {
  height: 100%;
  background: linear-gradient(90deg, var(--accent-primary), var(--accent-secondary));
  border-radius: 2px;
  transition: width 0.3s ease;
}

/* 表格样式覆盖 */
:deep(.n-data-table) {
  --n-border-color: var(--border-secondary);
  --n-th-color: var(--bg-tertiary);
  --n-td-color: var(--bg-card);
  --n-td-color-hover: var(--bg-hover);
  --n-text-color: var(--text-primary);
  --n-th-text-color: var(--text-secondary);
}

:deep(.n-data-table-thead) {
  background: var(--bg-secondary);
}

:deep(.n-data-table-tr:hover) {
  cursor: pointer;
}

:deep(.n-data-table-td) {
  border-bottom: 1px solid var(--border-tertiary) !important;
}
</style>