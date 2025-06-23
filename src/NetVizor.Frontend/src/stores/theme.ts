// stores/theme.ts - 完善的主题存储
import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { darkTheme, lightTheme } from 'naive-ui'
import type { GlobalTheme, GlobalThemeOverrides } from 'naive-ui'

export const useThemeStore = defineStore('theme', () => {
  // 主题状态
  const isDark = ref(false)

  // 初始化主题（从本地存储读取）
  const initTheme = () => {
    const savedTheme = localStorage.getItem('theme')
    isDark.value = savedTheme === 'dark'

    // 同步到 HTML 元素
    if (isDark.value) {
      document.documentElement.classList.add('dark')
    } else {
      document.documentElement.classList.remove('dark')
    }
  }

  // 切换主题
  const toggleTheme = () => {
    isDark.value = !isDark.value
    localStorage.setItem('theme', isDark.value ? 'dark' : 'light')

    // 同步到 HTML 元素
    if (isDark.value) {
      document.documentElement.classList.add('dark')
    } else {
      document.documentElement.classList.remove('dark')
    }
  }

  // Naive UI 主题
  const theme = computed<GlobalTheme | null>(() => {
    return isDark.value ? darkTheme : null
  })

  // 主题覆盖配置
  const themeOverrides = computed<GlobalThemeOverrides>(() => {
    if (isDark.value) {
      // 暗色主题配置
      return {
        common: {
          primaryColor: '#3b82f6',
          primaryColorHover: '#60a5fa',
          primaryColorPressed: '#2563eb',
          primaryColorSuppl: '#1d4ed8',

          infoColor: '#06b6d4',
          infoColorHover: '#22d3ee',
          infoColorPressed: '#0891b2',

          successColor: '#22c55e',
          successColorHover: '#4ade80',
          successColorPressed: '#16a34a',

          warningColor: '#f59e0b',
          warningColorHover: '#fbbf24',
          warningColorPressed: '#d97706',

          errorColor: '#ef4444',
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
          primaryColor: '#3b82f6',
          primaryColorHover: '#2563eb',
          primaryColorPressed: '#1d4ed8',
          primaryColorSuppl: '#60a5fa',

          infoColor: '#06b6d4',
          infoColorHover: '#0891b2',
          infoColorPressed: '#0e7490',

          successColor: '#22c55e',
          successColorHover: '#16a34a',
          successColorPressed: '#15803d',

          warningColor: '#f59e0b',
          warningColorHover: '#d97706',
          warningColorPressed: '#b45309',

          errorColor: '#ef4444',
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

  return {
    isDark,
    theme,
    themeOverrides,
    initTheme,
    toggleTheme
  }
})
