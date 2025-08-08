<template>
  <div class="chart-container">
    <v-chart :option="option" autoresize class="chart" />
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { use, getInstanceByDom } from 'echarts/core'
import VChart from 'vue-echarts'
import {
  TitleComponent,
  TooltipComponent,
  GridComponent,
  LegendComponent,
  DataZoomComponent,
} from 'echarts/components'
import { BarChart } from 'echarts/charts'
import { CanvasRenderer } from 'echarts/renderers'

use([
  TitleComponent,
  TooltipComponent,
  GridComponent,
  LegendComponent,
  DataZoomComponent,
  BarChart,
  CanvasRenderer,
])

// 5分钟数据，假设1秒1条
const MAX_POINTS = 300 // 总数据点（5分钟）
const DEFAULT_SHOW = 30 // 默认显示数量
const MAX_SHOW = 60 // 最大显示数量
const MIN_SHOW = 30 // 最小显示数量

const xAxisData = []
const uploadData = []
const downloadData = []

let userZoomLocked = false // 用户是否手动调整过滑轨

const option = ref({
  title: { text: '网络速度（MB/s）' },
  legend: { data: ['上传', '下载'] },
  tooltip: { trigger: 'axis', axisPointer: { type: 'shadow' } },
  grid: { left: 40, right: 20, top: 40, bottom: 60 },
  xAxis: { type: 'category', data: [] },
  yAxis: { type: 'value' },
  animationEasingUpdate: 'linear',
  animationDurationUpdate: 200,
  dataZoom: [
    {
      type: 'slider',
      start: 100 - (DEFAULT_SHOW / MAX_POINTS) * 100,
      end: 100,
      minSpan: (MIN_SHOW / MAX_POINTS) * 100, // 最小 30 个
      maxSpan: (MAX_SHOW / MAX_POINTS) * 100, // 最大 60 个
      handleSize: '80%',
    },
    { type: 'inside' },
  ],
  series: [
    {
      name: '上传',
      type: 'bar',
      data: [],
      itemStyle: { color: '#3b82f6' },
      emphasis: { focus: 'series' },
    },
    {
      name: '下载',
      type: 'bar',
      data: [],
      itemStyle: { color: '#10b981' },
      emphasis: { focus: 'series' },
    },
  ],
})

function addData() {
  const chartInstance = getInstanceByDom(document.querySelector('.chart'))
  const selectedState = chartInstance?.getOption()?.legend?.[0]?.selected || {}
  const zoomState = chartInstance?.getOption()?.dataZoom?.[0] || {}

  const upload = +(Math.random() * 10).toFixed(2)
  const download = +(Math.random() * 10).toFixed(2)
  const label = new Date().toLocaleTimeString()

  if (uploadData.length >= MAX_POINTS) {
    uploadData.shift()
    downloadData.shift()
    xAxisData.shift()
  }

  uploadData.push(upload)
  downloadData.push(download)
  xAxisData.push(label)

  const newOption = {
    ...option.value,
    legend: { ...option.value.legend, selected: selectedState },
    series: [
      { ...option.value.series[0], data: [...uploadData] },
      { ...option.value.series[1], data: [...downloadData] },
    ],
    xAxis: { ...option.value.xAxis, data: [...xAxisData] },
  }

  const range = getZoomRange(xAxisData.length)

  if (userZoomLocked) {
    newOption.dataZoom = [
      {
        ...option.value.dataZoom[0],
        start: zoomState.start,
        end: zoomState.end,
        minSpan: (MIN_SHOW / Math.max(xAxisData.length, MIN_SHOW)) * 100,
        maxSpan: (MAX_SHOW / Math.max(xAxisData.length, MAX_SHOW)) * 100
      },
      option.value.dataZoom[1]
    ]
  } else {
    newOption.dataZoom = [
      {
        ...option.value.dataZoom[0],
        start: range.start,
        end: range.end,
        minSpan: (MIN_SHOW / Math.max(xAxisData.length, MIN_SHOW)) * 100,
        maxSpan: (MAX_SHOW / Math.max(xAxisData.length, MAX_SHOW)) * 100
      },
      option.value.dataZoom[1]
    ]
  }

  option.value = newOption
}

function getZoomRange(currentPoints) {
  if (currentPoints <= DEFAULT_SHOW) {
    return { start: 0, end: 100 }
  }
  const spanPercent = (DEFAULT_SHOW / currentPoints) * 100
  return { start: 100 - spanPercent, end: 100 }
}

onMounted(() => {
  const chartInstance = getInstanceByDom(document.querySelector('.chart'))

  // 监听滑轨变化，判断是否锁定用户设置
  chartInstance?.on('dataZoom', () => {
    userZoomLocked = true
  })

  setInterval(addData, 1000)
})
</script>

<style scoped>
.chart-container {
  width: 100%;
  height: 400px;
}

.chart {
  width: 100%;
  height: 100%;
}
</style>
现在我想在src\views\MonitorView\components\TrafficChart.vue中实现网速的动态展示，目前他已经可以收集到信息，也能展示一些信息了，但是展示的效果不是我想要的
src\views\dev\charts\BarAnimationDelay.vue 中我用模拟数据也实现了一个demo，可以展示网速数据并且可以通过滑块动态展示，你可以直接参照使用把TrafficChart中的组件换掉
换成BarAnimationDelay中那样的使用形式，不过标题和图例可能要重新考虑拜访位置。 帮我做到把
