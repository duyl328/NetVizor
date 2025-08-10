<template>
  <div class="time-trend-chart">
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
import { LineChart } from 'echarts/charts'
import {
  TitleComponent,
  TooltipComponent,
  GridComponent,
  DataZoomComponent
} from 'echarts/components'
import type { EChartsOption } from 'echarts'

// 注册ECharts组件
use([
  CanvasRenderer,
  LineChart,
  TitleComponent,
  TooltipComponent,
  GridComponent,
  DataZoomComponent
])

// 接口定义
interface TimeTrendData {
  timestamp: number
  timeStr: string
  upload: number
  download: number
  connections: number
}

// Props定义
const props = defineProps<{
  data: TimeTrendData[]
}>()

// 计算图表配置
const chartOption = computed<EChartsOption>(() => {
  if (!props.data.length) {
    return {
      title: {
        text: '暂无时间趋势数据',
        left: 'center',
        top: 'center',
        textStyle: {
          color: 'var(--text-muted)',
          fontSize: 12
        }
      }
    }
  }

  const times = props.data.map(item => item.timeStr)
  const uploadData = props.data.map(item => item.upload)
  const downloadData = props.data.map(item => item.download)

  return {
    tooltip: {
      trigger: 'axis',
      axisPointer: {
        type: 'cross',
        label: {
          backgroundColor: '#6a7985'
        }
      },
      formatter: (params: any) => {
        const time = params[0].name
        let content = `<div style="font-weight: bold; margin-bottom: 4px;">${time}</div>`
        
        params.forEach((param: any) => {
          const value = formatBytes(param.value)
          const color = param.color
          content += `
            <div style="display: flex; align-items: center; gap: 6px;">
              <span style="display: inline-block; width: 8px; height: 8px; background: ${color}; border-radius: 50%;"></span>
              <span>${param.seriesName}: ${value}</span>
            </div>
          `
        })
        
        return content
      },
      backgroundColor: 'rgba(0, 0, 0, 0.8)',
      borderColor: 'rgba(255, 255, 255, 0.1)',
      textStyle: {
        color: '#ffffff',
        fontSize: 11
      }
    },
    grid: {
      left: 10,
      right: 10,
      bottom: 15,
      top: 15,
      containLabel: true
    },
    xAxis: {
      type: 'category',
      data: times,
      axisLabel: {
        fontSize: 10,
        color: 'var(--text-muted)',
        formatter: (value: string) => {
          // 只显示时间部分
          return value.split(' ')[1] || value
        }
      },
      axisTick: {
        alignWithLabel: true
      }
    },
    yAxis: {
      type: 'value',
      axisLabel: {
        fontSize: 10,
        color: 'var(--text-muted)',
        formatter: (value: number) => formatBytes(value)
      },
      splitLine: {
        lineStyle: {
          color: 'var(--border-tertiary)',
          opacity: 0.5
        }
      }
    },
    series: [
      {
        name: '上传流量',
        type: 'line',
        data: uploadData,
        smooth: true,
        symbol: 'circle',
        symbolSize: 4,
        lineStyle: {
          color: '#ef4444',
          width: 2
        },
        itemStyle: {
          color: '#ef4444'
        },
        areaStyle: {
          color: {
            type: 'linear',
            x: 0,
            y: 0,
            x2: 0,
            y2: 1,
            colorStops: [
              { offset: 0, color: 'rgba(239, 68, 68, 0.3)' },
              { offset: 1, color: 'rgba(239, 68, 68, 0.05)' }
            ]
          }
        }
      },
      {
        name: '下载流量',
        type: 'line',
        data: downloadData,
        smooth: true,
        symbol: 'circle',
        symbolSize: 4,
        lineStyle: {
          color: '#10b981',
          width: 2
        },
        itemStyle: {
          color: '#10b981'
        },
        areaStyle: {
          color: {
            type: 'linear',
            x: 0,
            y: 0,
            x2: 0,
            y2: 1,
            colorStops: [
              { offset: 0, color: 'rgba(16, 185, 129, 0.3)' },
              { offset: 1, color: 'rgba(16, 185, 129, 0.05)' }
            ]
          }
        }
      }
    ],
    animation: true,
    animationDuration: 1000,
    animationEasing: 'cubicOut'
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
.time-trend-chart {
  width: 100%;
  height: 100%;
  min-height: 120px;
}

.chart {
  width: 100%;
  height: 100%;
}
</style>