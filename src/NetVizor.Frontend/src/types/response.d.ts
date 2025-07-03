/**
 * 响应模型
 */
interface ResponseModel {
  success: boolean
  data: unknown
  message: string
}

/**
 * 订阅信息
 */
interface SubscriptionInfo {
  // ApplicationInfo ProcessInfo
  subscriptionType: string
  interval: number
}

interface SubscriptionProcessInfo extends SubscriptionInfo {
  // 订阅的进程列表
  processIds: number[]
}

export { ResponseModel, SubscriptionInfo,SubscriptionProcessInfo }
