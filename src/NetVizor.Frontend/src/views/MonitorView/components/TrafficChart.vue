<template>
  <div class="traffic-chart">
    <div class="chart-header">
      <span class="chart-title">实时流量</span>
      <span class="chart-value">{{ currentSpeed }}</span>
    </div>
    <div class="chart-container">
      <div class="chart-bars">
        <div
          v-for="(value, index) in data"
          :key="index"
          class="chart-bar"
          :style="{ height: value + '%' }"
          :class="{ 'bar-active': index === data.length - 1 }"
        >
          <div class="bar-tooltip">{{ formatSpeed(value) }}</div>
        </div>
      </div>
      <div class="chart-grid">
        <div class="grid-line" v-for="i in 4" :key="i"></div>
      </div>
    </div>
    <div class="chart-footer">
      <span class="chart-label">{{ timeRange }}</span>
      <div class="chart-legend">
        <span class="legend-item upload">
          <span class="legend-dot"></span>
          上传
        </span>
        <span class="legend-item download">
          <span class="legend-dot"></span>
          下载
        </span>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'

// Props
const props = defineProps<{
  data: number[]
}>()

// 当前速度
const currentSpeed = computed(() => {
  const lastValue = props.data[props.data.length - 1] || 0
  return formatSpeed(lastValue)
})

// 时间范围
const timeRange = ref('最近 60 秒')

// 格式化速度
const formatSpeed = (value: number) => {
  const speed = (value / 100) * 10 // 假设最大10MB/s
  if (speed < 1) {
    return `${(speed * 1024).toFixed(0)} KB/s`
  }
  return `${speed.toFixed(1)} MB/s`
}

// 动画定时器
let animationTimer: number | null = null

onMounted(() => {
  // 添加入场动画
  animationTimer = window.setTimeout(() => {
    document.querySelectorAll('.chart-bar').forEach((el, index) => {
      ;(el as HTMLElement).style.transitionDelay = `${index * 20}ms`
    })
  }, 100)
})

onUnmounted(() => {
  if (animationTimer) {
    clearTimeout(animationTimer)
  }
})
</script>

<style scoped>
/* 图表容器 */
.traffic-chart {
  background: var(--bg-card);
  border-radius: 8px;
  padding: 16px;
  border: 1px solid var(--border-tertiary);
}

/* 图表头部 */
.chart-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 12px;
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
  position: relative;
  height: 80px;
  margin-bottom: 8px;
}

.chart-bars {
  display: flex;
  align-items: flex-end;
  gap: 2px;
  height: 100%;
  position: relative;
  z-index: 2;
}

.chart-bar {
  flex: 1;
  background: linear-gradient(to top, var(--accent-primary), var(--accent-secondary));
  border-radius: 2px 2px 0 0;
  min-height: 2px;
  transition: height 0.3s cubic-bezier(0.4, 0, 0.2, 1);
  position: relative;
  cursor: pointer;
}

.chart-bar:hover {
  background: linear-gradient(to top, #1d4ed8, #0891b2);
}

.chart-bar.bar-active {
  background: linear-gradient(to top, var(--accent-warning), var(--accent-purple));
}

/* 工具提示 */
.bar-tooltip {
  position: absolute;
  bottom: 100%;
  left: 50%;
  transform: translateX(-50%) translateY(-4px);
  background: var(--bg-tooltip);
  color: white;
  padding: 4px 8px;
  border-radius: 4px;
  font-size: 11px;
  font-weight: 500;
  white-space: nowrap;
  opacity: 0;
  pointer-events: none;
  transition: opacity 0.2s;
}

.chart-bar:hover .bar-tooltip {
  opacity: 1;
}

/* 网格线 */
.chart-grid {
  position: absolute;
  inset: 0;
  display: flex;
  flex-direction: column;
  justify-content: space-between;
  pointer-events: none;
}

.grid-line {
  height: 1px;
  background: var(--border-secondary);
  opacity: 0.5;
}

/* 图表底部 */
.chart-footer {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.chart-label {
  font-size: 11px;
  color: var(--text-muted);
}

/* 图例 */
.chart-legend {
  display: flex;
  gap: 12px;
}

.legend-item {
  display: flex;
  align-items: center;
  gap: 4px;
  font-size: 11px;
  color: var(--text-muted);
}

.legend-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: var(--accent-primary);
}

.legend-item.upload .legend-dot {
  background: var(--accent-warning);
}

.legend-item.download .legend-dot {
  background: var(--accent-secondary);
}

/* 响应式 */
@media (max-width: 1400px) {
  .chart-container {
    height: 60px;
  }
}
</style>
