<template>
  <div class="network-relation-chart">
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
import { GraphChart } from 'echarts/charts'
import {
  TitleComponent,
  TooltipComponent,
  LegendComponent
} from 'echarts/components'
import type { EChartsOption } from 'echarts'

// 注册ECharts组件
use([
  CanvasRenderer,
  GraphChart,
  TitleComponent,
  TooltipComponent,
  LegendComponent
])

// 接口定义
interface NetworkNode {
  id: string
  name: string
  type: 'application' | 'port' | 'remote_host'
  size: number
  category: number
}

interface NetworkLink {
  source: string
  target: string
  value: number
  label: string
}

interface NetworkRelationData {
  nodes: NetworkNode[]
  links: NetworkLink[]
}

// Props定义
const props = defineProps<{
  data: NetworkRelationData
  software?: any
}>()

// 生成图表配置
const chartOption = computed<EChartsOption>(() => {
  const categories = [
    { name: '应用程序', itemStyle: { color: '#3b82f6' } },
    { name: '端口', itemStyle: { color: '#10b981' } },
    { name: '远程主机', itemStyle: { color: '#f59e0b' } }
  ]

  return {
    tooltip: {
      trigger: 'item',
      formatter: (params: any) => {
        if (params.dataType === 'node') {
          const node = params.data
          return `
            <div style="margin-bottom: 4px; font-weight: bold;">${node.name}</div>
            <div>类型: ${getCategoryName(node.category)}</div>
            <div>流量: ${node.size.toFixed(1)} MB</div>
          `
        } else if (params.dataType === 'edge') {
          const edge = params.data
          return `
            <div style="margin-bottom: 4px; font-weight: bold;">连接</div>
            <div>${edge.source} → ${edge.target}</div>
            <div>流量: ${edge.label}</div>
          `
        }
        return ''
      },
      backgroundColor: 'rgba(0, 0, 0, 0.8)',
      borderColor: 'rgba(255, 255, 255, 0.1)',
      textStyle: {
        color: '#ffffff',
        fontSize: 12
      }
    },
    legend: {
      data: categories.map(c => c.name),
      orient: 'horizontal',
      left: 'center',
      top: 10,
      textStyle: {
        color: 'var(--text-secondary)',
        fontSize: 11
      }
    },
    series: [
      {
        name: '网络连接',
        type: 'graph',
        layout: 'circular',
        circular: {
          rotateLabel: true
        },
        data: props.data.nodes.map(node => ({
          ...node,
          symbolSize: Math.max(20, Math.min(60, node.size * 2)),
          itemStyle: {
            color: categories[node.category]?.itemStyle.color || '#6b7280'
          },
          label: {
            show: true,
            position: 'right',
            formatter: '{b}',
            fontSize: 10,
            color: 'var(--text-secondary)'
          }
        })),
        links: props.data.links.map(link => ({
          ...link,
          lineStyle: {
            color: '#6b7280',
            width: Math.max(1, Math.min(4, link.value / 10)),
            opacity: 0.6
          },
          label: {
            show: true,
            fontSize: 9,
            color: 'var(--text-muted)'
          }
        })),
        categories: categories,
        roam: true,
        focusNodeAdjacency: true,
        itemStyle: {
          borderColor: '#fff',
          borderWidth: 1,
          shadowBlur: 10,
          shadowColor: 'rgba(0, 0, 0, 0.3)'
        },
        lineStyle: {
          color: 'source',
          curveness: 0.2
        },
        emphasis: {
          focus: 'adjacency',
          lineStyle: {
            width: 4
          }
        },
        animationDuration: 1500,
        animationEasingUpdate: 'quinticInOut'
      }
    ]
  }
})

// 获取分类名称
const getCategoryName = (category: number): string => {
  const names = ['应用程序', '端口', '远程主机']
  return names[category] || '未知'
}
</script>

<style scoped>
.network-relation-chart {
  width: 100%;
  height: 200px;
  min-height: 200px;
}

.chart {
  width: 100%;
  height: 100%;
}
</style>