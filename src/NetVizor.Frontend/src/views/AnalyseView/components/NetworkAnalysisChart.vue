<template>
  <div class="network-analysis-chart" ref="containerRef">
    <div v-if="!isContainerReady" class="loading-placeholder">
      <n-spin size="large" />
      <span>正在初始化图表...</span>
    </div>
    <v-chart
      v-else
      ref="chartRef"
      class="chart"
      :option="chartOption"
      :autoresize="true"
      :init-options="{ width: containerWidth, height: containerHeight }"
      @finished="handleChartReady"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, nextTick, onMounted, onUnmounted } from 'vue'
import { NSpin } from 'naive-ui'
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
interface TopConnection {
  localIP: string
  localPort: number
  remoteIP: string
  remotePort: number
  protocol: 'TCP' | 'UDP'
  totalUpload: number
  totalDownload: number
  totalTraffic: number
  connectionCount: number
  firstSeen: string
  lastSeen: string
}

interface NetworkAnalysisData {
  appInfo: {
    appId: string
    name: string
    company?: string
    version?: string
    path?: string
    icon?: string
    hash?: string
  }
  summary: {
    timeRange: string
    startTime: string
    endTime: string
    totalUpload: number
    totalDownload: number
    totalTraffic: number
    totalConnections: number
    uniqueRemoteIPs: number
    uniqueRemotePorts: number
  }
  topConnections: TopConnection[]
  protocolStats: Array<{
    protocol: string
    connectionCount: number
    totalTraffic: number
    percentage: number
  }>
  portAnalysis: Array<{
    port: number
    serviceName: string
    connectionCount: number
    totalTraffic: number
    protocols: string[]
  }>
}

// Props定义
const props = defineProps<{
  data: NetworkAnalysisData | null
  loading?: boolean
}>()

// 响应式数据
const chartRef = ref()
const containerRef = ref<HTMLElement>()
const isContainerReady = ref(false)
const containerWidth = ref(400)
const containerHeight = ref(600)

// 检查容器尺寸
const checkContainerSize = async () => {
  if (!containerRef.value) return false

  await nextTick()

  const rect = containerRef.value.getBoundingClientRect()
  const width = rect.width || containerRef.value.clientWidth || containerRef.value.offsetWidth
  const height = rect.height || containerRef.value.clientHeight || containerRef.value.offsetHeight

  if (width > 0 && height > 0) {
    containerWidth.value = width
    containerHeight.value = height
    return true
  }

  return false
}

// 等待容器准备就绪
const waitForContainer = async (maxAttempts = 10) => {
  for (let i = 0; i < maxAttempts; i++) {
    if (await checkContainerSize()) {
      isContainerReady.value = true
      return true
    }
    await new Promise(resolve => setTimeout(resolve, 100))
  }

  // 如果还是无法获取尺寸，使用默认值
  console.warn('无法获取容器尺寸，使用默认值')
  containerWidth.value = 600
  containerHeight.value = 600
  isContainerReady.value = true
  return false
}

// ResizeObserver 监听容器尺寸变化
let resizeObserver: ResizeObserver | null = null

const setupResizeObserver = () => {
  if (typeof ResizeObserver !== 'undefined' && containerRef.value) {
    resizeObserver = new ResizeObserver(entries => {
      const entry = entries[0]
      if (entry && entry.contentRect) {
        const { width, height } = entry.contentRect
        if (width > 0 && height > 0) {
          containerWidth.value = width
          containerHeight.value = height

          // 如果图表已经存在，刷新尺寸
          if (chartRef.value && chartRef.value.chart) {
            setTimeout(() => {
              chartRef.value.chart.resize({
                width: width,
                height: height
              })
            }, 100)
          }
        }
      }
    })

    resizeObserver.observe(containerRef.value)
  }
}

// 图表就绪回调
const handleChartReady = () => {
  console.log('图表渲染完成')
}

// 生命周期钩子
onMounted(async () => {
  await waitForContainer()
  setupResizeObserver()
})

onUnmounted(() => {
  if (resizeObserver) {
    resizeObserver.disconnect()
    resizeObserver = null
  }
})

// 暴露给父组件的方法
defineExpose({
  chartRef,
  resize: () => {
    if (chartRef.value && chartRef.value.chart) {
      checkContainerSize().then(() => {
        chartRef.value.chart.resize({
          width: containerWidth.value,
          height: containerHeight.value
        })
      })
    }
  }
})

// 计算图表配置
const chartOption = computed<EChartsOption>(() => {
  if (!props.data || props.loading) {
    return {
      title: {
        text: '暂无数据',
        left: 'center',
        top: 'center',
        textStyle: {
          color: 'var(--text-muted)',
          fontSize: 16
        }
      }
    }
  }

  const { appInfo, topConnections } = props.data

  // 创建节点和边
  const nodes: any[] = []
  const links: any[] = []
  const nodeSet = new Set<string>()

  // 添加应用程序节点（中心节点）
  const appNodeId = `app_${appInfo.appId}`
  nodes.push({
    id: appNodeId,
    name: appInfo.name,
    type: 'application',
    category: 0,
    symbolSize: 80,
    itemStyle: {
      color: '#3b82f6'
    },
    label: {
      show: true,
      position: 'inside',
      formatter: '{b}',
      fontSize: 12,
      fontWeight: 'bold',
      color: '#000000',
      backgroundColor: 'rgba(255, 255, 255, 0.8)',
      padding: [4, 8],
      borderRadius: 4,
      borderColor: '#cccccc',
      borderWidth: 1
    }
  })
  nodeSet.add(appNodeId)

  // 处理连接数据，创建IP节点和端口节点
  topConnections.slice(0, 20).forEach((conn, index) => {
    const isLocalhost = conn.remoteIP === '127.0.0.1'
    const isMulticast = conn.remoteIP.startsWith('224.') || conn.remoteIP.startsWith('239.')

    // 创建远程IP节点
    const remoteIpNodeId = `ip_${conn.remoteIP}`
    if (!nodeSet.has(remoteIpNodeId)) {
      let ipCategory = 1 // 远程IP
      let ipColor = '#10b981' // 绿色

      if (isLocalhost) {
        ipCategory = 3 // 本地回环
        ipColor = '#f59e0b' // 橙色
      } else if (isMulticast) {
        ipCategory = 4 // 组播地址
        ipColor = '#8b5cf6' // 紫色
      }

      nodes.push({
        id: remoteIpNodeId,
        name: conn.remoteIP,
        type: 'remote_ip',
        category: ipCategory,
        symbolSize: Math.max(30, Math.min(60, conn.totalTraffic / 1000)),
        itemStyle: {
          color: ipColor
        },
        label: {
          show: true,
          position: 'bottom',
          formatter: '{b}',
          fontSize: 11,
          fontWeight: 600,
          color: '#2c3e50',
          backgroundColor: 'rgba(255, 255, 255, 0.9)',
          padding: [2, 6],
          borderRadius: 4,
          borderColor: '#e1e5e9',
          borderWidth: 1
        }
      })
      nodeSet.add(remoteIpNodeId)
    }

    // 创建端口节点
    const portNodeId = `port_${conn.remoteIP}_${conn.remotePort}`
    if (!nodeSet.has(portNodeId)) {
      nodes.push({
        id: portNodeId,
        name: `${conn.protocol}:${conn.remotePort}`,
        type: 'port',
        category: 2,
        symbolSize: Math.max(20, Math.min(40, conn.connectionCount * 2)),
        itemStyle: {
          color: conn.protocol === 'TCP' ? '#ef4444' : '#06b6d4'
        },
        label: {
          show: true,
          position: 'right',
          formatter: `{b}`,
          fontSize: 10,
          fontWeight: 500,
          color: '#34495e',
          backgroundColor: 'rgba(255, 255, 255, 0.8)',
          padding: [1, 4],
          borderRadius: 3
        }
      })
      nodeSet.add(portNodeId)
    }

    // 创建应用到IP的连接
    links.push({
      source: appNodeId,
      target: remoteIpNodeId,
      value: conn.totalTraffic,
      lineStyle: {
        width: Math.max(2, Math.min(6, conn.totalTraffic / 5000)),
        color: conn.protocol === 'TCP' ? '#ef4444' : '#06b6d4',
        opacity: 0.6
      },
      label: {
        show: false,
        formatter: `${formatBytes(conn.totalTraffic)}`
      }
    })

    // 创建IP到端口的连接
    links.push({
      source: remoteIpNodeId,
      target: portNodeId,
      value: conn.connectionCount,
      lineStyle: {
        width: Math.max(1, Math.min(3, conn.connectionCount)),
        color: '#6b7280',
        opacity: 0.4,
        type: 'dashed'
      },
      label: {
        show: false,
        formatter: `${conn.connectionCount}连接`
      }
    })
  })

  const categories = [
    { name: '应用程序', itemStyle: { color: '#3b82f6' } },
    { name: '远程IP', itemStyle: { color: '#10b981' } },
    { name: '端口', itemStyle: { color: '#ef4444' } },
    { name: '本地回环', itemStyle: { color: '#f59e0b' } },
    { name: '组播地址', itemStyle: { color: '#8b5cf6' } }
  ]

  return {
    tooltip: {
      trigger: 'item',
      formatter: (params: any) => {
        if (params.dataType === 'node') {
          const node = params.data
          let typeText = '未知'
          switch (node.category) {
            case 0: typeText = '应用程序'; break
            case 1: typeText = '远程IP'; break
            case 2: typeText = '端口'; break
            case 3: typeText = '本地回环'; break
            case 4: typeText = '组播地址'; break
          }

          return `
            <div style="margin-bottom: 4px; font-weight: bold;">${node.name}</div>
            <div>类型: ${typeText}</div>
            <div>节点大小: ${node.symbolSize}</div>
          `
        } else if (params.dataType === 'edge') {
          const edge = params.data
          return `
            <div style="margin-bottom: 4px; font-weight: bold;">连接</div>
            <div>${edge.source} → ${edge.target}</div>
            <div>流量: ${formatBytes(edge.value)}</div>
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
        name: '网络分析',
        type: 'graph',
        layout: 'circular',
        circular: {
          rotateLabel: true
        },
        data: nodes,
        links: links,
        categories: categories,
        roam: true,
        focusNodeAdjacency: true,
        draggable: true,
        itemStyle: {
          borderColor: '#fff',
          borderWidth: 1,
          shadowBlur: 10,
          shadowColor: 'rgba(0, 0, 0, 0.3)'
        },
        lineStyle: {
          color: 'source',
          curveness: 0.1
        },
        emphasis: {
          focus: 'adjacency',
          lineStyle: {
            width: 6
          }
        },
        animationDuration: 1500,
        animationEasingUpdate: 'quinticInOut'
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
.network-analysis-chart {
  width: 100%;
  height: 100%;
  min-height: 400px;
  position: relative;
  overflow: hidden;
}

.chart {
  min-height: 500px;
  width: 100%;
  height: 100%;
}

.loading-placeholder {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  height: 100%;
  min-height: 400px;
  gap: 16px;
  color: var(--text-muted);
  font-size: 14px;
}

/* 加载状态样式 */
.loading-overlay {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  background: rgba(255, 255, 255, 0.8);
  z-index: 10;
}
</style>
