<template>
  <div class="app-container">
    <!-- 顶部标题栏组件 -->
    <TitleBar />

    <!-- 路由视图区域 -->
    <main>
      <slot /> <!-- 页面内容会被插入到这里 -->
    </main>
  </div>
</template>

<script setup lang="ts">
import { onMounted } from 'vue'
import { useThemeStore } from '@/stores/theme'
import TitleBar from '@/components/TitleBar.vue'

const themeStore = useThemeStore()

onMounted(() => {
  // 初始化主题
  themeStore.initTheme()

  // 页面加载完成后启用过渡动画
  setTimeout(() => {
    document.body.classList.add('theme-ready')
  }, 100)
})
</script>

<style scoped>
/* 主容器样式 */
.app-container {
  min-height: 100vh;
  background: linear-gradient(135deg, var(--bg-primary) 0%, var(--bg-secondary) 100%);
  color: var(--text-primary);
  user-select: none;
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
}

/* 主内容区域 */
main {
  min-height: calc(100vh - 60px); /* 减去标题栏高度 */
}

/* 滚动条样式 - 使用 CSS 变量 */
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

/* 全局 Naive UI 组件样式覆盖 */
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

/* 其他按钮类型样式 */
:deep(.n-button--info-type) {
  background: var(--accent-secondary);
  border-color: var(--accent-secondary);
  color: white;
}

:deep(.n-button--warning-type) {
  background: var(--accent-warning);
  border-color: var(--accent-warning);
  color: white;
}

:deep(.n-button--error-type) {
  background: var(--accent-error);
  border-color: var(--accent-error);
  color: white;
}

:deep(.n-button--success-type) {
  background: var(--accent-success);
  border-color: var(--accent-success);
  color: white;
}
</style>
