import { ref, computed } from 'vue'
import { defineStore } from 'pinia'
import lodash from 'lodash'

export const useUuidStore = defineStore('uuid', () => {
  const _uuid: ref = ref('')

  const uuid = computed(() => {
    if (lodash.isEmpty(_uuid.value)) {
      _uuid.value = crypto.randomUUID()
    }
    return _uuid.value
  })

  return { uuid }
})
