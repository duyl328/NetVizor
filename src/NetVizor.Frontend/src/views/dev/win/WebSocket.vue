<script setup lang="ts">
import CommandManager from '@/components/CommandManager.vue'
import type { CommandType } from '@/types/command'

const commands: CommandType[] = [
  {
    name: 'CloseWebSocket',
    description: '关闭 WebSocket',
    params: [],
    result: null,
  },
  {
    name: 'OpenWebSocket',
    description: '打开 WebSocket',
    params: [],
    result: null,
  },
]
</script>

<template>
  <CommandManager :isUseWebSocket="true" :pro="commands"></CommandManager>
</template>

<style scoped></style>
