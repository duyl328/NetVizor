<template>
  <div class="title-bar">
    <div class="title-bar-left">
      <div class="app-title">
        <div class="app-icon">
          <span>N</span>
        </div>
        <span class="app-name">NetVisor</span>
      </div>

      <div class="view-switcher">
        <n-tabs
          type="segment"
          class="n-tab-group-set"
          animated
          :value="activeTab"
          @update:value="handleTabChange"
        >
          <n-tab
            class="n-tab-item-set"
            v-for="tab in tabConfig"
            :key="tab.name"
            :name="tab.name"
            :tab="tab.label"
          />
        </n-tabs>
      </div>
    </div>

    <div class="title-bar-right">
      <div class="status-indicator" :class="statusClass">
        <div class="status-dot"></div>
        <span>{{ statusText }}</span>
      </div>

      <n-button circle size="small" quaternary @click="openSettings">
        <template #icon>
          <n-icon :component="SettingsOutline" />
        </template>
      </n-button>

      <n-button circle size="small" quaternary @click="toggleDarkMode">
        <template #icon>
          <n-icon :component="isDark ? SunnyOutline : MoonOutline" />
        </template>
      </n-button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { NButton, NIcon, NTabs, NTab } from 'naive-ui'
import { SettingsOutline, MoonOutline, SunnyOutline } from '@vicons/ionicons5'
import { computed, ref, watch } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useThemeStore } from '@/stores/theme'
import { useWebSocketStore } from '@/stores/websocketStore'
import { WebSocketState } from '@/types/websocket'

// region 路由的激活和绑定
const router = useRouter()
const route = useRoute()

// Tab配置
const tabConfig = [
  { name: 'monitor', label: '实时监控', path: '/monitor' },
  { name: 'firewall', label: '防火墙', path: '/firewall' },
  { name: 'analyse', label: '分析', path: '/analyse' },
]

// 根据当前路由设置激活的Tab
const activeTab = ref(String(route.name) || 'monitor')

// 监听路由变化，同步Tab状态
watch(
  () => route.name,
  (newRouteName) => {
    if (newRouteName) {
      activeTab.value = String(newRouteName)
    }
  },
)

// 处理Tab切换
const handleTabChange = (tabName: string) => {
  activeTab.value = tabName
  router.push({ name: tabName })
}
// endregion

// region WebSocket状态
const websocketStore = useWebSocketStore()

// 状态文本映射
const statusTextMap = {
  [WebSocketState.DISCONNECTED]: '连接断开',
  [WebSocketState.CONNECTING]: '正在连接...',
  [WebSocketState.CONNECTED]: '实时监控中',
  [WebSocketState.ERROR]: '连接错误'
}

// 状态样式映射
const statusStyleMap = {
  [WebSocketState.DISCONNECTED]: 'disconnected',
  [WebSocketState.CONNECTING]: 'connecting',
  [WebSocketState.CONNECTED]: 'connected',
  [WebSocketState.ERROR]: 'error'
}

// 计算当前状态文本
const statusText = computed(() => {
  return statusTextMap[websocketStore.connectionState] || '未知状态'
})

// 计算当前状态样式类
const statusClass = computed(() => {
  return statusStyleMap[websocketStore.connectionState] || 'disconnected'
})
// endregion

// region 黑暗模式
const themeStore = useThemeStore()

const isDark = computed(() => themeStore.isDark)

const toggleDarkMode = () => {
  themeStore.toggleTheme()
  console.log('切换暗色模式:', isDark.value)
}
// endregion

// region 打开设置
const openSettings = () => {
  console.log('打开设置')
}
// endregion
</script>

<style scoped>
/* 自定义 Naive UI Tabs 组件的主题样式 */
.view-switcher {
  --n-tab-text-color: var(--text-quaternary);
  --n-tab-text-color-active: var(--text-primary);
  --n-tab-text-color-hover: var(--text-secondary);
  --n-tab-color-segment: var(--bg-card);
  --n-tab-border-color: var(--border-tertiary);
  --n-bar-color: var(--accent-primary);
}

/* 强制覆盖 n-tabs 的样式 */
.n-tab-group-set {
  /* 段落式 tabs 的背景色 */

  :deep(.n-tabs-tab-wrapper) {
    background-color: var(--bg-card) !important;
    border-color: var(--border-tertiary) !important;
  }

  /* tab 项的文字颜色 */

  :deep(.n-tabs-tab) {
    color: var(--text-quaternary) !important;

    &:hover {
      color: var(--text-secondary) !important;
    }

    &.n-tabs-tab--active {
      color: var(--text-primary) !important;
      background-color: var(--bg-overlay) !important;
    }
  }

  /* 段落式 tabs 的活动指示器 */

  :deep(.n-tabs-tab-pad) {
    background-color: var(--bg-overlay) !important;
    border-color: var(--border-primary) !important;
  }
}

/* 确保主题切换时的过渡效果 */
.view-switcher * {
  transition: all 0.3s ease !important;
}

/* 顶部标题栏 */
.title-bar {
  height: 100%;
  min-height: 60px;
  border-bottom: 1px solid var(--border-primary);
  background: var(--bg-overlay);
  backdrop-filter: var(--backdrop-blur);
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 24px;
  position: sticky;
  top: 0;
  z-index: 100;
}

.title-bar-left {
  display: flex;
  align-items: center;
  gap: 24px;
}

/* tab 栏样式微调 */
.n-tab-group-set {
  width: 30vw;
  min-width: 270px;
}

/* 添加过渡动画，减少闪烁 */
.view-switcher {
  transition: all 0.2s ease;
}

.app-title {
  display: flex;
  align-items: center;
  gap: 12px;
}

.app-icon {
  width: 32px;
  height: 32px;
  background: linear-gradient(135deg, var(--accent-primary) 0%, #1d4ed8 100%);
  border-radius: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
  box-shadow: 0 4px 12px rgba(59, 130, 246, 0.3);
}

.app-icon span {
  font-size: 14px;
  font-weight: 700;
  color: white;
}

.app-name {
  font-weight: 700;
  font-size: 18px;
  background: linear-gradient(135deg, var(--accent-primary) 0%, var(--accent-secondary) 100%);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
}

.title-bar-right {
  display: flex;
  align-items: center;
  gap: 16px;
}

.status-indicator {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 14px;
  color: var(--text-quaternary);
  padding: 6px 12px;
  border-radius: 20px;
  border: 1px solid;
  transition: all 0.3s ease;
}

/* 连接状态样式 */
.status-indicator.connected {
  background: rgba(34, 197, 94, 0.1);
  border-color: rgba(34, 197, 94, 0.2);
  color: rgb(34, 197, 94);
}

.status-indicator.connecting {
  background: rgba(251, 191, 36, 0.1);
  border-color: rgba(251, 191, 36, 0.2);
  color: rgb(251, 191, 36);
}

.status-indicator.disconnected {
  background: rgba(239, 68, 68, 0.1);
  border-color: rgba(239, 68, 68, 0.2);
  color: rgb(239, 68, 68);
}

.status-indicator.error {
  background: rgba(239, 68, 68, 0.1);
  border-color: rgba(239, 68, 68, 0.2);
  color: rgb(239, 68, 68);
}

.status-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  transition: all 0.3s ease;
}

.status-indicator.connected .status-dot {
  background: rgb(34, 197, 94);
  animation: pulse 2s infinite;
}

.status-indicator.connecting .status-dot {
  background: rgb(251, 191, 36);
  animation: pulse 1s infinite;
}

.status-indicator.disconnected .status-dot {
  background: rgb(239, 68, 68);
}

.status-indicator.error .status-dot {
  background: rgb(239, 68, 68);
  animation: pulse 0.5s infinite;
}

/* 动画 */
@keyframes pulse {
  0%,
  100% {
    opacity: 1;
    transform: scale(1);
  }
  50% {
    opacity: 0.8;
    transform: scale(1.1);
  }
}

/* 响应式调整 */
@media (max-width: 768px) {
  .title-bar {
    padding: 0 16px;
  }

  .title-bar-left {
    gap: 16px;
  }

  .app-name {
    display: none;
  }

  .status-indicator span {
    display: none;
  }
}

/* 为特定组件添加过渡效果 */
.n-tabs,
.n-button,
.n-input {
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1) !important;
}

/* 确保 tabs 的所有子元素都有过渡 */
.n-tabs * {
  transition: inherit !important;
}
</style>
