import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { useApplicationStore } from './application'
import { useProcessStore } from './processInfo'
import { ApplicationType } from '@/types/infoModel'
import { ProcessType } from '@/types/process'

export const useFilterStore = defineStore('filter', () => {
  // 过滤文本
  const filterText = ref('')
  
  // 是否启用过滤
  const isFiltering = ref(false)
  
  // 获取stores
  const applicationStore = useApplicationStore()
  const processStore = useProcessStore()
  
  // 检查文本是否匹配过滤条件
  const isTextMatch = (text: string): boolean => {
    if (!filterText.value || !isFiltering.value || !text) return false
    return text.toLowerCase().includes(filterText.value.toLowerCase())
  }
  
  // 检查应用是否匹配过滤条件
  const isApplicationMatch = (app: ApplicationType): boolean => {
    if (!filterText.value || !isFiltering.value || !app) return false
    
    const query = filterText.value.toLowerCase()
    
    // 检查软件名称
    if (app.productName && typeof app.productName === 'string' && app.productName.toLowerCase().includes(query)) return true
    
    // 检查进程名称
    if (app.processName && typeof app.processName === 'string' && app.processName.toLowerCase().includes(query)) return true
    
    // 检查公司名称
    if (app.companyName && typeof app.companyName === 'string' && app.companyName.toLowerCase().includes(query)) return true
    
    // 检查进程ID
    if (app.processIds && Array.isArray(app.processIds) && app.processIds.some(id => id && id.toString().includes(query))) return true
    
    return false
  }
  
  // 检查进程是否匹配过滤条件
  const isProcessMatch = (process: ProcessType): boolean => {
    if (!filterText.value || !isFiltering.value || !process) return false
    
    const query = filterText.value.toLowerCase()
    
    // 检查进程名称
    if (process.processName && typeof process.processName === 'string' && process.processName.toLowerCase().includes(query)) return true
    
    // 检查进程ID
    if (process.processId && process.processId.toString().includes(query)) return true
    
    // 检查连接信息
    if (process.connections && Array.isArray(process.connections) && process.connections.some(conn => {
      if (!conn) return false
      
      // 检查本地IP和端口
      if ((conn.localEndpoint?.address && typeof conn.localEndpoint.address === 'string' && conn.localEndpoint.address.includes(query)) || 
          (conn.localEndpoint?.port && conn.localEndpoint.port.toString().includes(query))) return true
      
      // 检查远程IP和端口
      if ((conn.remoteEndpoint?.address && typeof conn.remoteEndpoint.address === 'string' && conn.remoteEndpoint.address.includes(query)) || 
          (conn.remoteEndpoint?.port && conn.remoteEndpoint.port.toString().includes(query))) return true
      
      return false
    })) return true
    
    return false
  }
  
  // 过滤后的应用列表 - 真正的过滤，只显示匹配的项目
  const filteredApplications = computed(() => {
    // 如果没有启用过滤，显示所有应用
    if (!isFiltering.value || !filterText.value.trim()) {
      return applicationStore.appInfos
    }
    
    try {
      // 只返回匹配的应用
      return applicationStore.appInfos.filter(app => {
        if (!app) return false
        return isApplicationMatch(app)
      })
    } catch (error) {
      console.error('过滤应用时出错:', error)
      return applicationStore.appInfos
    }
  })
  
  // 过滤后的进程列表 - 真正的过滤，只显示匹配的项目
  const filteredProcesses = computed(() => {
    // 如果没有启用过滤，显示所有进程
    if (!isFiltering.value || !filterText.value.trim()) {
      return processStore.processInfos
    }
    
    try {
      // 只返回匹配的进程
      return processStore.processInfos.filter(process => {
        if (!process) return false
        return isProcessMatch(process)
      })
    } catch (error) {
      console.error('过滤进程时出错:', error)
      return processStore.processInfos
    }
  })
  
  // 设置过滤文本
  const setFilterText = (text: string) => {
    filterText.value = text
  }
  
  // 启用/禁用过滤
  const setFiltering = (enabled: boolean) => {
    isFiltering.value = enabled
  }
  
  // 切换过滤状态
  const toggleFiltering = () => {
    isFiltering.value = !isFiltering.value
  }
  
  // 清除过滤
  const clearFilter = () => {
    filterText.value = ''
    isFiltering.value = false
  }
  
  // 应用过滤（激活过滤功能）
  const applyFilter = () => {
    if (filterText.value.trim()) {
      isFiltering.value = true
    }
  }
  
  return {
    // 状态
    filterText,
    isFiltering,
    
    // 计算属性
    filteredApplications,
    filteredProcesses,
    
    // 方法
    isTextMatch,
    isApplicationMatch,
    isProcessMatch,
    setFilterText,
    setFiltering,
    toggleFiltering,
    clearFilter,
    applyFilter,
  }
})