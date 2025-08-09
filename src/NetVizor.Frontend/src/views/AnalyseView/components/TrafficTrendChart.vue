<template>
  <div class="traffic-trend-chart">
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

// Props定义
interface TrafficDataPoint {
  timestamp: number
  uploadSpeed: number
  downloadSpeed: number
}

const props = defineProps<{
  data: TrafficDataPoint[]
  interfaceId: string
}>()

// 生成图表配置
const chartOption = computed<EChartsOption>(() => {
  const timeLabels = props.data.map(d => new Date(d.timestamp).toLocaleTimeString())
  const uploadData = props.data.map(d => +(d.uploadSpeed / 1024 / 1024).toFixed(2))
  const downloadData = props.data.map(d => +(d.downloadSpeed / 1024 / 1024).toFixed(2))

  return {
    grid: {
      top: 20,
      left: 40,
      right: 20,
      bottom: 60
    },
    tooltip: {
      trigger: 'axis',
      axisPointer: {
        type: 'cross'
      },
      formatter: (params: any) => {
        let tooltip = `<div style="margin-bottom: 4px; font-weight: bold;">${params[0].axisValue}</div>`
        params.forEach((param: any) => {
          tooltip += `
            <div style="margin-bottom: 2px;">
              <span style="display:inline-block;margin-right:4px;border-radius:50%;width:10px;height:10px;background-color:${param.color};"></span>
              ${param.seriesName}: ${param.value} MB/s
            </div>
          `
        })
        return tooltip
      },
      backgroundColor: 'rgba(0, 0, 0, 0.8)',
      borderColor: 'rgba(255, 255, 255, 0.1)',
      textStyle: {
        color: '#ffffff',
        fontSize: 12
      }
    },
    xAxis: {
      type: 'category',
      data: timeLabels,
      axisLine: {
        lineStyle: {
          color: 'var(--border-secondary)'
        }
      },
      axisLabel: {
        color: 'var(--text-muted)',
        fontSize: 11
      }
    },
    yAxis: {
      type: 'value',
      name: 'MB/s',
      nameTextStyle: {
        color: 'var(--text-muted)'
      },
      axisLine: {
        lineStyle: {
          color: 'var(--border-secondary)'
        }
      },
      axisLabel: {
        color: 'var(--text-muted)',
        fontSize: 11
      },
      splitLine: {
        lineStyle: {
          color: 'var(--border-tertiary)',
          type: 'dashed'
        }
      }
    },
    dataZoom: [
      {
        type: 'inside'
      },
      {
        type: 'slider',
        height: 20,
        bottom: 10,
        textStyle: {
          color: 'var(--text-muted)'
        }
      }
    ],
    series: [
      {
        name: '上传',
        type: 'line',
        data: uploadData,
        smooth: true,
        lineStyle: {
          color: '#10b981',
          width: 2
        },
        areaStyle: {
          color: {
            type: 'linear',
            x: 0,
            y: 0,
            x2: 0,
            y2: 1,
            colorStops: [
              {
                offset: 0,
                color: 'rgba(16, 185, 129, 0.3)'
              },
              {
                offset: 1,
                color: 'rgba(16, 185, 129, 0.05)'
              }
            ]
          }
        },
        symbol: 'none'
      },
      {
        name: '下载',
        type: 'line',
        data: downloadData,
        smooth: true,
        lineStyle: {
          color: '#3b82f6',
          width: 2
        },
        areaStyle: {
          color: {
            type: 'linear',
            x: 0,
            y: 0,
            x2: 0,
            y2: 1,
            colorStops: [
              {
                offset: 0,
                color: 'rgba(59, 130, 246, 0.3)'
              },
              {
                offset: 1,
                color: 'rgba(59, 130, 246, 0.05)'
              }
            ]
          }
        },
        symbol: 'none'
      }
    ]
  }
})
</script>

<style scoped>
.traffic-trend-chart {
  width: 100%;
  height: 100%;
  min-height: 240px;
}

.chart {
  width: 100%;
  height: 100%;
}
</style>