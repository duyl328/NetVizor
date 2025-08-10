<template>
  <div class="protocol-chart">
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
interface ProtocolData {
  protocol: string
  bytes: number
  percentage: number
  color: string
}

const props = defineProps<{
  data: ProtocolData[]
}>()

// 生成饼图配置
const chartOption = computed<EChartsOption>(() => {
  return {
    tooltip: {
      trigger: 'item',
      formatter: (params: any) => {
        const data = params.data
        return `
          <div style="margin-bottom: 4px; font-weight: bold;">${data.name}</div>
          <div>占比: ${data.value}%</div>
          <div>流量: ${formatBytes(data.bytes)}</div>
        `
      },
      backgroundColor: 'rgba(0, 0, 0, 0.8)',
      borderColor: 'rgba(255, 255, 255, 0.1)',
      textStyle: {
        color: '#ffffff',
        fontSize: 12
      }
    },
    legend: {
      orient: 'vertical',
      right: 10,
      top: 20,
      textStyle: {
        color: 'var(--text-secondary)',
        fontSize: 11
      },
      formatter: (name: string) => {
        const item = props.data.find(d => d.protocol === name)
        return `${name} ${item?.percentage.toFixed(1)}%`
      }
    },
    series: [
      {
        name: '协议分布',
        type: 'pie',
        radius: ['40%', '70%'],
        center: ['35%', '50%'],
        data: props.data.map(item => ({
          value: item.percentage,
          name: item.protocol,
          bytes: item.bytes,
          itemStyle: {
            color: item.color,
            borderWidth: 2,
            borderColor: '#fff'
          }
        })),
        label: {
          show: false
        },
        labelLine: {
          show: false
        },
        animationType: 'scale',
        animationEasing: 'elasticOut',
        animationDelayUpdate: (idx: number) => idx * 100
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
</script>

<style scoped>
.protocol-chart {
  width: 100%;
  height: 200px;
  min-height: 200px;
}

.chart {
  width: 100%;
  height: 100%;
}
</style>