<template>
  <div class="traffic-chart">
    <div class="chart-header">
      <span class="chart-title">实时流量</span>
      <span class="chart-value">{{ currentSpeed }}</span>
    </div>
    <div class="chart-container">
      <v-chart
        ref="chartRef"
        class="echarts-container"
        :option="chartOption"
        :autoresize="true"
        :theme="chartTheme"
      />
    </div>
    <div class="chart-footer">
      <span class="chart-label">{{ timeRange }}</span>
      <div class="chart-legend">
        <span class="legend-item upload">
          <span class="legend-dot upload"></span>
          上传: {{ formatSpeed(currentUploadSpeed) }}
        </span>
        <span class="legend-item download">
          <span class="legend-dot download"></span>
          下载: {{ formatSpeed(currentDownloadSpeed) }}
        </span>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, watch, nextTick } from 'vue'
import VChart from 'vue-echarts'
import { use, getInstanceByDom } from 'echarts/core'
import { CanvasRenderer } from 'echarts/renderers'
import { BarChart } from 'echarts/charts'
import {
  TitleComponent,
  TooltipComponent,
  LegendComponent,
  GridComponent,
  DataZoomComponent,
  ToolboxComponent
} from 'echarts/components'
import type { EChartsOption } from 'echarts'
import { convertFileSize } from '@/utils/fileUtil'
import { FILE_SIZE_UNIT_ENUM } from '@/constants/enums'

// 注册必需的组件
use([
  CanvasRenderer,
  BarChart,
  TitleComponent,
  TooltipComponent,
  LegendComponent,
  GridComponent,
  DataZoomComponent,
  ToolboxComponent
])

// 数据点管理常量
const MAX_POINTS = 300 // 最大数据点数（5分钟）
const DEFAULT_SHOW = 30 // 默认显示数量
const MAX_SHOW = 60 // 最大显示数量
const MIN_SHOW = 30 // 最小显示数量

// Props接口定义
interface TrafficDataPoint {
  timestamp: number
  uploadSpeed: number
  downloadSpeed: number
}

// Props
const props = defineProps<{
  data?: TrafficDataPoint[]
  maxDataPoints?: number
}>()

// 默认props值
const maxDataPoints = computed(() => props.maxDataPoints || 60) // 默认60个数据点（约20分钟，每20秒一个点）

// 组件引用
const chartRef = ref()

// 图表主题（深色模式适配）
const chartTheme = ref('light')

// 当前网速数据
const currentUploadSpeed = computed(() => {
  const data = props.data || []
  return data.length > 0 ? data[data.length - 1].uploadSpeed : 0
})

const currentDownloadSpeed = computed(() => {
  const data = props.data || []
  return data.length > 0 ? data[data.length - 1].downloadSpeed : 0
})

const currentSpeed = computed(() => {
  const total = currentUploadSpeed.value + currentDownloadSpeed.value
  return formatSpeed(total)
})

// 时间范围
const timeRange = computed(() => {
  const minutes = Math.ceil((maxDataPoints.value * 20) / 60) // 20秒一个点
  return `最近 ${minutes} 分钟`
})

// 格式化速度
const formatSpeed = (bytesPerSecond: number): string => {
  if (bytesPerSecond === 0) return '0 B/s'
  const result = convertFileSize(bytesPerSecond, FILE_SIZE_UNIT_ENUM.B)
  return `${result.size}${result.unit}/s`
}

// 计算缩放范围
const getZoomRange = (currentPoints: number) => {
  if (currentPoints <= DEFAULT_SHOW) {
    return { start: 0, end: 100 }
  }
  const spanPercent = (DEFAULT_SHOW / currentPoints) * 100
  return { start: 100 - spanPercent, end: 100 }
}

// 生成时间标签
const generateTimeLabels = (dataLength: number): string[] => {
  const labels: string[] = []
  const now = Date.now()

  for (let i = dataLength - 1; i >= 0; i--) {
    const time = new Date(now - i * 1000) // 每秒一个点
    const minutes = time.getMinutes().toString().padStart(2, '0')
    const seconds = time.getSeconds().toString().padStart(2, '0')
    labels.push(`${minutes}:${seconds}`)
  }

  return labels
}

// ECharts配置
const chartOption = computed<EChartsOption>(() => {
  const data = props.data || []
  const limitedData = data.slice(-maxDataPoints.value) // 只取最近的数据点

  // 准备数据
  const uploadData = limitedData.map(d => +(d.uploadSpeed / 1024 / 1024).toFixed(2))
  const downloadData = limitedData.map(d => +(d.downloadSpeed / 1024 / 1024).toFixed(2))
  const timeLabels = generateTimeLabels(limitedData.length)

  return {
    animation: true,
    animationDuration: 1000,
    animationEasing: 'elasticOut',
    animationDelayUpdate: (idx: number) => idx * 25,
    backgroundColor: 'transparent',
    tooltip: {
      trigger: 'axis',
      axisPointer: {
        type: 'shadow'
      },
      formatter: (params: unknown[]) => {
        const time = params[0]?.axisValue || ''
        let tooltip = `<div style="margin-bottom: 4px; font-weight: bold;">${time}</div>`

        params.forEach((param) => {
          const color = param.color
          const seriesName = param.seriesName
          const value = param.value
          tooltip += `
            <div style="margin-bottom: 2px;">
              <span style="display:inline-block;margin-right:4px;border-radius:50%;width:10px;height:10px;background-color:${color};"></span>
              ${seriesName}: ${value} MB/s
            </div>
          `
        })

        return tooltip
      },
      backgroundColor: 'rgba(0, 0, 0, 0.8)',
      borderColor: 'rgba(255, 255, 255, 0.2)',
      textStyle: {
        color: '#ffffff',
        fontSize: 12
      }
    },
    legend: {
      show: false // 我们使用自定义图例
    },
    grid: {
      top: 10,
      left: 10,
      right: 10,
      bottom: 20,
      containLabel: false
    },
    xAxis: {
      type: 'category',
      data: timeLabels,
      show: false // 隐藏x轴，节省空间
    },
    yAxis: {
      type: 'value',
      show: false, // 隐藏y轴，节省空间
      min: 0
    },
    series: [
      {
        name: '上传',
        type: 'bar',
        data: uploadData,
        itemStyle: {
          color: {
            type: 'linear',
            x: 0,
            y: 0,
            x2: 0,
            y2: 1,
            colorStops: [
              {
                offset: 0,
                color: '#10b981' // 绿色 - 顶部
              },
              {
                offset: 1,
                color: '#059669' // 深绿色 - 底部
              }
            ]
          },
          borderRadius: [2, 2, 0, 0]
        },
        emphasis: {
          itemStyle: {
            color: {
              type: 'linear',
              x: 0,
              y: 0,
              x2: 0,
              y2: 1,
              colorStops: [
                {
                  offset: 0,
                  color: '#34d399'
                },
                {
                  offset: 1,
                  color: '#10b981'
                }
              ]
            }
          }
        },
        animationDelay: (idx: number) => idx * 10, // bar-animation-delay效果
      },
      {
        name: '下载',
        type: 'bar',
        data: downloadData,
        itemStyle: {
          color: {
            type: 'linear',
            x: 0,
            y: 0,
            x2: 0,
            y2: 1,
            colorStops: [
              {
                offset: 0,
                color: '#3b82f6' // 蓝色 - 顶部
              },
              {
                offset: 1,
                color: '#1d4ed8' // 深蓝色 - 底部
              }
            ]
          },
          borderRadius: [2, 2, 0, 0]
        },
        emphasis: {
          itemStyle: {
            color: {
              type: 'linear',
              x: 0,
              y: 0,
              x2: 0,
              y2: 1,
              colorStops: [
                {
                  offset: 0,
                  color: '#60a5fa'
                },
                {
                  offset: 1,
                  color: '#3b82f6'
                }
              ]
            }
          }
        },
        animationDelay: (idx: number) => idx * 10 + 100 ,// bar-animation-delay效果，稍微延迟
      }
    ]
  }
})

// 监听数据变化，更新图表
watch(
  () => props.data,
  () => {
    nextTick(() => {
      if (chartRef.value) {
        chartRef.value.resize()
      }
    })
  },
  { deep: true }
)

// 主题切换检测
const detectTheme = () => {
  const isDark = window.matchMedia('(prefers-color-scheme: dark)').matches
  chartTheme.value = isDark ? 'dark' : 'light'
}

onMounted(() => {
  detectTheme()

  // 监听主题变化
  const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)')
  mediaQuery.addEventListener('change', detectTheme)

  // 确保图表正确初始化
  nextTick(() => {
    if (chartRef.value) {
      chartRef.value.resize()
    }
  })
})

onUnmounted(() => {
  const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)')
  mediaQuery.removeEventListener('change', detectTheme)
})
</script>

<style scoped>
/* 图表容器 */
.traffic-chart {
  background: var(--bg-card);
  border-radius: 8px;
  padding: 16px;
  border: 1px solid var(--border-tertiary);
  height: 100%;
  display: flex;
  flex-direction: column;
}

/* 图表头部 */
.chart-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 12px;
  flex-shrink: 0;
}

.chart-title {
  font-size: 12px;
  color: var(--text-muted);
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.chart-value {
  font-size: 14px;
  font-weight: 700;
  color: var(--accent-secondary);
  font-family: 'JetBrains Mono', 'Fira Code', monospace;
}

/* 图表主体 */
.chart-container {
  flex: 1;
  min-height: 120px;
  margin-bottom: 12px;
  position: relative;
}

.echarts-container {
  width: 100% !important;
  height: 100% !important;
  min-height: 120px;
}

/* 图表底部 */
.chart-footer {
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-shrink: 0;
}

.chart-label {
  font-size: 11px;
  color: var(--text-muted);
}

/* 图例 */
.chart-legend {
  display: flex;
  gap: 12px;
  flex-wrap: wrap;
}

.legend-item {
  display: flex;
  align-items: center;
  gap: 4px;
  font-size: 11px;
  color: var(--text-muted);
  font-weight: 500;
}

.legend-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  flex-shrink: 0;
}

.legend-dot.upload {
  background: linear-gradient(135deg, #10b981, #059669);
  box-shadow: 0 0 4px rgba(16, 185, 129, 0.3);
}

.legend-dot.download {
  background: linear-gradient(135deg, #3b82f6, #1d4ed8);
  box-shadow: 0 0 4px rgba(59, 130, 246, 0.3);
}

/* 上传和下载颜色区分 */
.legend-item.upload {
  color: #10b981;
}

.legend-item.download {
  color: #3b82f6;
}

/* 响应式设计 */
@media (max-width: 1400px) {
  .chart-container {
    min-height: 100px;
  }

  .echarts-container {
    min-height: 100px;
  }
}

@media (max-width: 1200px) {
  .traffic-chart {
    padding: 12px;
  }

  .chart-legend {
    gap: 8px;
  }

  .legend-item {
    font-size: 10px;
  }

  .chart-header {
    margin-bottom: 8px;
  }

  .chart-container {
    margin-bottom: 8px;
    min-height: 80px;
  }

  .echarts-container {
    min-height: 80px;
  }
}

/* 暗色主题适配 */
@media (prefers-color-scheme: dark) {
  .chart-value {
    color: #60a5fa;
  }

  .legend-item.upload {
    color: #34d399;
  }

  .legend-item.download {
    color: #60a5fa;
  }

  .legend-dot.upload {
    background: linear-gradient(135deg, #34d399, #10b981);
    box-shadow: 0 0 4px rgba(52, 211, 153, 0.4);
  }

  .legend-dot.download {
    background: linear-gradient(135deg, #60a5fa, #3b82f6);
    box-shadow: 0 0 4px rgba(96, 165, 250, 0.4);
  }
}

/* ECharts自定义样式覆盖 */
:deep(.echarts-tooltip) {
  font-family: 'JetBrains Mono', 'Fira Code', monospace !important;
}
</style>
