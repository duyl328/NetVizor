<template>
  <div class="top-apps-chart">
    <v-chart 
      ref="chartRef"
      class="chart"
      :option="chartOption"
      :autoresize="true"
    />
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import VChart from 'vue-echarts'
import { use } from 'echarts/core'
import { CanvasRenderer } from 'echarts/renderers'
import { PieChart } from 'echarts/charts'
import {
  TitleComponent,
  TooltipComponent,
  LegendComponent
} from 'echarts/components'
import type { EChartsOption } from 'echarts'

// 注册ECharts组件
use([
  CanvasRenderer,
  PieChart,
  TitleComponent,
  TooltipComponent,
  LegendComponent
])

// Props定义
interface TopAppData {
  processName: string
  displayName: string
  totalBytes: number
  percentage: number
}

const props = defineProps<{
  data: TopAppData[]
  timeRange: string
}>()

// 生成玫瑰图配置
const chartOption = computed<EChartsOption>(() => {
  const topData = props.data.slice(0, 10) // 只显示前10个
  
  return {
    tooltip: {
      trigger: 'item',
      formatter: (params: any) => {
        const data = params.data
        return `
          <div style="margin-bottom: 4px; font-weight: bold;">${data.name}</div>
          <div>流量占比: ${data.value}%</div>
          <div>流量大小: ${formatBytes(data.totalBytes)}</div>
        `
      },
      backgroundColor: 'rgba(0, 0, 0, 0.8)',
      borderColor: 'rgba(255, 255, 255, 0.1)',
      textStyle: {
        color: '#ffffff',
        fontSize: 12
      }
    },
    series: [
      {
        name: 'Top应用流量',
        type: 'pie',
        radius: ['30%', '70%'],
        center: ['50%', '50%'],
        roseType: 'area', // 玫瑰图
        itemStyle: {
          borderRadius: 2,
          borderColor: '#fff',
          borderWidth: 1
        },
        label: {
          show: true,
          position: 'outside',
          formatter: '{b}\n{d}%',
          fontSize: 10,
          color: 'var(--text-secondary)'
        },
        labelLine: {
          show: true,
          length: 15,
          length2: 8,
          lineStyle: {
            color: 'var(--border-secondary)'
          }
        },
        data: topData.map((item, index) => ({
          value: item.percentage,
          name: item.displayName,
          totalBytes: item.totalBytes,
          itemStyle: {
            color: getColorByIndex(index)
          }
        })),
        animationType: 'scale',
        animationEasing: 'elasticOut',
        animationDelayUpdate: (idx: number) => idx * 50
      }
    ]
  }
})

// 格式化字节数
const formatBytes = (bytes: number): string => {
  if (bytes === 0) return '0 B'
  
  const k = 1024
  const sizes = ['B', 'KB', 'MB', 'GB', 'TB']
  const i = Math.floor(Math.log(bytes) / Math.log(k))
  
  return `${parseFloat((bytes / Math.pow(k, i)).toFixed(1))} ${sizes[i]}`
}

// 根据索引获取颜色
const getColorByIndex = (index: number): string => {
  const colors = [
    '#3b82f6', '#10b981', '#f59e0b', '#ef4444', '#8b5cf6',
    '#06b6d4', '#84cc16', '#f97316', '#ec4899', '#6366f1'
  ]
  return colors[index % colors.length]
}
</script>

<style scoped>
.top-apps-chart {
  width: 100%;
  height: 100%;
  min-height: 240px;
}

.chart {
  width: 100%;
  height: 100%;
}
</style>