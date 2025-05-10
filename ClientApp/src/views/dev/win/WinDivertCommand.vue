<script setup lang="ts">
import CommandManager from '@/components/CommandManager.vue'
import type { CommandType } from '@/types/command'

const commands: CommandType[] = [
  {
    name: 'win_divert_command',
    description: '显示所有网络连接信息',
    params: [],
    result: null,
  },
  {
    name: 'monitor_network_with_windivert_command',
    description: '使用WinDivert监控网络数据包',
    params: [],
    result: null,
  },
  {
    name: 'recv_packet_command',
    description: '安全接收数据包的辅助函数',
    params: [],
    result: null,
  },
  {
    name: 'recv_packet_command1',
    description: '外部包接口！',
    params: [],
    result: null,
  },
  {
    name: 'get_all_tcp_info',
    description: '获取所有 TCP 信息!',
    params: [],
    result: null,
  },
  {
    name: 'get_more_info',
    description: 'get_more_info',
    params: [
      {
        name: 'id',
        label: 'id',
        type: 'text',
        value: 'Vue',
        placeholder: 'text'
      }
    ],
    result: null,
  },
]
</script>

<template>
  <CommandManager :pro="commands"></CommandManager>
</template>

<style scoped></style>
