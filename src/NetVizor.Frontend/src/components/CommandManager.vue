<script setup lang="ts">
import { onMounted, type Ref, ref } from 'vue'
import type { CommandType } from '@/types/command'
import { logN } from '@/utils/logHelper/logUtils.ts'
import CSharpBridgeV2 from '@/correspond/CSharpBridgeV2'
import { useWebSocketStore } from '@/stores/websocketStore'
import { NButton, NInput, NCheckbox } from 'naive-ui'

// 传递参数
const props = defineProps({
  pro: Array<CommandType>,
  isUseWebSocket: Boolean,
})
// 使用 ref 使数据变成响应式数据
const modules: Ref<CommandType[]> = ref([...(props.pro || [])])
const bridge = CSharpBridgeV2.getBridge()

const useWebSocket = useWebSocketStore()

const newVar = (data: any) => {
  logN.warning('后端返回的参数', modules.value[0]?.name, data)
  modules.value[0].result = JSON.stringify(data, null, 2)

  useWebSocket.unsubscribe(modules.value[0]?.name)
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
      bridge?.send(module.name, params, (data: any) => {
        logN.warning('后端返回的参数', module.name, data)
        module.result = JSON.stringify(data, null, 2)
      })
    } catch (error) {
      console.error(error)
      module.result = `Error: ${error}`
    }
  }
}
</script>

<template>
  <div class="command-manager">
    <div class="modules-container">
      <div
        v-for="(module, index) in modules"
        :key="index"
        class="module-card"
      >
        <h2 class="module-name">
          {{ module.name }}
        </h2>
        <p class="module-description">{{ module.description }}</p>

        <div class="params-container">
          <div
            v-for="(param, paramIndex) in module.params"
            :key="paramIndex"
            class="param-item"
          >
            <label :for="`${module.name}-${param.name}`" class="param-label">
              {{ param.label }}
            </label>
            <n-input
              v-if="param.type === 'text'"
              :id="`${module.name}-${param.name}`"
              v-model:value="param.value"
              :placeholder="param.placeholder"
              type="text"
              class="param-input"
            />
            <n-checkbox
              v-if="param.type === 'checkbox'"
              :id="`${module.name}-${param.name}`"
              v-model:checked="param.value"
              class="param-checkbox"
            />
          </div>
        </div>

        <n-button
          type="primary"
          block
          @click="invokeCommand(module)"
          class="trigger-button"
        >
          触发 {{ module.name }}
        </n-button>

        <div v-if="module.result" class="result-container">
          <div class="result-title">结果</div>
          <div class="result-content">
            {{ module.result }}
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.command-manager {
  padding: 1.5rem;
  font-family: sans-serif;
}

.modules-container {
  display: flex;
  flex-wrap: wrap;
  gap: 1.5rem;
  width: 100%;
  justify-content: center;
}

.module-card {
  width: 100%;
  border: 1px solid #e5e7eb;
  box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1), 0 2px 4px -1px rgba(0, 0, 0, 0.06);
  padding: 1rem;
  border-radius: 0.375rem;
}

.module-name {
  font-size: 1.125rem;
  font-weight: 600;
  margin-bottom: 0.5rem;
  user-select: text;
  pointer-events: auto;
}

.module-description {
  color: #6b7280;
  margin-bottom: 1rem;
}

.params-container {
  width: 100%;
  display: flex;
  flex-direction: row;
}

.param-item {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
  width: 100%;
  margin: 0.5rem;
}

.param-label {
  display: block;
  font-weight: bold;
  color: #ef4444;
}

.param-input {
  width: 100%;
}

.param-checkbox {
  margin-top: 0.25rem;
}

.trigger-button {
  margin-top: 1rem;
  width: 100%;
}

.result-container {
  margin-top: 1rem;
  padding: 0.75rem;
  background-color: #f3f4f6;
  border: 1px solid #e5e7eb;
  border-radius: 0.375rem;
}

.result-title {
  font-weight: 600;
  margin-bottom: 0.5rem;
}

.result-content {
  font-size: 0.875rem;
  white-space: pre-wrap;
  user-select: text;
  pointer-events: auto;
}
</style>
