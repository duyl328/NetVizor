<script setup lang="ts">
import { RouterView, useRoute, useRouter } from 'vue-router'
import PathUtils from '@/utils/pathUtils'
import type { subRoute, subRouteList } from '@/types/devIndex'
import StringUtils from '@/utils/stringUtils'
import ArrayUtils from '@/utils/arrayUtils'
import { onMounted, computed } from 'vue'
import gsap from 'gsap'
import { TextPlugin } from 'gsap/TextPlugin'

gsap.registerPlugin(TextPlugin)

const router = useRouter()
const route = useRoute()

const routes = import.meta.glob('@/views/dev/**/*.vue')

// region 子路由列表 点击、激活 处理
// 子路由列表
const subRouteLists: subRouteList = []

// 遍历路由page列表进行路由渲染
Object.keys(routes).map((path) => {
  if ('.vue' === PathUtils.extname(path).trim()) {
    const fileName = PathUtils.basename(path).split('.')[0]
    const items = {
      displayName: fileName,
      path: '/dev-index' + `/${StringUtils.toCustomCase(fileName).toLowerCase()}`, // 生成路径
    }
    subRouteLists.push(items)
  }
  return ''
})

/**
 * 列表点击调准
 */
function listClickJump(item: subRoute) {
  console.log(item.path)
  router.push({ path: item.path })
}

/**
 * 列表路径是否激活
 * @param path
 */
const isActive = (path: string) => {
  return route.path.includes(path)
}
// endregion

// region 无子路由数据自动跳转至主页
// 多久后跳转
let countdownSeconds = 9
// 指定跳转路径
const targetUrl = '/'
// 跳转定时器
let targetCountdown: number
// 路由列表是否为空【计算属性存在缓存】
const routerListIsEmpty = computed(() => {
  return ArrayUtils.isEmpty(subRouteLists)
})

onMounted(() => {
  // 如果数据为空，设置跳转倒计时
  if (routerListIsEmpty.value) {
    targetCountdown = setInterval(() => {
      gsap.fromTo(
        '#animated-text',
        { text: countdownSeconds.toString(), opacity: 1, y: -10 },
        {
          duration: 1,
          y: 150,
          text: (countdownSeconds--).toString(),
          ease: 'expo.in',
        },
      )
      if (countdownSeconds < 0) {
        jumpToHome()
      }
    }, 1500)
  }
})

/**
 * 跳转至主页
 */
function jumpToHome() {
  clearInterval(targetCountdown)
  router.replace({ path: targetUrl })
}

// endregion
</script>
<template>
  <div class="dev-container">
    <h1 class="dev-title">Dev</h1>
    <hr class="divider" />

    <div v-if="routerListIsEmpty" class="empty-container">
      <div class="empty-content">
        无数据!
        <br />
        <div class="countdown-container">
          <div class="countdown-number-wrapper">
            <p id="animated-text" class="countdown-number">9</p>
          </div>
          <p class="countdown-text">
            <span>秒后跳转至</span>
            <span class="home-link" @click="jumpToHome">主页</span>
          </p>
        </div>
      </div>
    </div>

    <div v-else class="content-wrapper">
      <ul class="route-list">
        <li
          v-for="(subRoute, index) in subRouteLists"
          :key="index"
          :class="['route-item', { active: isActive(subRoute.path) }]"
          @click="listClickJump(subRoute)"
        >
          {{ subRoute.displayName }}
        </li>
      </ul>

      <main class="main-content">
        <router-view />
      </main>
    </div>
  </div>
</template>

<style scoped>
.dev-container {
  height: auto;
  user-select: none;
}

.dev-title {
  color: #1e3a8a;
  font-weight: bold;
  font-family: sans-serif;
  font-size: 3rem;
  text-align: center;
  padding: 2.5rem 0;
}

.divider {
  border: none;
  border-top: 1px solid #e5e7eb;
  margin: 0;
}

.empty-container {
  background-color: #f8fafc;
  overflow: hidden;
}

.empty-content {
  text-align: center;
  font-family: serif;
  color: #ef4444;
  font-size: 3.75rem;
  padding-bottom: 20rem;
}

.countdown-container {
  display: inline-flex;
  transform: translate(-20rem, 2.5rem);
  font-size: 1.5rem;
  min-height: 10rem;
  height: 10rem;
  max-height: 10rem;
  color: #1e3a8a;
  justify-content: center;
  align-items: center;
}

.countdown-number-wrapper {
  height: 6rem;
  overflow: hidden;
  text-align: center;
}

.countdown-number {
  font-size: 2.25rem;
}

.countdown-text {
  margin-left: 1rem;
}

.home-link {
  text-decoration: underline;
  cursor: pointer;
  margin-left: 0.375rem;
}

.content-wrapper {
  display: flex;
  flex-direction: column;
  margin-top: 1.25rem;
  margin-left: auto;
  margin-right: auto;
  width: 75%;
}

.route-list {
  display: none;
  margin-right: 5rem;
  list-style: none;
  padding: 0;
}

@media (min-width: 1024px) {
  .route-list {
    display: block;
  }
}

.route-item {
  list-style: none;
  padding: 0.25rem;
  margin: 0.25rem;
  transition: all 0.3s ease-in-out;
  user-select: none;
  cursor: pointer;
  color: #374151;
}

.route-item:hover {
  color: #fbbf24;
}

.route-item.active {
  font-weight: bold;
  text-decoration: underline;
  font-size: 1.5rem;
}

.main-content {
  display: flex;
  flex-direction: column;
  margin-top: 1.25rem;
  overflow-y: auto;
}
</style>
