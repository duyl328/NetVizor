<template>
  <div v-if="isDemoMode" :class="indicatorClass">
    <div class="indicator-content">
      <div class="indicator-icon">
        <n-icon :component="PlayCircleOutline" size="16" />
      </div>
      <span class="indicator-text">演示模式</span>
      <div v-if="showDetails" class="indicator-details">
        <n-tooltip placement="bottom-start">
          <template #trigger>
            <n-icon :component="InformationCircleOutline" size="14" class="info-icon" />
          </template>
          <div class="tooltip-content">
            <div class="tooltip-item">
              <strong>数据源：</strong>{{ demoInfo?.dataSourceType || '模拟数据' }}
            </div>
            <div class="tooltip-item">
              <strong>环境：</strong>{{ demoInfo?.environmentType || 'DEMO' }}
            </div>
            <div class="tooltip-item">
              <strong>活跃连接：</strong>{{ demoInfo?.activeSubscriptions || 0 }}
            </div>
          </div>
        </n-tooltip>
      </div>
    </div>

    <!-- 数据更新动画指示器 -->
    <div v-if="showActivityIndicator" class="activity-indicator">
      <div class="activity-dot"></div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, ref, onMounted, onUnmounted } from 'vue'
import { PlayCircleOutline, InformationCircleOutline } from '@vicons/ionicons5'
import { environmentDetector } from '@/utils/environmentDetector'
import { useWebSocketStore } from '@/stores/websocketStore'

// Props
interface Props {
  position?: 'top-left' | 'top-right' | 'bottom-left' | 'bottom-right' | 'inline'
  variant?: 'badge' | 'pill' | 'minimal'
  showDetails?: boolean
  showActivity?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  position: 'top-right',
  variant: 'badge',
  showDetails: true,
  showActivity: true
})

// 响应式数据
const showActivityIndicator = ref(false)
const websocketStore = useWebSocketStore()

// 计算属性
const isDemoMode = computed(() => environmentDetector.shouldUseMockData())
const demoInfo = computed(() => websocketStore.getDemoModeInfo())

const indicatorClass = computed(() => [
  'demo-indicator',
  `position-${props.position}`,
  `variant-${props.variant}`
])

// 模拟数据更新活动指示器
let activityTimer: NodeJS.Timeout | null = null

const startActivityIndicator = () => {
  if (!props.showActivity || !isDemoMode.value) return

  const showActivity = () => {
    showActivityIndicator.value = true
    setTimeout(() => {
      showActivityIndicator.value = false
    }, 800)
  }

  // 随机间隔显示活动指示器
  const scheduleNext = () => {
    const delay = 2000 + Math.random() * 3000 // 2-5秒随机间隔
    activityTimer = setTimeout(() => {
      showActivity()
      scheduleNext()
    }, delay)
  }

  scheduleNext()
}

const stopActivityIndicator = () => {
  if (activityTimer) {
    clearTimeout(activityTimer)
    activityTimer = null
  }
  showActivityIndicator.value = false
}

// 生命周期
onMounted(() => {
  if (isDemoMode.value) {
    console.log('[DemoModeIndicator] 演示模式指示器已激活')
    startActivityIndicator()
  }
})

onUnmounted(() => {
  stopActivityIndicator()
})

// 对外暴露
defineExpose({
  triggerActivity: () => {
    if (props.showActivity) {
      showActivityIndicator.value = true
      setTimeout(() => {
        showActivityIndicator.value = false
      }, 800)
    }
  }
})
</script>

<style scoped>
.demo-indicator {
  display: inline-flex;
  align-items: center;
  font-size: 12px;
  font-weight: 500;
  transition: all 0.3s ease;
  user-select: none;
  position: relative;
}

/* 位置样式 */
.position-top-left {
  position: fixed;
  top: 20px;
  left: 20px;
  z-index: 1000;
}

.position-top-right {
  position: fixed;
  top: 20px;
  right: 20px;
  z-index: 1000;
}

.position-bottom-left {
  position: fixed;
  bottom: 20px;
  left: 20px;
  z-index: 1000;
}

.position-bottom-right {
  position: fixed;
  bottom: 20px;
  right: 20px;
  z-index: 1000;
}

.position-inline {
  position: relative;
  z-index: auto;
}

/* 变体样式 */
.variant-badge {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  padding: 6px 12px;
  border-radius: 16px;
  box-shadow: 0 2px 8px rgba(102, 126, 234, 0.3);
}

.variant-pill {
  background: rgba(102, 126, 234, 0.1);
  color: #667eea;
  border: 1px solid rgba(102, 126, 234, 0.3);
  padding: 4px 10px;
  border-radius: 12px;
  backdrop-filter: blur(10px);
}

.variant-minimal {
  background: transparent;
  color: #667eea;
  padding: 2px 6px;
  border-radius: 4px;
}

.indicator-content {
  display: flex;
  align-items: center;
  gap: 6px;
}

.indicator-icon {
  display: flex;
  align-items: center;
  animation: pulse 2s infinite;
}

.indicator-text {
  font-weight: 500;
  letter-spacing: 0.3px;
}

.info-icon {
  cursor: pointer;
  opacity: 0.7;
  transition: opacity 0.2s ease;
}

.info-icon:hover {
  opacity: 1;
}

.activity-indicator {
  position: absolute;
  top: -2px;
  right: -2px;
  width: 8px;
  height: 8px;
}

.activity-dot {
  width: 100%;
  height: 100%;
  background: #52c41a;
  border-radius: 50%;
  animation: activityPulse 0.8s ease-out;
  box-shadow: 0 0 0 0 rgba(82, 196, 26, 0.7);
}

/* Tooltip 样式 */
.tooltip-content {
  padding: 8px 0;
  min-width: 160px;
}

.tooltip-item {
  padding: 2px 0;
  font-size: 12px;
  line-height: 1.4;
}

.tooltip-item strong {
  color: #333;
  margin-right: 4px;
}

/* 动画 */
@keyframes pulse {
  0%, 100% {
    opacity: 1;
    transform: scale(1);
  }
  50% {
    opacity: 0.8;
    transform: scale(0.95);
  }
}

@keyframes activityPulse {
  0% {
    transform: scale(0.8);
    box-shadow: 0 0 0 0 rgba(82, 196, 26, 0.7);
  }
  70% {
    transform: scale(1);
    box-shadow: 0 0 0 8px rgba(82, 196, 26, 0);
  }
  100% {
    transform: scale(0.8);
    box-shadow: 0 0 0 0 rgba(82, 196, 26, 0);
  }
}

/* 暗色主题适配 */
.dark .variant-pill {
  background: rgba(102, 126, 234, 0.2);
  border-color: rgba(102, 126, 234, 0.4);
}

.dark .variant-minimal {
  color: #8b9be6;
}

/* 响应式 */
@media (max-width: 768px) {
  .position-top-left,
  .position-top-right {
    top: 10px;
  }

  .position-top-left,
  .position-bottom-left {
    left: 10px;
  }

  .position-top-right,
  .position-bottom-right {
    right: 10px;
  }

  .position-bottom-left,
  .position-bottom-right {
    bottom: 10px;
  }

  .variant-badge,
  .variant-pill {
    padding: 4px 8px;
    font-size: 11px;
  }
}
</style>
