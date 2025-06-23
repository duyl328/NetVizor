<template>
  <div ref="appContainer" class="app-container">
    <!-- 顶部标题栏组件 -->
    <TitleBar />

    <!-- 路由视图区域 -->
    <main>
      <slot /> <!-- 页面内容会被插入到这里 -->
    </main>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, watch, nextTick, computed } from 'vue'
import { useThemeStore } from '@/stores/theme'
import TitleBar from '@/components/TitleBar.vue'

const themeStore = useThemeStore()
const isDark = computed(() => themeStore.isDark)

// 引用app容器元素
const appContainer = ref<HTMLElement | null>(null)

// 监听主题变化并更新CSS类
watch(isDark, () => {
  nextTick(() => {
    updateThemeClass()
  })
}, { immediate: true })

// 更新主题CSS类
const updateThemeClass = () => {
  if (appContainer.value) {
    if (isDark.value) {
      appContainer.value.classList.add('dark-theme')
    } else {
      appContainer.value.classList.remove('dark-theme')
    }
  }
}

onMounted(() => {
  // 初始化主题类
  updateThemeClass()

  // 页面加载完成后启用过渡动画
  setTimeout(() => {
    document.body.classList.add('theme-ready')
  }, 100)
})
</script>

<style scoped>
/* 主题色彩变量 */
.app-container {
  /* 暗色主题 */
  --bg-primary: #0f172a;
  --bg-secondary: #1e293b;
  --bg-tertiary: #334155;
  --bg-quaternary: #475569;
  --bg-overlay: rgba(15, 23, 42, 0.95);
  --bg-glass: rgba(30, 41, 59, 0.8);
  --bg-card: rgba(51, 65, 85, 0.5);
  --bg-hover: rgba(59, 130, 246, 0.1);
  --bg-selected: rgba(59, 130, 246, 0.2);

  --text-primary: #f8fafc;
  --text-secondary: #f1f5f9;
  --text-tertiary: #e2e8f0;
  --text-quaternary: #cbd5e1;
  --text-muted: #94a3b8;
  --text-disabled: #64748b;

  --border-primary: #334155;
  --border-secondary: rgba(51, 65, 85, 0.3);
  --border-tertiary: rgba(71, 85, 105, 0.3);
  --border-hover: rgba(59, 130, 246, 0.5);
  --border-focus: #3b82f6;

  --accent-primary: #3b82f6;
  --accent-secondary: #06b6d4;
  --accent-success: #22c55e;
  --accent-warning: #f59e0b;
  --accent-error: #ef4444;
  --accent-purple: #8b5cf6;

  --shadow-sm: 0 1px 2px 0 rgba(0, 0, 0, 0.05);
  --shadow-md: 0 4px 6px -1px rgba(0, 0, 0, 0.1);
  --shadow-lg: 0 10px 15px -3px rgba(0, 0, 0, 0.1);
  --shadow-xl: 0 20px 25px -5px rgba(0, 0, 0, 0.1);

  --backdrop-blur: blur(20px);
  --transition: all 0.2s ease;
}

/* 亮色主题变量 */
.app-container:not(.dark-theme) {
  --bg-primary: #ffffff;
  --bg-secondary: #f8fafc;
  --bg-tertiary: #f1f5f9;
  --bg-quaternary: #e2e8f0;
  --bg-overlay: rgba(255, 255, 255, 0.95);
  --bg-glass: rgba(248, 250, 252, 0.8);
  --bg-card: rgba(241, 245, 249, 0.8);
  --bg-hover: rgba(59, 130, 246, 0.08);
  --bg-selected: rgba(59, 130, 246, 0.15);

  --text-primary: #0f172a;
  --text-secondary: #1e293b;
  --text-tertiary: #334155;
  --text-quaternary: #475569;
  --text-muted: #64748b;
  --text-disabled: #94a3b8;

  --border-primary: #e2e8f0;
  --border-secondary: rgba(226, 232, 240, 0.8);
  --border-tertiary: rgba(203, 213, 225, 0.6);
  --border-hover: rgba(59, 130, 246, 0.3);
  --border-focus: #3b82f6;

  --shadow-sm: 0 1px 2px 0 rgba(0, 0, 0, 0.05);
  --shadow-md: 0 4px 6px -1px rgba(0, 0, 0, 0.1);
  --shadow-lg: 0 10px 15px -3px rgba(0, 0, 0, 0.1);
  --shadow-xl: 0 20px 25px -5px rgba(0, 0, 0, 0.1);
}

/* 根容器样式 */
.app-container {
  min-height: 100vh;
  background: linear-gradient(135deg, var(--bg-primary) 0%, var(--bg-secondary) 100%);
  color: var(--text-primary);
  user-select: none;
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
  transition: var(--transition);
}

/* 滚动条样式 */
::-webkit-scrollbar {
  width: 6px;
  height: 6px;
}

::-webkit-scrollbar-track {
  background: transparent;
}

::-webkit-scrollbar-thumb {
  background: var(--border-tertiary);
  border-radius: 3px;
}

::-webkit-scrollbar-thumb:hover {
  background: var(--border-hover);
}

/* 自定义Naive UI组件样式 */
:deep(.n-input .n-input__input-el) {
  background: var(--bg-card);
  border-color: var(--border-tertiary);
  color: var(--text-secondary);
}

:deep(.n-input:hover .n-input__input-el) {
  border-color: var(--border-hover);
}

:deep(.n-input.n-input--focus .n-input__input-el) {
  border-color: var(--accent-primary);
}

:deep(.n-button) {
  border-color: var(--border-tertiary);
  color: var(--text-quaternary);
}

:deep(.n-button:hover) {
  border-color: var(--border-hover);
  color: var(--text-secondary);
}

:deep(.n-button--primary-type) {
  background: var(--accent-primary);
  border-color: var(--accent-primary);
  color: white;
}

:deep(.n-button--primary-type:hover) {
  background: #1d4ed8;
  border-color: #1d4ed8;
}

:deep(.n-button--info-type) {
  background: var(--accent-secondary);
  border-color: var(--accent-secondary);
  color: white;
}

:deep(.n-button--info-type:hover) {
  background: #0891b2;
  border-color: #0891b2;
}

:deep(.n-button--warning-type) {
  background: var(--accent-warning);
  border-color: var(--accent-warning);
  color: white;
}

:deep(.n-button--warning-type:hover) {
  background: #d97706;
  border-color: #d97706;
}

/* 主题切换过渡效果 - 只在页面加载完成后启用 */
body.theme-ready * {
  transition:
    background-color 0.3s ease,
    border-color 0.3s ease,
    color 0.3s ease,
    box-shadow 0.3s ease;
}

/* 初始加载时禁用所有过渡 */
body:not(.theme-ready) * {
  transition: none !important;
}
</style>
