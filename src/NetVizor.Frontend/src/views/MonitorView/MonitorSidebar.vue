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
        <h3 class="sidebar-title">
          {{ isFiltering && filterText ? '过滤结果' : '所有连接' }}
        </h3>
        <div v-if="isFiltering && filterText" class="filter-indicator">
          <span>搜索: "{{ filterText }}"</span>
          <span class="filter-count">找到 {{ filteredApplications.length }} 个应用</span>
        </div>
      </div>

      <!-- 应用列表容器 - 虚拟滚动 -->
      <div class="app-list-container">
        <!-- 空状态显示 -->
        <div v-if="filteredApplications.length === 0" class="empty-state">
          <n-icon :component="DesktopOutline" size="48" class="empty-icon" />
          <div class="empty-title">
            {{ isFiltering && filterText ? '没有找到匹配的应用' : '暂无运行的应用' }}
          </div>
          <div class="empty-subtitle">
            {{
              isFiltering && filterText
                ? `没有找到包含 "${filterText}" 的应用程序，请尝试其他搜索词`
                : '系统中没有检测到正在运行的应用程序'
            }}
          </div>
        </div>

        <!-- 虚拟滚动列表 -->
        <RecycleScroller
          v-else
          ref="recycleScrollerRef"
          class="app-list scrollbar-primary scrollbar-thin"
          :items="filteredApplications"
          :item-size="itemHeight"
          key-field="id"
          v-slot="{ item: app, index }"
          tabindex="0"
          @keydown="handleKeyDown"
          @blur="handleListBlur"
        >
          <div
            :ref="(el) => setAppItemRef(el, index)"
            class="app-item"
            :class="{
              'app-item--selected': selectedApp?.id === app.id,
              'app-item--focused': focusedIndex === index,
            }"
            @click="selectApp(app)"
            @mouseenter="handleMouseEnter(index)"
            @mouseleave="handleMouseLeave"
          >
            <!-- 书角折叠效果 -->
            <div v-if="selectedApp?.id === app.id" class="folded-corner"></div>

            <!-- 应用图标 -->
            <div class="app-icon">
              <img
                v-if="!lodash.isEmpty(app.iconBase64)"
                :src="'data:image/jpeg;base64,' + app.iconBase64"
                :alt="app.productName"
              />
              <div v-else>
                <div class="app-icon-span" :style="getGradientColor(getFirstChar(app.productName))">
                  <span>{{ getFirstChar(app.productName) }}</span>
                </div>
              </div>
            </div>

            <!-- 应用信息 -->
            <div class="app-info">
              <div class="app-name">
                <!-- 在过滤模式下不需要高亮，因为显示的都是匹配项 -->
                <template v-if="isFiltering && filterText">
                  {{ getProductName(app) }}
                </template>
                <template v-else>
                  <n-highlight
                    :text="getProductName(app)"
                    :patterns="highlightPatterns"
                    :highlight-style="highlightStyle"
                  />
                </template>
              </div>
              <div class="app-details">
                <span class="app-detail">
                  线程数:
                  <template v-if="isFiltering && filterText">
                    {{ getProcessCount(app) }}
                  </template>
                  <template v-else>
                    <n-highlight
                      :text="getProcessCount(app)"
                      :patterns="highlightPatterns"
                      :highlight-style="highlightStyle"
                    />
                  </template>
                </span>
                <span class="app-detail">内存: {{ formatMemory(app.useMemory || 0) }}</span>
                <span class="app-detail">{{ !!app.exitCode ? '已退出' : '活动' }}</span>
              </div>
            </div>

            <!-- 应用状态指示器 -->
            <div class="app-status">
              <div
                class="status-indicator"
                :class="`status-${!!app.exitCode ? 'inactive' : 'active'}`"
              ></div>
            </div>
          </div>
        </RecycleScroller>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, defineProps, watch, onMounted, onUnmounted, nextTick, computed, defineExpose } from 'vue'
import { NIcon, NHighlight, useThemeVars } from 'naive-ui'
import { storeToRefs } from 'pinia'
import { DesktopOutline } from '@vicons/ionicons5'
import { RecycleScroller } from 'vue-virtual-scroller'
import { httpClient } from '@/utils/http.ts'
import { ResponseModel, SubscriptionInfo } from '@/types/response'
import { useWebSocketStore } from '@/stores/websocketStore'
import { useApplicationStore } from '@/stores/application'
import { useFilterStore } from '@/stores/filterStore'
import { ApplicationType } from '@/types/infoModel'
import { convertFileSize } from '@/utils/fileUtil'
import { FILE_SIZE_UNIT_ENUM } from '@/constants/enums'
import lodash from 'lodash'
import { getGradientColor } from '@/utils/colorUtils'
import 'vue-virtual-scroller/dist/vue-virtual-scroller.css'

// Props
const props = defineProps<{
  width: number
}>()

// Store
const webSocketStore = useWebSocketStore()
const applicationStore = useApplicationStore()
const filterStore = useFilterStore()
const { isOpen } = storeToRefs(webSocketStore)
const { selectedApp } = storeToRefs(applicationStore)
const { filterText, isFiltering, filteredApplications } = storeToRefs(filterStore)

// 主题变量
const themeVars = useThemeVars()

// 高亮模式
const highlightPatterns = computed(() => {
  if (!isFiltering.value || !filterText.value) return []
  return [filterText.value]
})

// 高亮样式
const highlightStyle = computed(() => ({
  padding: '1px 2px',
  borderRadius: themeVars.value.borderRadius,
  display: 'inline-block',
  color: themeVars.value.baseColor,
  background: themeVars.value.primaryColor,
  transition: `all .3s ${themeVars.value.cubicBezierEaseInOut}`,
}))

// 安全地获取产品名称
const getProductName = (app: ApplicationType) => {
  return app?.productName || ''
}

// 安全地获取进程数量
const getProcessCount = (app: ApplicationType) => {
  return app?.processIds?.length?.toString() || '0'
}

// Refs
const recycleScrollerRef = ref<InstanceType<typeof RecycleScroller>>()
const appItemRefs = ref<(HTMLDivElement | null)[]>([])
const focusedIndex = ref<number>(-1) // 添加聚焦索引

// 虚拟滚动配置
const itemHeight = 88 // 每个应用项的固定高度 (增加间距)

// 设置应用项目的 ref
const setAppItemRef = (el: unknown, index: number) => {
  if (el) {
    appItemRefs.value[index] = el
  }
}
function postWithPolling(url: string, data: any, interval = 2000, maxRetries = 10) {
  let attempt = 0;

  const tryRequest = () => {
    httpClient
    .post(url, JSON.stringify(data))
    .then((res: ResponseModel) => {
      console.log('订阅应用信息成功:', res);
      // 成功后就停止
    })
    .catch((err) => {
      attempt++;
      if (attempt < maxRetries) {
        console.warn(`请求失败(${attempt}/${maxRetries})，${interval}ms 后重试...`);
        setTimeout(tryRequest, interval);
      } else {
        console.error(`请求最终失败，已达到最大重试次数:`, err);
      }
    });
  };

  tryRequest();
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

        postWithPolling("/subscribe-application", subAppInfo, 500, 20);
      }
    },
    { immediate: true },
  )

  // 监听应用列表变化，自动选中第一个
  watch(
    filteredApplications,
    async (newAppInfos, oldAppInfos) => {
      // 重置聚焦索引
      // focusedIndex.value = -1

      // 虚拟滚动列表数据更新处理
      await nextTick()

      // 强制虚拟滚动器更新布局（处理数据变化）
      if (recycleScrollerRef.value) {
        // 通知虚拟滚动器数据已变化
        recycleScrollerRef.value.$forceUpdate()
      }

      // 如果有应用且当前没有选中的应用
      if (newAppInfos.length > 0 && !selectedApp.value) {
        await nextTick()
        selectApp(newAppInfos[0], true) // 自动选中时需要滚动
      }
      // 如果当前选中的应用不在列表中了，选中第一个
      else if (
        newAppInfos.length > 0 &&
        selectedApp.value &&
        !newAppInfos.find((app) => app.id === selectedApp.value?.id)
      ) {
        await nextTick()
        selectApp(newAppInfos[0], true) // 自动选中时需要滚动
      }
      // 如果没有应用了，清空选中
      else if (newAppInfos.length === 0) {
        applicationStore.setSelectedApp(null)
      }
      // 如果选中的应用仍在列表中，但位置可能发生了变化，滚动到新位置
      else if (selectedApp.value && newAppInfos.length > 0) {
        const newIndex = newAppInfos.findIndex((app) => app.id === selectedApp.value?.id)
        const oldIndex = oldAppInfos?.findIndex((app) => app.id === selectedApp.value?.id) ?? -1

        // 如果位置发生了变化，或者这是过滤操作的结果，滚动到新位置
        if (newIndex !== -1 && (newIndex !== oldIndex || newAppInfos.length !== oldAppInfos?.length)) {
          await nextTick()
          scrollToFocusedItem(newIndex)
        }
      }
    },
    { immediate: true, deep: false },
  )

  // 监听过滤状态变化，确保虚拟列表正确更新
  watch(
    [isFiltering, filterText],
    async () => {
      await nextTick()
      // 过滤状态变化时，强制更新虚拟滚动器
      if (recycleScrollerRef.value) {
        recycleScrollerRef.value.$forceUpdate()

        // 如果有选中的应用，滚动到对应位置
        if (selectedApp.value) {
          const index = filteredApplications.value.findIndex((app) => app.id === selectedApp.value?.id)
          if (index !== -1) {
            setTimeout(() => {
              scrollToFocusedItem(index)
            }, 100) // 延迟一点确保虚拟滚动器已更新
          }
        }
      }
    }
  )

  // 自动聚焦到列表容器
  nextTick(() => {
    recycleScrollerRef.value?.$el?.focus()
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
  if (filteredApplications.value.length === 0) return

  let newFocusedIndex = focusedIndex.value

  switch (event.key) {
    case 'ArrowUp':
      event.preventDefault()
      // 如果当前没有聚焦，从选中项开始
      if (focusedIndex.value === -1 && selectedApp.value) {
        const selectedIndex = filteredApplications.value.findIndex(
          (app: ApplicationType) => app.id === selectedApp.value?.id,
        )
        newFocusedIndex = selectedIndex > 0 ? selectedIndex - 1 : filteredApplications.value.length - 1
      }
      // 如果没有聚焦也没有选中，从最后一个开始
      else if (focusedIndex.value === -1) {
        newFocusedIndex = filteredApplications.value.length - 1
      }
      // 正常向上移动
      else if (focusedIndex.value === 0) {
        newFocusedIndex = filteredApplications.value.length - 1
      } else {
        newFocusedIndex = focusedIndex.value - 1
      }
      break

    case 'ArrowDown':
      event.preventDefault()
      // 如果当前没有聚焦，从选中项开始
      if (focusedIndex.value === -1 && selectedApp.value) {
        const selectedIndex = filteredApplications.value.findIndex((app) => app.id === selectedApp.value?.id)
        newFocusedIndex = selectedIndex < filteredApplications.value.length - 1 ? selectedIndex + 1 : 0
      }
      // 如果没有聚焦也没有选中，从第一个开始
      else if (focusedIndex.value === -1) {
        newFocusedIndex = 0
      }
      // 正常向下移动
      else if (focusedIndex.value >= filteredApplications.value.length - 1) {
        newFocusedIndex = 0
      } else {
        newFocusedIndex = focusedIndex.value + 1
      }
      break

    case 'Enter':
      event.preventDefault()
      // 如果有聚焦的应用，选中它（键盘操作需要滚动）
      if (focusedIndex.value >= 0 && focusedIndex.value < filteredApplications.value.length) {
        selectApp(filteredApplications.value[focusedIndex.value], true)
      }
      break

    case 'Escape':
      event.preventDefault()
      // ESC 键清除聚焦
      focusedIndex.value = -1
      break

    default:
      return
  }

  // 更新聚焦索引
  if (event.key === 'ArrowUp' || event.key === 'ArrowDown') {
    focusedIndex.value = newFocusedIndex
    // 滚动到可见区域
    scrollToFocusedItem(newFocusedIndex)
  }
}

// 滚动到聚焦的项目 - 虚拟滚动版本
const scrollToFocusedItem = (index: number) => {
  nextTick(() => {
    if (recycleScrollerRef.value && index >= 0 && index < filteredApplications.value.length) {
      try {
        // 使用虚拟滚动的 scrollToItem 方法
        recycleScrollerRef.value.scrollToItem(index)
      } catch (error) {
        console.warn('Virtual scroller scrollToItem failed:', error)
        // 如果虚拟滚动失败，尝试手动滚动
        const viewport = recycleScrollerRef.value.$el?.querySelector('.vue-recycle-scroller__viewport')
        if (viewport) {
          const targetPosition = index * itemHeight
          viewport.scrollTo({
            top: targetPosition,
            behavior: 'smooth'
          })
        }
      }
    }
  })
}

// 滚动到指定的应用（外部调用接口）
const scrollToApp = (appId: string) => {
  const index = filteredApplications.value.findIndex((app) => app.id === appId)
  if (index !== -1) {
    scrollToFocusedItem(index)
    // 可选：同时设置聚焦和选中
    focusedIndex.value = index
    selectApp(filteredApplications.value[index])
  }
}

// 滚动到选中的应用
const scrollToSelectedApp = () => {
  if (selectedApp.value) {
    const index = filteredApplications.value.findIndex((app) => app.id === selectedApp.value?.id)
    if (index !== -1) {
      scrollToFocusedItem(index)
    }
  }
}

// 刷新虚拟滚动器（用于处理数据同步问题）
const refreshVirtualScroller = () => {
  nextTick(() => {
    if (recycleScrollerRef.value) {
      try {
        recycleScrollerRef.value.$forceUpdate()
      } catch (error) {
        console.warn('Virtual scroller refresh failed:', error)
      }
    }
  })
}

// 列表失去焦点时的处理
const handleListBlur = () => {
  // 延迟清除聚焦，避免点击列表项时立即清除
  // setTimeout(() => {
  //   focusedIndex.value = -1
  // }, 200)
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
const selectApp = (app: ApplicationType, shouldScroll = false) => {
  applicationStore.setSelectedApp(app)

  // 选中后，如果是通过键盘选中的，保持聚焦在当前项
  // 如果是通过鼠标点击的，会在 mouseLeave 时自动清除聚焦
  const selectedIndex = filteredApplications.value.findIndex((a: ApplicationType) => a.id === app.id)
  if (focusedIndex.value === selectedIndex) {
    // 通过键盘选中的，保持聚焦
  } else {
    // 通过鼠标选中的，清除聚焦
    focusedIndex.value = -1
  }

  // 只在需要滚动时才滚动（避免用户点击时的意外滚动）
  if (shouldScroll && selectedIndex !== -1) {
    nextTick(() => {
      scrollToFocusedItem(selectedIndex)
    })
  }
}

// 鼠标悬停事件
const handleMouseEnter = (index: number) => {
  // 鼠标悬停时更新聚焦索引
  focusedIndex.value = index
}

const handleMouseLeave = () => {
  // 鼠标离开时清除聚焦
  focusedIndex.value = -1
}

// 暴露给父组件的方法
defineExpose({
  scrollToApp,
  scrollToSelectedApp,
  scrollToFocusedItem,
  refreshVirtualScroller,
})
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
  flex-direction: column;
  align-items: flex-start;
  gap: 8px;
}

.filter-indicator {
  display: flex;
  flex-direction: column;
  gap: 4px;
  font-size: 12px;
  color: var(--text-muted);
  background: rgba(59, 130, 246, 0.1);
  padding: 8px 12px;
  border-radius: 12px;
  border: 1px solid rgba(59, 130, 246, 0.2);
  width: 100%;
  text-align: left;
}

.filter-count {
  color: var(--accent-primary);
  font-weight: 600;
  font-size: 11px;
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

/* 应用列表 - 虚拟滚动适配 */
.app-list {
  padding: 5px;
  flex: 1;
  height: 100%; /* 虚拟滚动需要明确的高度 */
  outline: none; /* 移除聚焦时的默认轮廓 */
}

/* 虚拟滚动器内部样式适配 */
.app-list .vue-recycle-scroller {
  height: 100%;
}

.app-list .vue-recycle-scroller__viewport {
  overflow-y: auto !important; /* 启用垂直滚动 */
  overflow-x: hidden !important;
  padding-top: 5px;
  padding-left: 4px;
  padding-right: 8px; /* 为滚动条留出空间 */
}

.app-list .vue-recycle-scroller__item-wrapper {
  padding-bottom: 12px; /* 项目间间距 - 增加 */
}

/* 自定义滚动条样式 - 适配虚拟滚动 */
.app-list .vue-recycle-scroller__viewport::-webkit-scrollbar {
  width: 6px;
}

.app-list .vue-recycle-scroller__viewport::-webkit-scrollbar-track {
  background: transparent;
}

.app-list .vue-recycle-scroller__viewport::-webkit-scrollbar-thumb {
  background: var(--border-primary);
  border-radius: 3px;
  transition: background 0.2s;
}

.app-list .vue-recycle-scroller__viewport::-webkit-scrollbar-thumb:hover {
  background: var(--text-quaternary);
}

/* Firefox 滚动条 - 适配虚拟滚动 */
.app-list .vue-recycle-scroller__viewport {
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
  margin: 4px;
  display: flex;
  align-items: center;
  gap: 16px;
  cursor: pointer;
  transition: all 0.2s ease;
  position: relative;
  overflow: hidden;
  flex-shrink: 0; /* 防止压缩 */
  /* 修复模糊问题 */
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
  transform-style: preserve-3d;
  backface-visibility: hidden;
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
  transform: translateY(-1px);
  position: relative;
  z-index: 5;
}

/* 键盘导航聚焦状态 */
.app-item--focused {
  border-color: var(--accent-primary);
  background: var(--bg-hover, var(--bg-card));
  box-shadow:
    inset 0 0 0 2px var(--accent-primary),
    0 4px 12px rgba(59, 130, 246, 0.2);
  transform: translateY(-1px);
  animation: focusPulse 1.5s ease-in-out infinite;
}

/* 同时聚焦和选中的状态 */
.app-item--focused.app-item--selected {
  border-color: var(--accent-primary);
  box-shadow:
    inset 0 0 0 2px var(--accent-primary),
    0 0 0 4px var(--monitor-accent-primary-alpha),
    0 8px 25px -5px rgba(59, 130, 246, 0.3);
  animation: none;
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

@keyframes focusPulse {
  0% {
    box-shadow:
      inset 0 0 0 2px var(--accent-primary),
      0 4px 12px rgba(59, 130, 246, 0.2);
  }
  50% {
    box-shadow:
      inset 0 0 0 2px var(--accent-primary),
      0 4px 16px rgba(59, 130, 246, 0.35);
  }
  100% {
    box-shadow:
      inset 0 0 0 2px var(--accent-primary),
      0 4px 12px rgba(59, 130, 246, 0.2);
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

@media (max-width: 1080px) {
  .app-item {
    padding: 12px;
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

  .app-list .vue-recycle-scroller__viewport::-webkit-scrollbar-thumb {
    background: var(--border-primary);
  }

  .app-list .vue-recycle-scroller__viewport::-webkit-scrollbar-thumb:hover {
    background: var(--text-quaternary);
  }
}
</style>
