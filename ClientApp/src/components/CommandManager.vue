<script setup lang="ts">
import { type Ref, ref } from 'vue'
import { baseInvoke } from '@/utils/commandUtil.ts'
import type { CommandType } from '@/types/command'
import { logN } from '@/utils/logHelper/logUtils.ts'

// 传递参数
const props = defineProps({
  pro: Array<CommandType>,
})
// 使用 ref 使数据变成响应式数据
const modules: Ref<CommandType[]> = ref([...(props.pro || [])])

/**
 * 指令触发
 * @param module
 */
const invokeCommand = async (module: CommandType) => {
  const params = Object.fromEntries(module.params.map((param) => [param.name, param.value]))
  logN.success('前端发送的参数', module.name, params)

  try {
    const s = baseInvoke(module.name, params)
    s.then((res) => {
      logN.warning('后端返回的参数', module.name, res)
      module.result = JSON.stringify(res, null, 2)
    }).catch((err) => {
      console.error(err)
      module.result = `Error: ${err.message}`
    })
  } catch (error) {
    console.error(err)
    module.result = `Error: ${error}`
  }
}
</script>


<template>
  <div class="container">
    <div class="modules-grid">
      <div v-for="(module, index) in modules" :key="index" class="module-card">
        <h2 class="module-title">{{ module.name }}</h2>
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
            <input
              v-if="param.type === 'text'"
              :id="`${module.name}-${param.name}`"
              v-model="param.value"
              :placeholder="param.placeholder"
              type="text"
              class="text-input"
            />
            <input
              v-if="param.type === 'checkbox'"
              :id="`${module.name}-${param.name}`"
              v-model="param.value"
              type="checkbox"
              class="checkbox-input"
            />
          </div>
        </div>

        <button
          @click="invokeCommand(module)"
          class="action-button"
        >
          触发 {{ module.name }}
        </button>

        <div v-if="module.result" class="result-container">
          <h3 class="result-title">结果</h3>
          <pre class="result-content">{{ module.result }}</pre>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped lang="scss">
.container {
  padding: 1.5rem; // p-6
  font-family: sans-serif;

  .modules-grid {
    display: flex;
    flex-wrap: wrap;
    gap: 1.5rem; // gap-6
    width: 100%;
    justify-content: center;

    .module-card {
      width: 100%;
      padding: 1rem; // p-4
      box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1); // shadow-md
      border-radius: 0.375rem; // rounded-md

      .module-title {
        font-size: 1.125rem; // text-lg
        font-weight: 600; // font-semibold
        margin-bottom: 0.5rem; // mb-2
        user-select: text;     /* 允许选中 */
        pointer-events: auto;  /* 确保可以响应鼠标操作 */

      }

      .module-description {
        color: #718096; // text-gray-600
        margin-bottom: 1rem; // mb-4
      }

      .params-container {
        width: 100%;
        display: flex;
        flex-direction: row;

        .param-item {
          display: flex;
          flex-direction: column;
          gap: 0.25rem; // space-y-1
          width: 100%;
          margin: 0.5rem; // m-2

          .param-label {
            display: block;
            font-weight: 700; // font-bold
            color: #f56565; // text-red-400
          }

          .text-input {
            width: 100%;
            padding: 0.5rem 0.75rem; // px-3 py-2
            border: 1px solid #e2e8f0; // border border-gray-300
            border-radius: 0.375rem; // rounded-md

            &:focus {
              outline: none;
              border-color: #4299e1;
              box-shadow: 0 0 0 3px rgba(66, 153, 225, 0.5);
            }
          }

          .checkbox-input {
            border-radius: 0.25rem; // rounded

            &:checked {
              background-color: #4299e1;
            }
          }
        }
      }

      .action-button {
        margin-top: 1rem; // mt-4
        width: 100%;
        padding: 0.5rem 0; // py-2
        background-color: #2b6cb0; // bg-blue-700
        color: white;
        border-radius: 0.375rem; // rounded-md
        border: none;
        cursor: pointer;

        &:hover {
          background-color: #3182ce; // bg-blue-600
        }
      }

      .result-container {
        margin-top: 1rem; // mt-4
        padding: 0.75rem; // p-3
        background-color: #f7fafc; // bg-gray-100
        border: 1px solid #e2e8f0;
        border-radius: 0.375rem; // rounded-md

        .result-title {
          font-weight: 600; // font-semibold
          margin-bottom: 0.5rem; // mb-2
        }

        .result-content {
          font-size: 0.875rem; // text-sm
          white-space: pre-wrap;
          user-select: text;     /* 允许选中 */
          pointer-events: auto;  /* 确保可以响应鼠标操作 */
        }
      }
    }
  }
}
</style>
