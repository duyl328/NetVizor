// stores/theme.ts - 增强版主题存储
import { defineStore } from 'pinia'
import { ref, computed, watch } from 'vue'
import { darkTheme, lightTheme } from 'naive-ui'
import type { GlobalTheme, GlobalThemeOverrides } from 'naive-ui'

export const useThemeStore = defineStore('theme', () => {
  // 主题状态
  const isDark = ref(false)

  // 是否跟随系统主题
  const followSystem = ref(false)

  // 系统主题媒体查询
  const systemPrefersDark = window.matchMedia('(prefers-color-scheme: dark)')

  // 应用主题到 DOM
  const applyTheme = (dark: boolean) => {
    if (dark) {
      document.documentElement.classList.add('dark')
      // 同时设置 data-theme 属性，方便 CSS 选择器使用
      document.documentElement.setAttribute('data-theme', 'dark')
    } else {
      document.documentElement.classList.remove('dark')
      document.documentElement.removeAttribute('data-theme')
    }
  }

  // 初始化主题
  const initTheme = () => {
    const savedTheme = localStorage.getItem('theme')
    const savedFollowSystem = localStorage.getItem('theme-follow-system')

    // 恢复跟随系统设置
    followSystem.value = savedFollowSystem === 'true'

    if (followSystem.value) {
      // 跟随系统主题
      isDark.value = systemPrefersDark.matches
    } else if (savedTheme) {
      // 使用保存的主题
      isDark.value = savedTheme === 'dark'
    } else {
      // 默认跟随系统
      isDark.value = systemPrefersDark.matches
      followSystem.value = true
    }

    applyTheme(isDark.value)

    // 监听系统主题变化
    systemPrefersDark.addEventListener('change', handleSystemThemeChange)
  }

  // 处理系统主题变化
  const handleSystemThemeChange = (e: MediaQueryListEvent) => {
    if (followSystem.value) {
      isDark.value = e.matches
      applyTheme(isDark.value)
    }
  }

  // 切换主题
  const toggleTheme = () => {
    isDark.value = !isDark.value
    followSystem.value = false // 手动切换后不再跟随系统

    localStorage.setItem('theme', isDark.value ? 'dark' : 'light')
    localStorage.setItem('theme-follow-system', 'false')

    applyTheme(isDark.value)
  }

  // 设置是否跟随系统
  const setFollowSystem = (follow: boolean) => {
    followSystem.value = follow
    localStorage.setItem('theme-follow-system', follow.toString())

    if (follow) {
      isDark.value = systemPrefersDark.matches
      applyTheme(isDark.value)
    }
  }

  // 监听主题变化
  watch(isDark, (newValue) => {
    if (!followSystem.value) {
      localStorage.setItem('theme', newValue ? 'dark' : 'light')
    }
  })

  // Naive UI 主题
  const theme = computed<GlobalTheme | null>(() => {
    return isDark.value ? darkTheme : null
  })

  // 主题覆盖配置 - 抽取共同配置
  const commonColors = {
    primaryColor: '#3b82f6',
    infoColor: '#06b6d4',
    successColor: '#22c55e',
    warningColor: '#f59e0b',
    errorColor: '#ef4444',
  }

  // 主题覆盖配置
  const themeOverrides = computed<GlobalThemeOverrides>(() => {
    const baseOverrides = {
      common: {
        ...commonColors,
        borderRadius: '8px',
        borderRadiusSmall: '6px',
      }
    }

    if (isDark.value) {
      // 暗色主题配置
      return {
        common: {
          ...baseOverrides.common,
          primaryColorHover: '#60a5fa',
          primaryColorPressed: '#2563eb',
          primaryColorSuppl: '#1d4ed8',

          infoColorHover: '#22d3ee',
          infoColorPressed: '#0891b2',

          successColorHover: '#4ade80',
          successColorPressed: '#16a34a',

          warningColorHover: '#fbbf24',
          warningColorPressed: '#d97706',

          errorColorHover: '#f87171',
          errorColorPressed: '#dc2626',

          textColorBase: '#f8fafc',
          textColor1: '#f1f5f9',
          textColor2: '#e2e8f0',
          textColor3: '#cbd5e1',

          dividerColor: '#334155',
          borderColor: '#334155',

          popoverColor: '#1e293b',
          cardColor: '#1e293b',
          modalColor: '#1e293b',
          bodyColor: '#0f172a',

          tableColor: '#1e293b',
          tableColorHover: 'rgba(59, 130, 246, 0.1)',
          tableColorStriped: '#334155',
        },

        Tabs: {
          colorSegment: 'rgba(51, 65, 85, 0.5)',
          tabTextColorSegment: '#cbd5e1',
          tabTextColorActiveSegment: '#f8fafc',
          tabTextColorHoverSegment: '#f1f5f9',
          tabColorSegment: 'rgba(15, 23, 42, 0.95)',
          tabBorderColor: '#334155',
          barColor: '#3b82f6',
        },

        Button: {
          textColorGhost: '#cbd5e1',
          textColorGhostHover: '#f1f5f9',
          textColorGhostPressed: '#e2e8f0',
          borderColor: '#334155',
          borderColorHover: '#475569',
        },

        Input: {
          color: 'rgba(51, 65, 85, 0.5)',
          colorFocus: 'rgba(51, 65, 85, 0.8)',
          textColor: '#f1f5f9',
          caretColor: '#3b82f6',
          border: '1px solid #334155',
          borderHover: '1px solid #475569',
          borderFocus: '1px solid #3b82f6',
          placeholderColor: '#64748b',
        }
      }
    } else {
      // 亮色主题配置
      return {
        common: {
          ...baseOverrides.common,
          primaryColorHover: '#2563eb',
          primaryColorPressed: '#1d4ed8',
          primaryColorSuppl: '#60a5fa',

          infoColorHover: '#0891b2',
          infoColorPressed: '#0e7490',

          successColorHover: '#16a34a',
          successColorPressed: '#15803d',

          warningColorHover: '#d97706',
          warningColorPressed: '#b45309',

          errorColorHover: '#dc2626',
          errorColorPressed: '#b91c1c',

          textColorBase: '#0f172a',
          textColor1: '#1e293b',
          textColor2: '#334155',
          textColor3: '#475569',

          dividerColor: '#e2e8f0',
          borderColor: '#e2e8f0',

          popoverColor: '#ffffff',
          cardColor: '#ffffff',
          modalColor: '#ffffff',
          bodyColor: '#f8fafc',

          tableColor: '#ffffff',
          tableColorHover: 'rgba(59, 130, 246, 0.08)',
          tableColorStriped: '#f8fafc',
        },

        Tabs: {
          colorSegment: 'rgba(241, 245, 249, 0.8)',
          tabTextColorSegment: '#475569',
          tabTextColorActiveSegment: '#0f172a',
          tabTextColorHoverSegment: '#1e293b',
          tabColorSegment: '#ffffff',
          tabBorderColor: '#e2e8f0',
          barColor: '#3b82f6',
        },

        Button: {
          textColorGhost: '#475569',
          textColorGhostHover: '#1e293b',
          textColorGhostPressed: '#334155',
          borderColor: '#e2e8f0',
          borderColorHover: '#cbd5e1',
        },

        Input: {
          color: 'rgba(241, 245, 249, 0.8)',
          colorFocus: '#ffffff',
          textColor: '#1e293b',
          caretColor: '#3b82f6',
          border: '1px solid #e2e8f0',
          borderHover: '1px solid #cbd5e1',
          borderFocus: '1px solid #3b82f6',
          placeholderColor: '#94a3b8',
        }
      }
    }
  })

  // 清理函数
  const cleanup = () => {
    systemPrefersDark.removeEventListener('change', handleSystemThemeChange)
  }

  return {
    isDark,
    followSystem,
    theme,
    themeOverrides,
    initTheme,
    toggleTheme,
    setFollowSystem,
    cleanup
  }
})
