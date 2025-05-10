<script setup lang="ts">
import CommandManager from '@/components/CommandManager.vue'
import type { CommandType } from '@/types/command'

const commands: CommandType[] = [
  {
    name: 'greet',
    description: '查询exif信息',
    params: [
      {
        name: 'name',
        label: 'name',
        type: 'text',
        value: 'Vue',
        placeholder: 'string'
      }
    ],
    result: null
  }
]
</script>

<template>
  <CommandManager :pro="commands"></CommandManager>
</template>

<style scoped></style>
