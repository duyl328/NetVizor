<template>
  <div class="network-analysis-chart">
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
      color: '#ffffff'
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
}

.chart {
  width: 100%;
  height: 100%;
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