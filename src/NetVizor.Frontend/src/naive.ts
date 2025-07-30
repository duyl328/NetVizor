// naive.ts
import {
  create,
  darkTheme,
  lightTheme,
} from 'naive-ui'

const isDark = ref(false) // 用于控制主题状态，可接入 pinia 或 localStorage

export function createNaiveUI() {
  return create({
    theme: isDark.value ? darkTheme : lightTheme,
  })
}
