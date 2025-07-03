<script setup lang="ts">
import { useThemeVars } from 'naive-ui'
import { ref, watch } from 'vue'
import lodash from 'lodash'
import StringUtils from '@/utils/stringUtils'

const themeVars = useThemeVars()
const text = 'Naive UI 全量使用 TypeScript 编写，和你的 TypeScript 项目无缝衔接'
const patterns = ref([])

const input = ref('')
watch(input, (value) => {
  if (StringUtils.isBlank(value)) {
    patterns.value = []
  } else {
    patterns.value = [value]
  }
})
</script>

<template>
  <n-highlight
    :text="text"
    :patterns="patterns"
    :highlight-style="{
      padding: '0 1px',
      borderRadius: themeVars.borderRadius,
      display: 'inline-block',
      color: themeVars.baseColor,
      background: themeVars.primaryColor,
      transition: `all .3s ${themeVars.cubicBezierEaseInOut}`,
    }"
  />
  <n-input v-model:value="input"></n-input>
</template>

<style scoped></style>
