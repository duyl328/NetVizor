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
  subscriptionType: 'ApplicationInfo'
  interval: number
}

export { ResponseModel, SubscriptionInfo }
