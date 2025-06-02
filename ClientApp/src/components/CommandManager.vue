<script setup lang="ts">
import { onMounted, type Ref, ref } from 'vue'
import type { CommandType } from '@/types/command'
import { logN } from '@/utils/logHelper/logUtils.ts'
import CSharpBridgeV2 from '@/correspond/CSharpBridgeV2'
import { useWebSocketStore } from '@/stores/websocketStore'

// 传递参数
const props = defineProps({
  pro: Array<CommandType>,
  isUseWebSocket: Boolean,
})
// 使用 ref 使数据变成响应式数据
const modules: Ref<CommandType[]> = ref([...(props.pro || [])])
const bridge = CSharpBridgeV2.getBridge()

const useWebSocket = useWebSocketStore()

const newVar = (data) => {
  logN.warning('后端返回的参数', module.name, data)
  module.result = JSON.stringify(data, null, 2)

  useWebSocket.unsubscribe(module.name)
}
/**
 * 指令触发
 * @param module
 */
const invokeCommand = async (module: CommandType) => {
  const params = Object.fromEntries(module.params.map((param) => [param.name, param.value]))
  logN.success('前端发送的参数', module.name, params)
  if (props.isUseWebSocket) {
    useWebSocket.registerHandler(module.name, newVar)

    useWebSocket.send(module.name)
  } else {
    try {
      bridge.send(module.name, params, (data) => {
        logN.warning('后端返回的参数', module.name, data)
        module.result = JSON.stringify(data, null, 2)
      })
    } catch (error) {
      console.error(err)
      module.result = `Error: ${error}`
    }
  }
}
</script>

<template>
  <div class="p-6 font-sans">
    <div class="flex flex-wrap gap-6 w-full justify-center">
      <div
        v-for="(module, index) in modules"
        :key="index"
        class="w-full border shadow-lg p-4 shadow-md rounded-md"
      >
        <h2 class="text-lg font-semibold mb-2 select-text pointer-events-auto">
          {{ module.name }}
        </h2>
        <p class="text-gray-600 mb-4">{{ module.description }}</p>

        <div class="w-full flex flex-row">
          <div
            v-for="(param, paramIndex) in module.params"
            :key="paramIndex"
            class="flex flex-col gap-1 w-full m-2"
          >
            <label :for="`${module.name}-${param.name}`" class="block font-bold text-red-400">
              {{ param.label }}
            </label>
            <input
              v-if="param.type === 'text'"
              :id="`${module.name}-${param.name}`"
              v-model="param.value"
              :placeholder="param.placeholder"
              type="text"
              class="w-full px-3 py-2 border border-gray-500 rounded-md focus:outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-300"
            />
            <input
              v-if="param.type === 'checkbox'"
              :id="`${module.name}-${param.name}`"
              v-model="param.value"
              type="checkbox"
              class="rounded checked:bg-blue-500"
            />
          </div>
        </div>

        <button
          @click="invokeCommand(module)"
          class="mt-4 w-full py-2 bg-blue-700 text-white rounded-md border-none cursor-pointer hover:bg-blue-600"
        >
          触发 {{ module.name }}
        </button>

        <div v-if="module.result" class="mt-4 p-3 bg-gray-100 border border-gray-300 rounded-md">
          <div class="font-semibold mb-2">结果</div>
          <div class="text-sm whitespace-pre-wrap select-text pointer-events-auto">
            {{ module.result }}
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
