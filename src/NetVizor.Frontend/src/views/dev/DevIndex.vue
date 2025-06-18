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
  <div class="h-auto select-none">
    <h1 class="text-blue-900 font-bold font-sans text-[3rem] text-center py-10">Dev</h1>
    <hr />

    <div v-if="routerListIsEmpty" class="bg-slate-50 overflow-hidden">
      <div class="text-center font-serif text-red-500 text-[3.75rem] pb-[20rem]">
        无数据!
        <br />
        <div
          class="inline-flex translate-x-[-20rem] translate-y-10 text-[1.5rem] min-h-[10rem] h-[10rem] max-h-[10rem] text-blue-900 justify-center items-center"
        >
          <div class="h-[6rem] overflow-hidden text-center">
            <p id="animated-text" class="text-[2.25rem]">9</p>
          </div>
          <p class="ml-4">
            <span>秒后跳转至</span>
            <span class="underline cursor-pointer ml-1.5" @click="jumpToHome">主页</span>
          </p>
        </div>
      </div>
    </div>

    <div v-else class="flex flex-col mt-5 mx-auto w-9/12">
      <ul class="hidden lg:block mr-20">
        <li
          v-for="(subRoute, index) in subRouteLists"
          :key="index"
          :class="[
            'list-none p-1 m-1 transition-all duration-300 ease-in-out select-none cursor-pointer text-gray-700 hover:text-yellow-300',
            isActive(subRoute.path) && 'font-bold underline text-[1.5rem]',
          ]"
          @click="listClickJump(subRoute)"
        >
          {{ subRoute.displayName }}
        </li>
      </ul>

      <main class="flex flex-col mt-5 overflow-y-auto">
        <router-view />
      </main>
    </div>
  </div>
</template>
