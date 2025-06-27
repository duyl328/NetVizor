<template>
  <div class="sidebar" :style="{ width: width + 'px' }">
    <div class="sidebar-content">
      <template v-if="false">
        <div class="sidebar-header">
          <h3 class="sidebar-title">系统概览</h3>
          <div class="sidebar-badge">{{ appInfos.length }}</div>
        </div>

        <div class="sidebar-stats">
          <span>使用 echarts 绘制当前网速</span>
        </div>
      </template>

      <div class="sidebar-header">
        <h3 class="sidebar-title">所有连接</h3>
      </div>

      <!-- 应用列表容器 - 添加独立滚动 -->
      <div class="app-list-container">
        <div
          class="app-list scrollbar-primary scrollbar-thin"
          ref="appListRef"
          tabindex="0"
          @keydown="handleKeyDown"
        >
          <div
            v-for="(app, index) in appInfos"
            :key="app.Id"
            :ref="el => setAppItemRef(el, index)"
            class="app-item"
            :class="{ 'app-item--selected': selectedApp?.Id === app.Id }"
            @click="selectApp(app)"
            @mouseenter="handleMouseEnter"
            @mouseleave="handleMouseLeave"
          >
            <!-- 书角折叠效果 -->
            <div v-if="selectedApp?.Id === app.Id" class="folded-corner"></div>

            <!-- 应用图标 -->
            <div class="app-icon">
              <img
                v-if="!lodash.isEmpty(app.IconBase64)"
                :src="'data:image/jpeg;base64,' + app.IconBase64"
                :alt="app.ProductName"
              />
              <div v-else>
                <div class="app-icon-span" :style="getGradientColor(getFirstChar(app.ProductName))">
                  <span>{{ getFirstChar(app.ProductName) }}</span>
                </div>
              </div>
            </div>

            <!-- 应用信息 -->
            <div class="app-info">
              <div class="app-name">{{ app.ProductName }}</div>
              <div class="app-details">
                <span class="app-detail">线程数: {{ app.ProcessIds.length }}</span>
                <span class="app-detail">内存: {{ formatMemory(app.UseMemory) }}</span>
                <span class="app-detail">{{ !!app.ExitCode ? '已退出' : '活动' }}</span>
              </div>
            </div>

            <!-- 应用状态指示器 -->
            <div class="app-status">
              <div
                class="status-indicator"
                :class="`status-${!!app.ExitCode ? 'inactive' : 'active'}`"
              ></div>
            </div>
          </div>

          <!-- 空状态 -->
          <div v-if="appInfos.length === 0" class="empty-state">
            <n-icon :component="DesktopOutline" size="48" class="empty-icon" />
            <div class="empty-title">暂无运行的应用</div>
            <div class="empty-subtitle">系统中没有检测到正在运行的应用程序</div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, defineProps, watch, onMounted, onUnmounted, nextTick } from 'vue'
import { NIcon } from 'naive-ui'
import { storeToRefs } from 'pinia'
import { DesktopOutline } from '@vicons/ionicons5'
import { httpClient } from '@/utils/http.ts'
import { ResponseModel, SubscriptionInfo } from '@/types/response'
import { useWebSocketStore } from '@/stores/websocketStore'
import { useApplicationStore } from '@/stores/application'
import { ApplicationType } from '@/types/infoModel'
import { convertFileSize } from '@/utils/fileUtil'
import { FILE_SIZE_UNIT_ENUM } from '@/constants/enums'
import lodash from 'lodash'
import { getGradientColor } from '@/utils/colorUtils'

// Props
const props = defineProps<{
  width: number
}>()

// Store
const webSocketStore = useWebSocketStore()
const applicationStore = useApplicationStore()
const { isOpen } = storeToRefs(webSocketStore)
const { appInfos, selectedApp } = storeToRefs(applicationStore)

// Refs
const appListRef = ref<HTMLDivElement>()
const appItemRefs = ref<(HTMLDivElement | null)[]>([])

// 设置应用项目的 ref
const setAppItemRef = (el: unknown, index: number) => {
  if (el) {
    appItemRefs.value[index] = el
  }
}

// 监听 WebSocket 连接状态
onMounted(() => {
  // 监听 WebSocket 状态
  watch(
    isOpen,
    (newValue) => {
      if (newValue) {
        // 发送请求【请求订阅软件列表】
        const subAppInfo: SubscriptionInfo = {
          subscriptionType: 'ApplicationInfo',
          interval: 1000,
        }

        httpClient
          .post(`/subscribe`, JSON.stringify(subAppInfo))
          .then((res: ResponseModel) => {
            console.log('订阅应用信息成功:', res)
          })
          .catch((err) => {
            console.error('订阅应用信息失败:', err)
          })
      }
    },
    { immediate: true },
  )

  // 监听应用列表变化，自动选中第一个
  watch(
    appInfos,
    async (newAppInfos) => {
      // 如果有应用且当前没有选中的应用
      if (newAppInfos.length > 0 && !selectedApp.value) {
        await nextTick()
        selectApp(newAppInfos[0])
      }
      // 如果当前选中的应用不在列表中了，选中第一个
      else if (newAppInfos.length > 0 && selectedApp.value &&
        !newAppInfos.find(app => app.Id === selectedApp.value?.Id)) {
        await nextTick()
        selectApp(newAppInfos[0])
      }
      // 如果没有应用了，清空选中
      else if (newAppInfos.length === 0) {
        applicationStore.setSelectedApp(null)
      }
    },
    { immediate: true }
  )

  // 自动聚焦到列表容器
  nextTick(() => {
    appListRef.value?.focus()
  })
})

onUnmounted(() => {
  const subAppInfo: SubscriptionInfo = {
    subscriptionType: 'ApplicationInfo',
    interval: 1,
  }

  httpClient
    .post(`/unsubscribe`, JSON.stringify(subAppInfo))
    .then((res: ResponseModel) => {
      console.log('取消订阅应用信息成功:', res)
    })
    .catch((err) => {
      console.error('取消订阅应用信息失败:', err)
    })
})

// 处理键盘事件
const handleKeyDown = (event: KeyboardEvent) => {
  if (appInfos.value.length === 0) return

  const currentIndex = selectedApp.value
    ? appInfos.value.findIndex(app => app.Id === selectedApp.value?.Id)
    : -1

  let newIndex = currentIndex

  switch (event.key) {
    case 'ArrowUp':
      event.preventDefault()
      // 如果当前没有选中或者是第一个，则选中最后一个
      if (currentIndex <= 0) {
        newIndex = appInfos.value.length - 1
      } else {
        newIndex = currentIndex - 1
      }
      break

    case 'ArrowDown':
      event.preventDefault()
      // 如果当前没有选中或者是最后一个，则选中第一个
      if (currentIndex === -1 || currentIndex >= appInfos.value.length - 1) {
        newIndex = 0
      } else {
        newIndex = currentIndex + 1
      }
      break

    case 'Enter':
      event.preventDefault()
      // 如果有选中的应用，可以在这里触发某些操作
      if (selectedApp.value) {
        console.log('Enter pressed on:', selectedApp.value.ProductName)
        // 可以触发一个事件或执行某个操作
      }
      break

    default:
      return
  }

  // 选择新的应用
  if (newIndex !== currentIndex && newIndex >= 0 && newIndex < appInfos.value.length) {
    selectApp(appInfos.value[newIndex])
    // 滚动到可见区域
    scrollToSelectedItem(newIndex)
  }
}

// 滚动到选中的项目
const scrollToSelectedItem = (index: number) => {
  nextTick(() => {
    const itemEl = appItemRefs.value[index]
    if (itemEl && appListRef.value) {
      const listRect = appListRef.value.getBoundingClientRect()
      const itemRect = itemEl.getBoundingClientRect()

      // 如果项目不在可见区域内，滚动到中心位置
      if (itemRect.top < listRect.top || itemRect.bottom > listRect.bottom) {
        itemEl.scrollIntoView({
          behavior: 'smooth',
          block: 'center'
        })
      }
    }
  })
}

// 格式化内存显示
const formatMemory = (memoryInBytes: number): string => {
  const result = convertFileSize(memoryInBytes, FILE_SIZE_UNIT_ENUM.B)
  return result.size + result.unit
}

// 获取软件名称的第一个字符
const getFirstChar = (name: string): string => {
  if (lodash.isEmpty(name)) {
    return '?'
  }
  return name.charAt(0).toUpperCase()
}

// 选择应用
const selectApp = (app: ApplicationType) => {
  applicationStore.setSelectedApp(app)
}

// 鼠标悬停事件（预留扩展）
const handleMouseEnter = () => {
  // 可以在这里添加额外的悬停逻辑
}

const handleMouseLeave = () => {
  // 可以在这里添加额外的离开逻辑
}
</script>

<style scoped>
/* 样式部分保持不变 */
/* 侧边栏容器 */
.sidebar {
  background: var(--bg-glass, var(--bg-card));
  backdrop-filter: var(--backdrop-blur, blur(10px));
  border-right: 1px solid var(--border-primary);
  height: 100%;
  display: flex;
  flex-direction: column;
  flex-shrink: 0;
}

.sidebar-content {
  padding: 24px;
  height: 100%;
  display: flex;
  flex-direction: column;
  overflow: hidden; /* 防止整体滚动 */
}

/* 头部 */
.sidebar-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 20px;
  flex-shrink: 0; /* 防止压缩 */
}

.sidebar-title {
  font-size: 16px;
  font-weight: 600;
  color: var(--text-secondary);
  margin: 0;
  z-index: 90;
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
  flex-shrink: 0; /* 防止压缩 */
}

/* 应用列表容器 - 关键改动 */
.app-list-container {
  flex: 1;
  min-height: 0; /* 重要：允许收缩 */
  display: flex;
  flex-direction: column;
}

/* 应用列表 - 独立滚动 */
.app-list {
  flex: 1;
  overflow-y: auto; /* 启用垂直滚动 */
  overflow-x: hidden;
  display: flex;
  flex-direction: column;
  gap: 8px;
  padding-top: 5px;
  padding-left: 4px;
  padding-right: 8px; /* 为滚动条留出空间 */
  margin-right: -8px; /* 抵消padding */
  outline: none; /* 移除聚焦时的默认轮廓 */
}

/* 聚焦时的样式 */
.app-item:focus {
  box-shadow: inset 0 0 0 1px var(--accent-primary);
  border-radius: 8px;
}

/* 自定义滚动条样式 */
.app-list::-webkit-scrollbar {
  width: 6px;
}

.app-list::-webkit-scrollbar-track {
  background: transparent;
}

.app-list::-webkit-scrollbar-thumb {
  background: var(--border-primary);
  border-radius: 3px;
  transition: background 0.2s;
}

.app-list::-webkit-scrollbar-thumb:hover {
  background: var(--text-quaternary);
}

/* Firefox 滚动条 */
.app-list {
  scrollbar-width: thin;
  scrollbar-color: var(--border-primary) transparent;
}

/* 应用项目 */
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
  flex-shrink: 0; /* 防止压缩 */
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
  transform: translateY(-1px) scale(1.01);
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

/* 字体图标 */
.app-icon-span {
  width: 32px;
  height: 32px;
  background: linear-gradient(135deg, var(--accent-primary) 0%, #1d4ed8 100%);
  border-radius: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
  box-shadow: 0 4px 12px rgba(59, 130, 246, 0.3);
}

.app-icon-span span {
  font-size: 14px;
  font-weight: 700;
  color: white;
}

/* 书角折叠效果 */
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

.status-inactive {
  background: var(--text-quaternary);
}

.status-inactive::before {
  background: var(--text-quaternary);
}

/* 空状态 */
.empty-state {
  text-align: center;
  padding: 60px 20px;
  margin: auto; /* 居中显示 */
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

/* 响应式 */
@media (max-width: 768px) {
  .sidebar-content {
    padding: 16px;
  }

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

  .app-list::-webkit-scrollbar-thumb {
    background: var(--border-primary);
  }

  .app-list::-webkit-scrollbar-thumb:hover {
    background: var(--text-quaternary);
  }
}
</style>
