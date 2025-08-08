import { defineStore } from 'pinia'
import { ref, computed } from 'vue'

export interface TrafficDataPoint {
  timestamp: number
  uploadSpeed: number
  downloadSpeed: number
}

export const useTrafficStore = defineStore('traffic', () => {
  // 存储实时网络流量数据
  const trafficHistory = ref<TrafficDataPoint[]>([])

  // 最大数据点数量（默认60个点，约20分钟的数据）
  const maxDataPoints = ref(60)

  // 数据收集间隔（毫秒）
  const dataInterval = ref(1000) // 20秒

  // 添加新的流量数据点
  const addTrafficData = (uploadSpeed: number, downloadSpeed: number) => {
    const now = Date.now()
    const newDataPoint: TrafficDataPoint = {
      timestamp: now,
      uploadSpeed,
      downloadSpeed
    }

    trafficHistory.value.push(newDataPoint)

    // 限制数据点数量，删除过期数据
    if (trafficHistory.value.length > maxDataPoints.value) {
      trafficHistory.value.shift()
    }

    // 可选：基于时间限制删除老数据（20-30分钟）
    const timeLimit = 30 * 60 * 1000 // 30分钟
    const cutoffTime = now - timeLimit

    trafficHistory.value = trafficHistory.value.filter(
      point => point.timestamp > cutoffTime
    )
  }

  // 获取当前上传速度
  const currentUploadSpeed = computed(() => {
    const latest = trafficHistory.value[trafficHistory.value.length - 1]
    return latest?.uploadSpeed || 0
  })

  // 获取当前下载速度
  const currentDownloadSpeed = computed(() => {
    const latest = trafficHistory.value[trafficHistory.value.length - 1]
    return latest?.downloadSpeed || 0
  })

  // 获取总的当前速度
  const currentTotalSpeed = computed(() => {
    return currentUploadSpeed.value + currentDownloadSpeed.value
  })

  // 清空历史数据
  const clearHistory = () => {
    trafficHistory.value = []
  }

  // 设置最大数据点数量
  const setMaxDataPoints = (max: number) => {
    maxDataPoints.value = max

    // 如果当前数据超过新的最大值，删除老数据
    if (trafficHistory.value.length > max) {
      trafficHistory.value = trafficHistory.value.slice(-max)
    }
  }

  // 获取指定时间范围内的数据
  const getDataInRange = (minutes: number): TrafficDataPoint[] => {
    const now = Date.now()
    const cutoffTime = now - (minutes * 60 * 1000)

    return trafficHistory.value.filter(point => point.timestamp > cutoffTime)
  }

  // 获取平均速度（最近N个数据点）
  const getAverageSpeed = (lastNPoints?: number): { upload: number; download: number } => {
    const dataToAnalyze = lastNPoints
      ? trafficHistory.value.slice(-lastNPoints)
      : trafficHistory.value

    if (dataToAnalyze.length === 0) {
      return { upload: 0, download: 0 }
    }

    const totalUpload = dataToAnalyze.reduce((sum, point) => sum + point.uploadSpeed, 0)
    const totalDownload = dataToAnalyze.reduce((sum, point) => sum + point.downloadSpeed, 0)

    return {
      upload: totalUpload / dataToAnalyze.length,
      download: totalDownload / dataToAnalyze.length
    }
  }

  // 获取峰值速度
  const getPeakSpeed = (): { upload: number; download: number; total: number } => {
    if (trafficHistory.value.length === 0) {
      return { upload: 0, download: 0, total: 0 }
    }

    const maxUpload = Math.max(...trafficHistory.value.map(p => p.uploadSpeed))
    const maxDownload = Math.max(...trafficHistory.value.map(p => p.downloadSpeed))
    const maxTotal = Math.max(...trafficHistory.value.map(p => p.uploadSpeed + p.downloadSpeed))

    return {
      upload: maxUpload,
      download: maxDownload,
      total: maxTotal
    }
  }

  return {
    // 状态
    trafficHistory,
    maxDataPoints,
    dataInterval,

    // 计算属性
    currentUploadSpeed,
    currentDownloadSpeed,
    currentTotalSpeed,

    // 方法
    addTrafficData,
    clearHistory,
    setMaxDataPoints,
    getDataInRange,
    getAverageSpeed,
    getPeakSpeed
  }
})
