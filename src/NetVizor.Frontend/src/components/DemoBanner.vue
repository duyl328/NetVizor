<template>
  <div v-if="showBanner" class="demo-banner">
    <div class="demo-banner-content">
      <div class="demo-info">
        <div class="demo-icon">
          <n-icon :component="InformationCircleOutline" size="24" />
        </div>
        <div class="demo-text">
          <h4 class="demo-title">在线演示版本</h4>
          <p class="demo-description">
            您正在体验 NetVizor 网络监控系统的演示版本，所有数据均为模拟数据。
            <span class="download-hint">下载完整版本获得真实的网络监控功能。</span>
          </p>
        </div>
      </div>

      <div class="demo-actions">
        <n-button
          type="primary"
          size="medium"
          @click="handleDownload"
          class="download-btn"
        >
          <template #icon>
            <n-icon :component="DownloadOutline" />
          </template>
          下载完整版
        </n-button>

        <n-button
          quaternary
          size="medium"
          @click="handleClose"
          class="close-btn"
        >
          <template #icon>
            <n-icon :component="CloseOutline" />
          </template>
        </n-button>
      </div>
    </div>

    <!-- 演示模式详细信息（可折叠） -->
    <div v-if="showDetails" class="demo-details">
      <div class="demo-features">
        <h5>演示功能包括：</h5>
        <ul class="feature-list">
          <li>✅ 实时网络连接监控</li>
          <li>✅ 防火墙规则管理界面</li>
          <li>✅ 流量统计与分析</li>
          <li>✅ 协议分布可视化</li>
          <li>✅ 安全事件展示</li>
          <li>⚠️ 所有数据均为模拟生成</li>
        </ul>
      </div>

      <div class="demo-stats" v-if="demoInfo">
        <h5>演示环境信息：</h5>
        <div class="stats-grid">
          <div class="stat-item">
            <span class="stat-label">数据源：</span>
            <span class="stat-value">{{ demoInfo.dataSourceType }}</span>
          </div>
          <div class="stat-item">
            <span class="stat-label">活跃订阅：</span>
            <span class="stat-value">{{ demoInfo.activeSubscriptions }}</span>
          </div>
          <div class="stat-item">
            <span class="stat-label">环境类型：</span>
            <span class="stat-value">{{ demoInfo.environmentType }}</span>
          </div>
        </div>
      </div>
    </div>

    <!-- 展开/收起按钮 -->
    <div class="demo-toggle">
      <n-button
        text
        size="small"
        @click="showDetails = !showDetails"
        class="toggle-btn"
      >
        <template #icon>
          <n-icon :component="showDetails ? ChevronUpOutline : ChevronDownOutline" />
        </template>
        {{ showDetails ? '收起' : '详细信息' }}
      </n-button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import {
  InformationCircleOutline,
  DownloadOutline,
  CloseOutline,
  ChevronDownOutline,
  ChevronUpOutline
} from '@vicons/ionicons5'
import { useMessage } from 'naive-ui'
import { environmentDetector } from '@/utils/environmentDetector'
import { useWebSocketStore } from '@/stores/websocketStore'

// Props
interface Props {
  position?: 'top' | 'bottom'
  persistent?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  position: 'top',
  persistent: false
})

// 响应式数据
const showBanner = ref(true)
const showDetails = ref(false)
const message = useMessage()
const websocketStore = useWebSocketStore()

// 计算属性
const isDemoMode = computed(() => environmentDetector.shouldUseMockData())
const demoInfo = computed(() => websocketStore.getDemoModeInfo())

// 如果不是演示模式，隐藏横幅
const shouldShow = computed(() => {
  return isDemoMode.value && showBanner.value
})

// 方法
const handleDownload = () => {
  // 这里可以跳转到下载页面或显示下载链接
  message.info('完整版本下载功能即将开放，敬请期待！')

  // 示例：打开新页面
  // window.open('https://github.com/your-repo/releases', '_blank')
}

const handleClose = () => {
  if (props.persistent) {
    message.warning('演示模式横幅无法关闭')
    return
  }

  showBanner.value = false

  // 可以将关闭状态保存到localStorage
  localStorage.setItem('demo-banner-closed', 'true')
}

// 生命周期
onMounted(() => {
  // 检查用户是否曾经关闭过横幅
  if (!props.persistent) {
    const wasClosed = localStorage.getItem('demo-banner-closed')
    if (wasClosed === 'true') {
      showBanner.value = false
    }
  }

  // 打印演示模式信息
  if (isDemoMode.value) {
    console.log('[DemoBanner] 演示模式已激活')
    console.log('[DemoBanner] 环境信息:', environmentDetector.getEnvironmentInfo())
  }
})

// 对外暴露
defineExpose({
  show: () => { showBanner.value = true },
  hide: () => { showBanner.value = false },
  toggle: () => { showBanner.value = !showBanner.value }
})
</script>

<style scoped>
.demo-banner {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
  z-index: 1000;
  animation: slideDown 0.3s ease-out;
}

.demo-banner-content {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 12px 20px;
  max-width: 1200px;
  margin: 0 auto;
}

.demo-info {
  display: flex;
  align-items: center;
  gap: 12px;
  flex: 1;
}

.demo-icon {
  display: flex;
  align-items: center;
  color: #ffd700;
}

.demo-text {
  flex: 1;
}

.demo-title {
  font-size: 16px;
  font-weight: 600;
  margin: 0 0 4px 0;
  color: #ffffff;
}

.demo-description {
  font-size: 14px;
  margin: 0;
  color: rgba(255, 255, 255, 0.9);
  line-height: 1.4;
}

.download-hint {
  font-weight: 500;
  color: #ffd700;
}

.demo-actions {
  display: flex;
  align-items: center;
  gap: 8px;
}

.download-btn {
  background: rgba(255, 255, 255, 0.2);
  border: 1px solid rgba(255, 255, 255, 0.3);
  backdrop-filter: blur(10px);
}

.download-btn:hover {
  background: rgba(255, 255, 255, 0.3);
  border-color: rgba(255, 255, 255, 0.5);
}

.close-btn {
  color: rgba(255, 255, 255, 0.8);
  min-width: auto;
  padding: 0 8px;
}

.close-btn:hover {
  color: #ffffff;
  background: rgba(255, 255, 255, 0.1);
}

.demo-details {
  border-top: 1px solid rgba(255, 255, 255, 0.2);
  padding: 16px 20px;
  background: rgba(0, 0, 0, 0.1);
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 24px;
  max-width: 1200px;
  margin: 0 auto;
}

.demo-features h5,
.demo-stats h5 {
  margin: 0 0 12px 0;
  color: #ffd700;
  font-size: 14px;
  font-weight: 600;
}

.feature-list {
  margin: 0;
  padding: 0;
  list-style: none;
}

.feature-list li {
  font-size: 13px;
  color: rgba(255, 255, 255, 0.9);
  margin-bottom: 6px;
  line-height: 1.4;
}

.stats-grid {
  display: grid;
  grid-template-columns: 1fr;
  gap: 8px;
}

.stat-item {
  display: flex;
  justify-content: space-between;
  font-size: 13px;
}

.stat-label {
  color: rgba(255, 255, 255, 0.7);
}

.stat-value {
  color: #ffd700;
  font-weight: 500;
}

.demo-toggle {
  text-align: center;
  padding: 8px 20px;
  border-top: 1px solid rgba(255, 255, 255, 0.1);
}

.toggle-btn {
  color: rgba(255, 255, 255, 0.8);
  font-size: 12px;
}

.toggle-btn:hover {
  color: #ffffff;
}

/* 响应式设计 */
@media (max-width: 768px) {
  .demo-banner-content {
    flex-direction: column;
    gap: 12px;
    padding: 16px;
  }

  .demo-info {
    flex-direction: column;
    text-align: center;
    gap: 8px;
  }

  .demo-details {
    grid-template-columns: 1fr;
    gap: 16px;
  }

  .demo-title {
    font-size: 15px;
  }

  .demo-description {
    font-size: 13px;
  }
}

/* 动画 */
@keyframes slideDown {
  from {
    transform: translateY(-100%);
    opacity: 0;
  }
  to {
    transform: translateY(0);
    opacity: 1;
  }
}

/* 主题变体 */
.demo-banner.position-bottom {
  position: fixed;
  bottom: 0;
  left: 0;
  right: 0;
}

.demo-banner.position-top {
  position: relative;
  width: 100%;
}
</style>
