<script setup lang="ts">
import CommandManager from '@/components/CommandManager.vue'
import type { CommandType } from '@/types/command'

const commands: CommandType[] = [
  {
    name: 'etwNetworkManger',
    description: 'ETW 内容测试',
    params: [],
    result: null,
  },
]
</script>

<template>
  <CommandManager :isUseWebSocket="true" :pro="commands"></CommandManager>
</template>

<style scoped></style>
