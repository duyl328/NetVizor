<script setup lang="ts">
import CommandManager from '@/components/CommandManager.vue'
import type { CommandType } from '@/types/command'

const commands: CommandType[] = [
  {
    name: 'is_elevated_command',
    description: '是否有管理员权限',
    params: [],
    result: null,
  },
  {
    name: 'get_exe_directory_command',
    description: '获取程序所在目录',
    params: [],
    result: null,
  },
]
</script>

<template>
  <CommandManager :pro="commands"></CommandManager>
</template>

<style scoped></style>
