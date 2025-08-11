<script setup lang="ts">
import { useRoute, useRouter } from 'vue-router'
import PathUtils from '@/utils/pathUtils'
import StringUtils from '@/utils/stringUtils'
import type { subRoute, subRouteList } from '@/types/devIndex'
import app from '@/constants/app'
import { ref, onMounted, provide } from 'vue'
import { NButton } from 'naive-ui'
import { NConfigProvider, NGlobalStyle, NMessageProvider, NLoadingBarProvider, NDialogProvider } from 'naive-ui'
import { useThemeStore } from '@/stores/theme'
import NetworkConnectionPanel2 from '@/components/NetworkConnectionPanel2.vue'
import MainLayout from './layouts/MainLayout.vue'

const router = useRouter()
// 是否展示生成路由
const isShowGenerateRouter = ref(false)

const navRef = ref(null) // 用于获取导航栏元素

console.log('App.Vue')

// region 获取路由列表
// 自动获取路由列表
const routes = {
  ...import.meta.glob('@/views/*.vue'),
  ...import.meta.glob('@/views/dev/*.vue'),
}
const route = useRoute()

// 子路由列表
const subRouteLists: subRouteList = []
// 遍历路由page列表进行路由渲染
Object.keys(routes).map((path: string) => {
  if ('.vue' === PathUtils.extname(path).trim()) {
    const fileName = PathUtils.basename(path).split('.')[0]
    const s = StringUtils.toCustomCase(fileName).toLowerCase().split('-')[0]
    const items = {
      displayName: fileName,
      path: `/${s}`, // 生成路径
    }
    subRouteLists.push(items)
  }
  return ''
})

/**
 * 列表路径是否激活
 * @param path
 */
const isActive = (path: string) => {
  return route.path.includes(path)
}

/**
 * 列表点击调准
 */
function listClickJump(item: subRoute) {
  console.log(item.path)
  router.push({ path: item.path })
}

// endregion

// region 开发和生产状态确认
const nodeEnv: string = import.meta.env.MODE

console.log(nodeEnv)
if (nodeEnv !== undefined && !StringUtils.isBlank(nodeEnv) && nodeEnv === app.DEVELOPMENT) {
  app.IS_PRODUCTION = false
  isShowGenerateRouter.value = app.IS_SHOW_GENERATE_ROUTER
}
// endregion

// 主题样式
const themeStore = useThemeStore()
// 提供主题给子组件
provide('theme', themeStore.theme)
// 初始化主题
onMounted(() => {
  themeStore.initTheme()
  // 标记页面已准备好，启用过渡动画
  document.body.classList.add('theme-ready')
})
// region 拖拽 tab
// 添加拖拽相关状态
const isDragging = ref(false)
const navPosition = ref({ x: 10, y: 10 }) // 默认位置
const dragOffset = ref({ x: 0, y: 0 })

// 拖拽相关函数
function startDrag(event: MouseEvent) {
  if (event.target.closest('.drag-handle')) {
    isDragging.value = true

    // 计算鼠标与元素左上角的偏移量
    const navElement = navRef.value
    if (!navElement) return
    const rect = navElement.getBoundingClientRect()
    dragOffset.value = {
      x: event.clientX - rect.left,
      y: event.clientY - rect.top,
    }

    // 阻止默认事件，防止文本选择等
    event.preventDefault()
  }
}

function onDrag(event: MouseEvent) {
  if (isDragging.value) {
    // 计算新位置
    navPosition.value = {
      x: event.clientX - dragOffset.value.x,
      y: event.clientY - dragOffset.value.y,
    }
    event.preventDefault()
  }
}

function endDrag() {
  isDragging.value = false
}

// endregion

// 添加全局事件监听
onMounted(() => {
  window.addEventListener('mousemove', onDrag)
  window.addEventListener('mouseup', endDrag)

  // 清理函数
  return () => {
    window.removeEventListener('mousemove', onDrag)
    window.removeEventListener('mouseup', endDrag)
  }
})

const getLayout = (route) => {
  return route.meta.layout === 'main' ? MainLayout : 'div' // 'div' 表示没有布局
}
</script>

<template>
  <n-config-provider
    class="app-wrapper"
    :theme-overrides="themeStore.themeOverrides"
    :theme="themeStore.theme"
  >
    <n-message-provider>
      <n-dialog-provider>
        <n-loading-bar-provider>
          <n-global-style />

      <!-- 可拖动的导航栏 -->
      <div
        v-if="isShowGenerateRouter"
        ref="navRef"
        class="nav-container"
        :style="{ left: `${navPosition.x}px`, top: `${navPosition.y}px` }"
        @mousedown="startDrag"
      >
        <div class="drag-handle">
          <span class="drag-icon">≡</span>
        </div>
        <ul class="route-list">
          <li v-for="(subRoute, index) in subRouteLists" :key="index" class="route-item">
            <n-button
              :type="isActive(subRoute.path) ? 'primary' : 'default'"
              @click="listClickJump(subRoute)"
            >
              {{ subRoute.displayName }}
            </n-button>
          </li>
        </ul>
      </div>

      <!-- 展示主要内容 -->
            <div class="main-content">
              <router-view v-slot="{ Component, route }">
                <transition name="fade-slide" mode="out-in">
                  <component :is="getLayout(route)">
                    <component :is="Component" />
                  </component>
                </transition>
              </router-view>
              <!--      <network-connection-panel2/>-->
            </div>
<!--      <div class="div1">-->
<!--        <div class="div2">-->
<!--          <UnifiedConnectionsList4 />-->
<!--        </div>-->
<!--      </div>-->
        </n-loading-bar-provider>
      </n-dialog-provider>
    </n-message-provider>
  </n-config-provider>
</template>

<style scoped>
.div1 {
  padding: 50px;
  border: 1px solid red;
}

.div2 {
  border: 1px solid red;
  margin: 50px;
  height: 500px;
}

.app-wrapper {
  position: relative;
  width: 100%;
  height: 100%;
}

.nav-container {
  position: fixed;
  z-index: 9999;
  background-color: white;
  border-radius: 0.5rem;
  box-shadow:
    0 10px 15px -3px rgba(0, 0, 0, 0.1),
    0 4px 6px -2px rgba(0, 0, 0, 0.05);
  padding: 0.375rem;
  min-width: 300px;
  cursor: move;
}

.drag-handle {
  background-color: rgba(255, 0, 0, 0.1);
  padding: 0.375rem;
  text-align: center;
  border-bottom: 1px solid #eeeeee;
  font-size: 1.125rem;
  cursor: move;
}

.drag-icon {
  display: inline-block;
  padding: 0 0.5rem;
}

.route-list {
  display: flex;
  flex-wrap: wrap;
  justify-content: flex-start;
  padding: 0.375rem;
  margin: 0;
  list-style: none;
}

.route-item {
  text-align: center;
  margin: 0.5rem;
  background: green;
}

.main-content {
  width: 100%;
  height: 100%;
}
</style>
