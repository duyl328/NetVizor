<template>
  <div class="sidebar scrollbar-primary scrollbar-thin" :style="{ width: width + 'px' }">
    <div class="sidebar-content">
      <template v-if="false">
        <div class="sidebar-header">
          <h3 class="sidebar-title">系统概览</h3>
          <div class="sidebar-badge">{{ data.processCount }}</div>
        </div>

        <div class="sidebar-stats">
          <span>使用 echarts 绘制当前网速</span>
        </div>
      </template>

      <div class="sidebar-header">
        <h3 class="sidebar-title">所有连接</h3>
      </div>

      <!-- 应用列表 -->
      <div class="app-list">
        <div
          v-for="app in applications"
          :key="app.id"
          class="app-item"
          :class="{ 'app-item--selected': selectedApp?.id === app.id }"
          @click="selectApp(app)"
          @mouseenter="handleMouseEnter"
          @mouseleave="handleMouseLeave"
        >
          <!-- 书角折叠效果 -->
          <div v-if="selectedApp?.id === app.id" class="folded-corner"></div>

          <!-- 应用图标 -->
          <div class="app-icon">
            <img :src="app.icon" :alt="app.name" />
          </div>

          <!-- 应用信息 -->
          <div class="app-info">
            <div class="app-name">{{ app.name }}</div>
            <div class="app-details">
              <span class="app-detail">PID: {{ app.pid }}</span>
              <span class="app-detail">内存: {{ app.memory }}</span>
              <span class="app-detail">{{ app.status }}</span>
            </div>
          </div>

          <!-- 应用状态指示器 -->
          <div class="app-status">
            <div class="status-indicator" :class="`status-${app.statusType}`"></div>
          </div>
        </div>
      </div>

      <!-- 空状态 -->
      <div v-if="applications.length === 0" class="empty-state">
        <n-icon :component="DesktopOutline" size="48" class="empty-icon" />
        <div class="empty-title">暂无运行的应用</div>
        <div class="empty-subtitle">系统中没有检测到正在运行的应用程序</div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, defineEmits, defineProps, onMounted, watch } from 'vue'
import { NIcon } from 'naive-ui'
import { storeToRefs } from 'pinia'
import { CheckmarkOutline, DesktopOutline } from '@vicons/ionicons5'
import { httpClient } from '@/utils/http.ts'
import { ResponseModel, SubscriptionInfo } from '@/types/response'
import { useWebSocketStore,getIsConnected } from '@/stores/websocketStore'
// 定义应用数据类型
interface Application {
  id: string
  name: string
  icon: string
  pid: number
  memory: string
  status: string
  statusType: 'active' | 'idle' | 'background'
  path?: string
  windowTitle?: string
  cpuUsage?: number
}

// 事件定义
const emit = defineEmits<{
  'app-selected': [app: Application | null]
}>()
const webSocketStore = useWebSocketStore()
const { isOpen } = storeToRefs(webSocketStore)
console.log(isOpen);
watch(isOpen,(oldValue, newValue) => {
  if (oldValue || newValue) {
    console.log("触发！！！！！！！！");
    // 发送请求【请求订阅软件列表】
    // const subAppInfo: SubscriptionInfo = {
    //   subscriptionType: 'ApplicationInfo',
    //   interval: 1000,
    // }
    //
    // httpClient.post(`/subscribe`, JSON.stringify(subAppInfo)).then((res: ResponseModel) => {
    //   console.log(res)
    // })
  }
},{immediate:true})

// 选中的应用
const selectedApp = ref<Application | null>(null)

// 模拟应用数据
const applications = ref<Application[]>([
  {
    id: '1',
    name: 'Visual Studio Code',
    icon: 'https://code.visualstudio.com/assets/images/code-stable.png',
    pid: 12456,
    memory: '245.6 MB',
    status: '活跃',
    statusType: 'active',
    path: '/Applications/Visual Studio Code.app',
    windowTitle: 'main.js - myproject',
    cpuUsage: 5.2,
  },
  {
    id: '2',
    name: 'Google Chrome',
    icon: 'https://upload.wikimedia.org/wikipedia/commons/e/e1/Google_Chrome_icon_%28February_2022%29.svg',
    pid: 8923,
    memory: '512.3 MB',
    status: '活跃',
    statusType: 'active',
    path: '/Applications/Google Chrome.app',
    windowTitle: 'GitHub - Dashboard',
    cpuUsage: 12.8,
  },
  {
    id: '3',
    name: 'Spotify',
    icon: 'https://upload.wikimedia.org/wikipedia/commons/1/19/Spotify_logo_without_text.svg',
    pid: 5647,
    memory: '156.7 MB',
    status: '后台运行',
    statusType: 'background',
    path: '/Applications/Spotify.app',
    windowTitle: 'Spotify Premium',
    cpuUsage: 2.1,
  },
  {
    id: '4',
    name: 'Finder',
    icon: 'https://upload.wikimedia.org/wikipedia/en/f/f4/MacOS_Finder_Icon.png',
    pid: 1234,
    memory: '89.2 MB',
    status: '空闲',
    statusType: 'idle',
    path: '/System/Library/CoreServices/Finder.app',
    windowTitle: 'Documents',
    cpuUsage: 0.8,
  },
  {
    id: '5',
    name: 'Slack',
    icon: 'https://a.slack-edge.com/80588/marketing/img/icons/icon_slack_hash_colored.svg',
    pid: 9876,
    memory: '198.4 MB',
    status: '活跃',
    statusType: 'active',
    path: '/Applications/Slack.app',
    windowTitle: 'Slack - workspace',
    cpuUsage: 3.7,
  },
])

// 选择应用
const selectApp = (app: Application) => {
  selectedApp.value = app
  emit('app-selected', app)
}

// 鼠标悬停事件
const handleMouseEnter = () => {
  // 可以在这里添加额外的悬停逻辑
}

const handleMouseLeave = () => {
  // 可以在这里添加额外的离开逻辑
}

// 暴露方法给父组件
defineExpose({
  clearSelection: () => {
    selectedApp.value = null
    emit('app-selected', null)
  },
  selectById: (id: string) => {
    const app = applications.value.find((a) => a.id === id)
    if (app) {
      selectApp(app)
    }
  },
})

// Props
const props = defineProps<{
  width: number
}>()

// 模拟数据
const data = {
  activeConnections: 247,
  processCount: 43,
  networkSpeed: '5.2 MB/s',
  ruleCount: 156,
}

// 处理快速操作
const handleAction = (type: string) => {
  emit('action', type)
  console.log(`执行操作: ${type}`)
}
</script>

<style scoped>
/* 书角折叠效果 - 平衡版 */
.folded-corner {
  position: absolute;
  top: 0;
  right: 0;
  width: 0;
  height: 0;
  border-left: 30px solid transparent;
  border-top: 30px solid var(--accent-primary);
  z-index: 10;
  animation: foldIn 0.3s ease-out;
  filter: drop-shadow(1px 1px 3px rgba(0, 0, 0, 0.15));
}

.folded-corner::before {
  content: '';
  position: absolute;
  top: -30px;
  right: -30px;
  width: 0;
  height: 0;
  border-left: 30px solid var(--accent-primary);
  border-top: 30px solid transparent;
  transform: rotate(90deg);
  transform-origin: 0 0;
  filter: brightness(0.85);
}

.folded-corner::after {
  content: '';
  position: absolute;
  top: -22px;
  right: -22px;
  width: 0;
  height: 0;
  border-left: 22px solid rgba(0, 0, 0, 0.12);
  border-top: 22px solid transparent;
  transform: rotate(45deg);
  transform-origin: 0 0;
}

/* 书角的轻微发光效果 */
.app-item--selected .folded-corner {
  animation:
    foldIn 0.3s ease-out,
    cornerGlow 4s ease-in-out infinite alternate;
}

@keyframes foldIn {
  0% {
    transform: scale(0.7) rotate(-15deg);
    opacity: 0;
  }
  60% {
    transform: scale(1.05) rotate(-3deg);
    opacity: 0.9;
  }
  100% {
    transform: scale(1) rotate(0deg);
    opacity: 1;
  }
}

@keyframes cornerGlow {
  0% {
    filter: drop-shadow(1px 1px 3px rgba(0, 0, 0, 0.15));
  }
  100% {
    filter: drop-shadow(1px 1px 3px rgba(0, 0, 0, 0.15))
      drop-shadow(0 0 8px rgba(59, 130, 246, 0.4));
  }
}

.app-list-view {
  height: 100%;
  background: var(--bg-secondary);
}

.app-list-container {
  padding: 20px;
  max-width: 800px;
  margin: 0 auto;
}

/* 头部 */
.list-header {
  margin-bottom: 20px;
}

.list-title {
  font-size: 18px;
  font-weight: 600;
  color: var(--text-primary);
  margin: 0 0 4px 0;
}

.list-subtitle {
  font-size: 14px;
  color: var(--text-muted);
}

/* 应用列表 */
.app-list {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.app-item {
  background: var(--bg-card);
  backdrop-filter: var(--backdrop-blur);
  border: 1px solid var(--border-primary);
  border-radius: 12px;
  padding: 16px;
  display: flex;
  align-items: center;
  gap: 16px;
  cursor: pointer;
  transition: all 0.2s ease;
  position: relative;
  overflow: hidden;
}

.app-item:hover {
  transform: translateY(-1px);
  border-color: var(--border-hover);
  background: var(--bg-hover, var(--bg-card));
  box-shadow: var(--shadow-md);
}

.app-item--selected {
  border: 1px solid var(--accent-primary);
  background: var(--monitor-bg-card-selected);
  box-shadow:
    0 0 0 4px var(--monitor-accent-primary-alpha),
    0 8px 25px -5px rgba(59, 130, 246, 0.3);
  transform: translateY(-1px) scale(1.02);
  position: relative;
  z-index: 5;
}

/* 选中元素的左侧强调边框 */
.app-item--selected::before {
  content: '';
  position: absolute;
  left: -2px;
  top: -2px;
  bottom: -2px;
  width: 6px;
  background: linear-gradient(
    to bottom,
    var(--accent-primary) 0%,
    #1d4ed8 50%,
    var(--accent-primary) 100%
  );
  border-radius: 6px 0 0 6px;
  z-index: 1;
}

/* 应用图标 */
.app-icon {
  width: 48px;
  height: 48px;
  border-radius: 10px;
  overflow: hidden;
  display: flex;
  align-items: center;
  justify-content: center;
  background: var(--bg-tertiary);
  flex-shrink: 0;
}

.app-icon img {
  width: 32px;
  height: 32px;
  object-fit: contain;
}

/* 应用信息 */
.app-info {
  flex: 1;
  min-width: 0;
}

.app-name {
  font-size: 15px;
  font-weight: 600;
  color: var(--text-primary);
  margin-bottom: 4px;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.app-details {
  display: flex;
  align-items: center;
  gap: 12px;
  font-size: 12px;
  color: var(--text-muted);
}

.app-detail {
  display: flex;
  align-items: center;
  gap: 4px;
  white-space: nowrap;
}

/* 状态指示器 */
.app-status {
  display: flex;
  align-items: center;
}

.status-indicator {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  position: relative;
}

.status-indicator::before {
  content: '';
  position: absolute;
  top: -2px;
  left: -2px;
  right: -2px;
  bottom: -2px;
  border-radius: 50%;
  opacity: 0.3;
  animation: pulse 2s infinite;
}

.status-active {
  background: var(--accent-success);
}

.status-active::before {
  background: var(--accent-success);
}

.status-background {
  background: var(--accent-warning);
}

.status-background::before {
  background: var(--accent-warning);
}

.status-idle {
  background: var(--text-quaternary);
}

.status-idle::before {
  background: var(--text-quaternary);
}

/* 空状态 */
.empty-state {
  text-align: center;
  padding: 60px 20px;
}

.empty-icon {
  color: var(--text-quaternary);
  margin-bottom: 16px;
}

.empty-title {
  font-size: 16px;
  font-weight: 600;
  color: var(--text-secondary);
  margin-bottom: 8px;
}

.empty-subtitle {
  font-size: 14px;
  color: var(--text-muted);
}

/* 动画 */
@keyframes pulse {
  0%,
  100% {
    opacity: 0.3;
    transform: scale(1);
  }
  50% {
    opacity: 0.8;
    transform: scale(1.2);
  }
}

@keyframes bounceIn {
  0% {
    opacity: 0;
    transform: scale(0.3);
  }
  50% {
    transform: scale(1.1);
  }
  100% {
    opacity: 1;
    transform: scale(1);
  }
}

/* 响应式 */
@media (max-width: 768px) {
  .app-item {
    padding: 12px;
  }

  .app-icon {
    width: 40px;
    height: 40px;
  }

  .app-icon img {
    width: 28px;
    height: 28px;
  }

  .app-details {
    flex-direction: column;
    align-items: flex-start;
    gap: 4px;
  }

  .folded-corner {
    border-left-width: 24px;
    border-top-width: 24px;
  }

  .folded-corner::before {
    border-left-width: 24px;
    border-top-width: 24px;
    top: -24px;
    right: -24px;
  }

  .folded-corner::after {
    border-left-width: 18px;
    border-top-width: 18px;
    top: -18px;
    right: -18px;
  }
}

/* CSS变量定义 */
:root {
  --monitor-bg-card-selected: rgba(59, 130, 246, 0.08);
  --monitor-accent-primary-alpha: rgba(59, 130, 246, 0.2);
  --accent-primary: #3b82f6;
  --accent-success: #10b981;
  --accent-warning: #f59e0b;
  --bg-card: #ffffff;
  --bg-hover: #f8fafc;
  --bg-tertiary: #f8fafc;
  --border-primary: #e2e8f0;
  --border-hover: #cbd5e1;
  --text-primary: #1e293b;
  --text-secondary: #475569;
  --text-muted: #64748b;
  --text-quaternary: #94a3b8;
  --shadow-lg: 0 10px 15px -3px rgba(0, 0, 0, 0.1), 0 4px 6px -2px rgba(0, 0, 0, 0.05);
  --shadow-md: 0 4px 6px -1px rgba(0, 0, 0, 0.1), 0 2px 4px -1px rgba(0, 0, 0, 0.06);
}

/* 深色模式适配 */
@media (prefers-color-scheme: dark) {
  :root {
    --monitor-bg-card-selected: rgba(59, 130, 246, 0.12);
    --monitor-accent-primary-alpha: rgba(59, 130, 246, 0.25);
    --bg-card: #1e293b;
    --bg-hover: #334155;
    --bg-tertiary: #334155;
    --border-primary: #475569;
    --border-hover: #64748b;
    --text-primary: #f1f5f9;
    --text-secondary: #e2e8f0;
    --text-muted: #cbd5e1;
    --text-quaternary: #94a3b8;
  }

  .folded-corner::after {
    border-left-color: rgba(255, 255, 255, 0.08);
  }
}

/* 侧边栏容器 */
.sidebar {
  background: var(--bg-glass, var(--bg-card));
  backdrop-filter: var(--backdrop-blur, blur(10px));
  border-right: 1px solid var(--border-primary);
  overflow: hidden;
  display: flex;
  flex-direction: column;
  flex-shrink: 0;
}

.sidebar-content {
  padding: 24px;
  overflow-y: auto;
  flex: 1;
  min-height: 0;
}

/* 头部 */
.sidebar-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 20px;
}

.sidebar-title {
  font-size: 16px;
  font-weight: 600;
  color: var(--text-secondary);
  margin: 0;
}

.sidebar-badge {
  background: linear-gradient(135deg, var(--accent-primary) 0%, #1d4ed8 100%);
  color: white;
  padding: 4px 8px;
  border-radius: 12px;
  font-size: 12px;
  font-weight: 600;
}

/* 统计卡片 */
.sidebar-stats {
  display: flex;
  flex-direction: column;
  gap: 12px;
  margin-bottom: 24px;
}
</style>
