<template>
  <div class="min-h-screen bg-gray-50">
    <!-- 性能信息 -->
    <n-card
      size="small"
      class="performance-info fixed top-5 right-5 z-50 max-w-xs"
      :bordered="false"
    >
      <div class="text-xs font-mono text-gray-600">
        渲染: {{ renderedItemsCount }} / {{ totalItemsCount }} 项目
      </div>
    </n-card>

    <!-- 主容器 -->
    <div class="max-w-2xl mx-auto bg-white min-h-screen shadow-xl">
      <!-- 页面头部 -->
      <div class="bg-gradient-to-br from-blue-500 to-purple-600 text-white p-5 text-center relative overflow-hidden">
        <div class="absolute inset-0 bg-gradient-to-r from-transparent via-white to-transparent opacity-10 animate-pulse" />
        <h1 class="text-2xl font-semibold relative z-10">统一虚拟滚动列表演示</h1>
        <p class="text-sm mt-2 opacity-90 relative z-10">表头和列表项统一管理，支持大量数据</p>
      </div>

      <!-- 虚拟列表容器 -->
      <div ref="containerRef" class="relative w-full">
        <div ref="wrapperRef" class="relative w-full">
          <!-- 占位空间 -->
          <div
            class="w-full pointer-events-none"
            :style="{ height: `${allItems.length * ITEM_HEIGHT}px` }"
          />

          <!-- 视口 -->
          <div class="absolute top-0 left-0 w-full">
            <!-- 表头项 -->
            <template v-for="(item, index) in visibleItems" :key="`${item.type}-${item.sectionIndex}-${item.itemIndex || 0}-${visibleRange.start + index}`">
              <div
                v-if="item.type === 'header'"
                class="virtual-list-item header"
                :class="getCategoryClass(item.category)"
                :style="getItemStyle(visibleRange.start + index)"
                @click="() => toggleSection(item.sectionIndex!)"
              >
                <div class="header-content">
                  <component
                    :is="getCategoryIcon(item.category)"
                    :size="18"
                    class="mr-2 flex-shrink-0"
                  />
                  <span class="font-semibold text-base">{{ item.title }}</span>
                  <span class="text-xs opacity-70 ml-2">{{ item.count }} 项</span>
                </div>
                <ChevronDown
                  :size="16"
                  class="transition-transform duration-300 flex-shrink-0"
                  :class="{ 'rotate-[-90deg]': item.collapsed }"
                />
              </div>

              <!-- 列表项 -->
              <div
                v-else
                class="virtual-list-item item"
                :style="getItemStyle(visibleRange.start + index)"
                @click="() => handleItemClick(item)"
              >
                <div class="item-indicator" />
                <n-tag
                  :bordered="false"
                  round
                  size="small"
                  type="info"
                  class="item-number"
                >
                  {{ item.globalIndex }}
                </n-tag>
                <div class="item-content flex-1 text-sm text-gray-700 ml-3">
                  {{ item.content }}
                </div>
                <div class="rating flex space-x-1 ml-3">
                  <span
                    v-for="star in item.originalData?.rating"
                    :key="star"
                    class="text-yellow-400 text-sm"
                  >
                    ★
                  </span>
                </div>
              </div>
            </template>
          </div>
        </div>
      </div>
    </div>

    <!-- 吸顶表头 -->
    <Teleport to="body">
      <Transition name="sticky-header">
        <div
          v-if="stickyHeader.item"
          class="sticky-header-container"
          :style="getStickyHeaderStyle()"
          @click="() => toggleSection(stickyHeader.item!.sectionIndex!)"
        >
          <div class="sticky-header" :class="getCategoryClass(stickyHeader.item.category)">
            <div class="header-content">
              <component
                :is="getCategoryIcon(stickyHeader.item.category)"
                :size="18"
                class="mr-2 flex-shrink-0"
              />
              <span class="font-semibold text-base">{{ stickyHeader.item.title }}</span>
              <span class="text-xs opacity-70 ml-2">{{ stickyHeader.item.count }} 项</span>
            </div>
            <ChevronDown
              :size="16"
              class="transition-transform duration-300 flex-shrink-0"
              :class="{ 'rotate-[-90deg]': stickyHeader.item.collapsed }"
            />
          </div>
        </div>
      </Transition>
    </Teleport>

    <!-- 滚动提示 -->
    <Transition name="scroll-indicator">
      <n-card
        v-if="scrollIndicatorVisible"
        size="small"
        class="scroll-indicator fixed bottom-5 right-5 pointer-events-none"
        :bordered="false"
      >
        <div class="text-xs text-gray-600">继续滚动查看更多</div>
      </n-card>
    </Transition>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, nextTick, watch } from 'vue'
import { NCard, NTag } from 'naive-ui'
import {
  ChevronDown,
  Coffee,
  UtensilsCrossed,
  Salad,
  Cake,
  Wine,
  Menu
} from 'lucide-vue-next'

// 类型定义
interface DataItem {
  content: string
  rating: number
}

interface Dataset {
  title: string
  data: DataItem[]
  collapsed?: boolean
  category: 'breakfast' | 'main' | 'vegetarian' | 'dessert' | 'drinks'
}

interface VirtualItem {
  type: 'header' | 'item'
  title?: string
  count?: number
  sectionIndex?: number
  category?: string
  collapsed?: boolean
  content?: string
  globalIndex?: number
  itemIndex?: number
  originalData?: DataItem
}

interface VisibleRange {
  start: number
  end: number
}

interface StickyHeaderState {
  item: VirtualItem | null
  translateY: number
}

// 常量
const ITEM_HEIGHT = 60
const BUFFER_SIZE = 10
const MIN_RENDER_INTERVAL = 32

// 响应式数据
const containerRef = ref<HTMLDivElement>()
const wrapperRef = ref<HTMLDivElement>()
const collapsedSections = ref<Set<number>>(new Set())
const visibleRange = ref<VisibleRange>({ start: 0, end: 0 })
const stickyHeader = ref<StickyHeaderState>({ item: null, translateY: 0 })
const scrollIndicatorVisible = ref(false)

// 其他状态
const lastScrollTop = ref(0)
const scrollDirection = ref<'up' | 'down'>('down')
const lastRenderTime = ref(0)
let scrollTimer: NodeJS.Timeout | null = null

// 工具函数
const debounce = (func: Function, wait: number) => {
  let timeout: NodeJS.Timeout
  return function executedFunction(...args:unknown[]) {
    const later = () => {
      clearTimeout(timeout)
      func(...args)
    }
    clearTimeout(timeout)
    timeout = setTimeout(later, wait)
  }
}

const throttle = (func: Function, delay: number) => {
  let timeoutId: NodeJS.Timeout
  let lastExecTime = 0

  return function (...args:unknown[]) {
    const currentTime = Date.now()

    if (currentTime - lastExecTime > delay) {
      func.apply(this, args)
      lastExecTime = currentTime
    } else {
      clearTimeout(timeoutId)
      timeoutId = setTimeout(() => {
        func.apply(this, args)
        lastExecTime = Date.now()
      }, delay - (currentTime - lastExecTime))
    }
  }
}

// 生成测试数据
const generateLargeDataset = (prefix: string, count: number): DataItem[] => {
  const foods = [
    '宫保鸡丁', '麻婆豆腐', '红烧肉', '鱼香肉丝', '青椒肉丝', '糖醋里脊',
    '回锅肉', '蛋炒饭', '扬州炒饭', '牛肉面', '兰州拉面', '炸酱面',
    '担担面', '热干面', '重庆小面', '小笼包', '煎饺', '蒸饺',
    '包子', '馒头', '花卷', '烧饼', '油条', '豆浆', '胡辣汤', '煎蛋',
    '培根', '香肠', '吐司', '三明治', '汉堡', '披萨', '意面', '拉面'
  ]

  const items: DataItem[] = []
  for (let i = 0; i < count; i++) {
    const food = foods[i % foods.length]
    const variant = Math.floor(i / foods.length) + 1
    const rating = Math.floor(Math.random() * 5) + 1
    items.push({
      content: `${prefix}${food}${variant > 1 ? ` (${variant})` : ''}`,
      rating
    })
  }
  return items
}

// 数据集
const datasets: Dataset[] = [
  {
    title: '早餐推荐',
    data: generateLargeDataset('精品', 200),
    collapsed: false,
    category: 'breakfast'
  },
  {
    title: '主食类',
    data: generateLargeDataset('招牌', 800),
    collapsed: false,
    category: 'main'
  },
  {
    title: '素食专区',
    data: generateLargeDataset('健康', 300),
    collapsed: false,
    category: 'vegetarian'
  },
  {
    title: '甜品点心',
    data: generateLargeDataset('甜蜜', 500),
    collapsed: false,
    category: 'dessert'
  },
  {
    title: '饮品推荐',
    data: generateLargeDataset('特调', 400),
    collapsed: false,
    category: 'drinks'
  }
]

// 计算属性
const allItems = computed(() => {
  const items: VirtualItem[] = []

  datasets.forEach((dataset, sectionIndex) => {
    // 添加表头
    items.push({
      type: 'header',
      title: dataset.title,
      count: dataset.data.length,
      sectionIndex,
      category: dataset.category,
      collapsed: collapsedSections.value.has(sectionIndex)
    })

    // 添加列表项（如果未折叠）
    if (!collapsedSections.value.has(sectionIndex)) {
      dataset.data.forEach((item, itemIndex) => {
        items.push({
          type: 'item',
          content: item.content,
          sectionIndex,
          itemIndex,
          globalIndex: itemIndex + 1,
          originalData: item
        })
      })
    }
  })

  return items
})

const visibleItems = computed(() => {
  return allItems.value.slice(visibleRange.value.start, visibleRange.value.end)
})

const totalItemsCount = computed(() =>
  datasets.reduce((sum, d) => sum + d.data.length, 0)
)

const renderedItemsCount = computed(() =>
  visibleRange.value.end - visibleRange.value.start
)

// 分类相关函数
const getCategoryIcon = (category?: string) => {
  switch (category) {
    case 'breakfast': return Coffee
    case 'main': return UtensilsCrossed
    case 'vegetarian': return Salad
    case 'dessert': return Cake
    case 'drinks': return Wine
    default: return Menu
  }
}

const getCategoryClass = (category?: string) => {
  switch (category) {
    case 'breakfast':
      return 'bg-orange-50 text-orange-600 border-orange-200'
    case 'main':
      return 'bg-blue-50 text-blue-600 border-blue-200'
    case 'vegetarian':
      return 'bg-green-50 text-green-600 border-green-200'
    case 'dessert':
      return 'bg-pink-50 text-pink-600 border-pink-200'
    case 'drinks':
      return 'bg-purple-50 text-purple-600 border-purple-200'
    default:
      return 'bg-gray-50 text-gray-600 border-gray-200'
  }
}

// 样式函数
const getItemStyle = (index: number) => {
  return {
    position: 'absolute' as const,
    top: 0,
    left: 0,
    width: '100%',
    height: `${ITEM_HEIGHT}px`,
    transform: `translateY(${index * ITEM_HEIGHT}px)`,
    transition: 'transform 0.2s ease, background-color 0.2s ease'
  }
}

const getStickyHeaderStyle = () => {
  return {
    position: 'fixed' as const,
    top: 0,
    left: '50%',
    width: '100%',
    maxWidth: '32rem',
    zIndex: 1000,
    transform: `translateX(-50%) translateY(${stickyHeader.value.translateY}px)`,
    transition: 'transform 0.3s ease'
  }
}

// 核心函数
const getVisibleRange = (): VisibleRange => {
  if (!containerRef.value || allItems.value.length === 0) {
    return { start: 0, end: 0 }
  }

  const scrollTop = window.pageYOffset || document.documentElement.scrollTop
  const windowHeight = window.innerHeight
  const containerTop = containerRef.value.getBoundingClientRect().top + scrollTop
  const containerHeight = allItems.value.length * ITEM_HEIGHT

  // 检测滚动方向
  scrollDirection.value = scrollTop > lastScrollTop.value ? 'down' : 'up'
  lastScrollTop.value = scrollTop

  // 特殊处理：如果容器很小，强制显示所有内容
  if (allItems.value.length <= 10) {
    return { start: 0, end: allItems.value.length }
  }

  const viewportTop = scrollTop
  const viewportBottom = scrollTop + windowHeight
  const listTop = containerTop
  const listBottom = containerTop + containerHeight

  // 检查是否有交集
  if (listBottom < viewportTop - windowHeight || listTop > viewportBottom + windowHeight) {
    return { start: 0, end: Math.min(10, allItems.value.length) }
  }

  // 计算可见范围
  const visibleStartInList = Math.max(0, viewportTop - listTop)
  const visibleEndInList = Math.min(containerHeight, viewportBottom - listTop)

  let startIndex = Math.floor(visibleStartInList / ITEM_HEIGHT)
  let endIndex = Math.ceil(visibleEndInList / ITEM_HEIGHT)

  startIndex = Math.max(0, Math.min(startIndex, allItems.value.length - 1))
  endIndex = Math.max(startIndex + 1, Math.min(endIndex, allItems.value.length))

  // 动态缓冲区
  const dynamicBuffer = scrollDirection.value === 'down'
    ? { top: Math.floor(BUFFER_SIZE * 0.3), bottom: BUFFER_SIZE }
    : { top: BUFFER_SIZE, bottom: Math.floor(BUFFER_SIZE * 0.3) }

  const bufferedStart = Math.max(0, startIndex - dynamicBuffer.top)
  const bufferedEnd = Math.min(allItems.value.length, endIndex + dynamicBuffer.bottom)

  if (bufferedStart >= bufferedEnd || bufferedStart >= allItems.value.length) {
    return { start: 0, end: Math.min(10, allItems.value.length) }
  }

  return { start: bufferedStart, end: bufferedEnd }
}

const updateStickyHeader = () => {
  if (!containerRef.value) return

  const scrollTop = window.pageYOffset || document.documentElement.scrollTop
  const containerTop = containerRef.value.getBoundingClientRect().top + scrollTop

  let currentStickyHeader: VirtualItem | null = null
  let currentStickyIndex = -1

  for (let i = 0; i < allItems.value.length; i++) {
    const item = allItems.value[i]
    if (item.type === 'header') {
      const headerTop = containerTop + i * ITEM_HEIGHT

      if (headerTop <= scrollTop) {
        currentStickyHeader = item
        currentStickyIndex = i
      } else {
        break
      }
    }
  }

  // 查找下一个表头
  let nextHeaderIndex = -1
  if (currentStickyIndex >= 0) {
    for (let i = currentStickyIndex + 1; i < allItems.value.length; i++) {
      if (allItems.value[i].type === 'header') {
        nextHeaderIndex = i
        break
      }
    }
  }

  let translateY = 0
  if (currentStickyHeader && nextHeaderIndex >= 0) {
    const nextHeaderTop = containerTop + nextHeaderIndex * ITEM_HEIGHT
    const pushDistance = nextHeaderTop - scrollTop
    if (pushDistance < ITEM_HEIGHT) {
      translateY = pushDistance - ITEM_HEIGHT
    }
  }

  stickyHeader.value = {
    item: currentStickyHeader,
    translateY
  }
}

const shouldUpdate = (newStart: number, newEnd: number): boolean => {
  const now = Date.now()
  if (now - lastRenderTime.value < MIN_RENDER_INTERVAL) {
    return false
  }

  if (newStart === visibleRange.value.start && newEnd === visibleRange.value.end) {
    return false
  }

  if (allItems.value.length <= 10) {
    return true
  }

  const overlapStart = Math.max(visibleRange.value.start, newStart)
  const overlapEnd = Math.min(visibleRange.value.end, newEnd)
  const overlapSize = Math.max(0, overlapEnd - overlapStart)
  const currentSize = visibleRange.value.end - visibleRange.value.start

  if (currentSize > 0 && overlapSize / currentSize > 0.7) {
    const startDiff = Math.abs(newStart - visibleRange.value.start)
    const endDiff = Math.abs(newEnd - visibleRange.value.end)

    if (startDiff <= 2 && endDiff <= 2) {
      return false
    }
  }

  return true
}

const render = () => {
  const newRange = getVisibleRange()

  if (!shouldUpdate(newRange.start, newRange.end)) {
    updateStickyHeader()
    return
  }

  visibleRange.value = newRange
  lastRenderTime.value = Date.now()
  updateStickyHeader()
}

// 事件处理
const toggleSection = (sectionIndex: number) => {
  const newSet = new Set(collapsedSections.value)
  if (newSet.has(sectionIndex)) {
    newSet.delete(sectionIndex)
  } else {
    newSet.add(sectionIndex)
  }
  collapsedSections.value = newSet

  // 延迟调整滚动位置
  setTimeout(() => {
    render()
  }, 100)
}

const handleItemClick = (item: VirtualItem) => {
  console.log('Item clicked:', item)
}

// 滚动处理
const handleScroll = throttle(() => {
  render()

  // 滚动提示
  scrollIndicatorVisible.value = true
  if (scrollTimer) clearTimeout(scrollTimer)
  scrollTimer = setTimeout(() => {
    scrollIndicatorVisible.value = false
  }, 1000)
}, 32)

// 窗口大小变化处理
const handleResize = debounce(() => {
  visibleRange.value = { start: -1, end: -1 }
  render()
}, 300)

// 生命周期
onMounted(() => {
  window.addEventListener('scroll', handleScroll, { passive: true })
  window.addEventListener('resize', handleResize)

  // 初始渲染
  nextTick(() => {
    setTimeout(render, 100)
  })
})

onUnmounted(() => {
  window.removeEventListener('scroll', handleScroll)
  window.removeEventListener('resize', handleResize)
  if (scrollTimer) clearTimeout(scrollTimer)
})

// 监听 allItems 变化
watch(allItems, () => {
  nextTick(() => {
    render()
  })
}, { deep: true })
</script>

<style scoped>
.virtual-list-item {
  @apply border-b border-gray-100 flex items-center justify-between px-5 py-4 cursor-pointer select-none;
  transition: all 0.2s ease;
}

.virtual-list-item.header {
  @apply border-b-2 backdrop-blur-sm font-semibold;
}

.virtual-list-item.header:hover {
  @apply shadow-md transform -translate-y-px;
}

.virtual-list-item.item {
  @apply bg-white relative overflow-hidden;
}

.virtual-list-item.item:hover {
  @apply bg-gray-50;
}

.item-indicator {
  @apply absolute left-0 top-0 h-full w-1 bg-gradient-to-b from-blue-400 to-purple-500 transform -translate-x-full transition-transform duration-300;
}

.virtual-list-item.item:hover .item-indicator {
  @apply translate-x-0;
}

.header-content {
  @apply flex items-center;
}

.sticky-header-container {
  @apply cursor-pointer;
}

.sticky-header {
  @apply border-b-2 backdrop-blur-sm cursor-pointer select-none flex items-center justify-between px-5 py-4 shadow-lg;
}

.performance-info {
  @apply bg-black bg-opacity-80 text-white;
}

.scroll-indicator {
  @apply bg-black bg-opacity-80 text-white;
}

/* 过渡动画 */
.sticky-header-enter-active,
.sticky-header-leave-active {
  transition: all 0.3s ease;
}

.sticky-header-enter-from,
.sticky-header-leave-to {
  opacity: 0;
  transform: translateX(-50%) translateY(-100%);
}

.scroll-indicator-enter-active,
.scroll-indicator-leave-active {
  transition: opacity 0.3s ease;
}

.scroll-indicator-enter-from,
.scroll-indicator-leave-to {
  opacity: 0;
}

/* 移动端优化 */
@media (max-width: 768px) {
  .performance-info {
    @apply hidden;
  }

  .sticky-header-container {
    @apply left-0 transform-none w-full max-w-none;
  }

  .virtual-list-item.header,
  .sticky-header {
    @apply px-4 py-3 text-sm;
  }

  .virtual-list-item.item {
    @apply px-4 py-3;
    height: 55px;
  }
}
</style>
