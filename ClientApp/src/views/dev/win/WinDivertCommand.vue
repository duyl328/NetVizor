<script setup lang="ts">
import CommandManager from '@/components/CommandManager.vue'
import type { CommandType } from '@/types/command'
import {onMounted,onUnmounted} from 'vue'
import CSharpBridgeV2 from "@/correspond/CSharpBridgeV2"


const bridge = CSharpBridgeV2.getBridge()
bridge.listen("showMessage",(data) => {
  console.log('11111111',data);
})

function func() {
  console.log('点击anniu')

  bridge.send('showMessage',{
    title: "来自前端的问候",
    content: "Hello C#!"
  },(data) => {
    console.log(data,'======');
    console.log(window.externalFunctions.__BRIDGE_LISTEN__FUNCTIONS__);
  })
}

const commands: CommandType[] = [
  {
    name: 'showMessage',
    description: '获取信息!!',
    params: [
      {
        name: 'title',
        label: '标题',
        type: 'text',
        value: 'Vue',
        placeholder: 'text'
      },{
        name: 'content',
        label: '标题',
        type: 'text',
        value: 'Hello C#!',
        placeholder: 'text'
      },
    ],
    result: null,
  },
]

</script>

<template>
  <CommandManager :pro="commands"></CommandManager>
</template>

<style scoped></style>
