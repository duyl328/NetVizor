<script setup lang="ts">
import { RouterView, useRoute, useRouter } from 'vue-router'
import PathUtils from '@/utils/pathUtils'
import type { subRoute, subRouteList } from '@/types/devIndex'
import StringUtils from '@/utils/stringUtils'
import ArrayUtils from '@/utils/arrayUtils'
import { onMounted } from 'vue'
import gsap from 'gsap'
import { TextPlugin } from 'gsap/TextPlugin'
import { computed } from 'vue'

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
  <div class="root-container">
    <h1 class="title">Dev</h1>
    <hr />

    <div v-if="routerListIsEmpty" class="empty-container">
      <div class="empty-message">
        无数据!
        <br />
        <div class="countdown-box">
          <div class="countdown-top">
            <p id="animated-text" class="countdown-number">9</p>
          </div>
          <p class="jump-message">
            <span>秒后跳转至</span>
            <span class="jump-link" @click="jumpToHome">主页</span>
          </p>
        </div>
      </div>
    </div>

    <div v-else class="main-content">
      <ul class="nav-list">
        <li
          class="nav-item"
          v-for="(subRoute, index) in subRouteLists"
          :class="{ active: isActive(subRoute.path) }"
          :key="index"
          @click="listClickJump(subRoute)"
        >
          {{ subRoute.displayName }}
        </li>
      </ul>
      <main class="router-content">
        <router-view />
      </main>
    </div>
  </div>
</template>

<style scoped lang="scss">
.root-container {
  height: auto;
  user-select: none;
}

.title {
  color: #1e3a8a; // tailwind's text-blue-900
  font-weight: bold;
  font-family: sans-serif;
  font-size: 3rem;
  text-align: center;
  padding: 2.5rem 0;
}

.empty-container {
  background-color: #f8fafc; // slate-50
  overflow: hidden;
}

.empty-message {
  text-align: center;
  font-family: serif;
  color: #ef4444; // red-500
  font-size: 3.75rem;
  padding-bottom: 20rem;
}

.countdown-box {
  display: inline-flex;
  transform: translate(-20rem, 2.5rem); // 原 translate-x-80 translate-y-10
  font-size: 1.5rem;
  min-height: 10rem;
  height: 10rem;
  max-height: 10rem;
  color: #1e3a8a;
  justify-content: center;
  align-items: center;
}

.countdown-top {
  height: 6rem;
  overflow: hidden;
  text-align: center;
}

.countdown-number {
  font-size: 2.25rem;
}

.jump-message {
  margin-left: 1rem;
}

.jump-link {
  text-decoration: underline;
  cursor: pointer;
  margin-left: 0.375rem;
}

.main-content {
  display: flex;
  margin-top: 1.25rem;
  flex-direction: column;
  margin-inline: auto;
  width: 75%; // 相当于 lg:w-9/12
}

.nav-list {
  margin-right: 5rem;
  display: none;

  @media (min-width: 1024px) {
    display: block;
  }
}

.nav-item {
  list-style: none;
  padding: 0.25rem;
  margin: 0.25rem;
  transition: all 0.3s ease;
  user-select: none;
  cursor: pointer;
  color: #4b5563; // gray-700

  &:hover {
    color: #facc15; // yellow-300
  }

  &.active {
    font-weight: bold;
    text-decoration: underline;
    font-size: 1.5rem;
  }
}

.router-content {
  display: flex;
  flex-direction: column;
  margin: 1.25rem auto auto;
  overflow-y: auto;
}
</style>
