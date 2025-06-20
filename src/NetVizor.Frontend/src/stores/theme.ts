// stores/theme.ts
import { defineStore } from 'pinia'
import { ref, computed, watch } from 'vue'
import { darkTheme, lightTheme } from 'naive-ui'

export const useThemeStore = defineStore('theme', () => {
  // 初始读取系统设置或本地缓存
  const prefersDark = window.matchMedia('(prefers-color-scheme: dark)')
  const saved = localStorage.getItem('theme')
  const isDark = ref(saved ? saved === 'dark' : prefersDark.matches)

  // 当前 Naive UI 的主题对象
  const theme = computed(() => (isDark.value ? darkTheme : lightTheme))

  // 切换主题
  function toggleTheme() {
    isDark.value = !isDark.value
  }

  // 自动保存主题状态
  watch(isDark, (val) => {
    localStorage.setItem('theme', val ? 'dark' : 'light')
  })

  return {
    isDark,
    theme,
    toggleTheme,
  }
})
