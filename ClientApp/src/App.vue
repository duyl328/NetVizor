<script setup lang="ts">
import { useRoute, useRouter } from 'vue-router'
import PathUtils from '@/utils/pathUtils'
import StringUtils from '@/utils/stringUtils'
import type { subRoute, subRouteList } from '@/types/devIndex'
import app from '@/constants/app'
import { ref, onMounted } from 'vue'

const router = useRouter()
// 是否展示生成路由
const isShowGenerateRouter = ref(false)

// 添加拖拽相关状态
const isDragging = ref(false)
const navPosition = ref({ x: 10, y: 10 }) // 默认位置
const dragOffset = ref({ x: 0, y: 0 })
const navRef = ref(null) // 用于获取导航栏元素

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
Object.keys(routes).map((path) => {
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

// 拖拽相关函数
function startDrag(event) {
  if (event.target.closest('.drag-handle')) {
    isDragging.value = true

    // 计算鼠标与元素左上角的偏移量
    const navElement = navRef.value
    const rect = navElement.getBoundingClientRect()
    dragOffset.value = {
      x: event.clientX - rect.left,
      y: event.clientY - rect.top
    }

    // 阻止默认事件，防止文本选择等
    event.preventDefault()
  }
}

function onDrag(event) {
  if (isDragging.value) {
    // 计算新位置
    navPosition.value = {
      x: event.clientX - dragOffset.value.x,
      y: event.clientY - dragOffset.value.y
    }
    event.preventDefault()
  }
}

function endDrag() {
  isDragging.value = false
}

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
</script>

<template>
  <div class="main-container">
    <!-- 可拖动的导航栏 -->
    <div
      v-if="isShowGenerateRouter"
      ref="navRef"
      class="floating-navigation"
      :style="{ left: `${navPosition.x}px`, top: `${navPosition.y}px` }"
      @mousedown="startDrag"
    >
      <div class="drag-handle">
        <span>≡</span> <!-- 拖动图标 -->
      </div>
      <ul class="nav-list">
        <li v-for="(subRoute, index) in subRouteLists" :key="index">
          <v-btn :type="isActive(subRoute.path) ? 'primary' : ''" @click="listClickJump(subRoute)">
            {{ subRoute.displayName }}
          </v-btn>
        </li>
      </ul>
    </div>

    <!--  展示主要内容-->
    <div class="main-show">
      <router-view />
    </div>
  </div>
</template>

<style scoped lang="scss">
.main-container {
  position: relative;
  width: 100%;
  height: 100%;

  .floating-navigation {
    z-index: 9999; // 确保在最上层
    position: fixed; // 改为固定定位，不随滚动条滚动
    background: white;
    border-radius: 8px;
    box-shadow: 0 2px 10px rgba(0, 0, 0, 0.2);
    padding: 5px;
    min-width: 300px;
    cursor: move; // 鼠标显示为可移动状态

    .drag-handle {
      padding: 5px;
      text-align: center;
      border-bottom: 1px solid #eee;
      font-size: 18px;
      cursor: move;

      span {
        display: inline-block;
        padding: 0 10px;
      }
    }

    .nav-list {
      display: flex;
      flex-wrap: wrap; // 允许内容换行
      justify-content: flex-start;
      list-style: none;
      padding: 5px;
      margin: 0;

      li {
        margin: 3px;
        text-align: center;
      }
    }
  }

  .main-show {
    width: 100%;
    height: 100%;
  }
}
</style>
