// 预定义的渐变色方案
const gradientSchemes = [
  // 蓝色系（原始）
  { from: '#3b82f6', to: '#1d4ed8' },
  // 紫色系
  { from: '#8b5cf6', to: '#6d28d9' },
  // 青色系
  { from: '#06b6d4', to: '#0891b2' },
  // 绿色系
  { from: '#10b981', to: '#059669' },
  // 粉色系
  { from: '#ec4899', to: '#db2777' },
  // 橙色系
  { from: '#f97316', to: '#ea580c' },
  // 靛蓝色系
  { from: '#6366f1', to: '#4f46e5' },
  // 玫瑰色系
  { from: '#f43f5e', to: '#e11d48' },
  // 紫罗兰系
  { from: '#a78bfa', to: '#8b5cf6' },
  // 天蓝色系
  { from: '#38bdf8', to: '#0284c7' },
]

/**
 * 基于字符串生成稳定的颜色索引
 * @param str
 */
export const getColorIndex = (str: string): number => {
  let hash = 0
  for (let i = 0; i < str.length; i++) {
    const char = str.charCodeAt(i)
    hash = (hash << 5) - hash + char
    hash = hash & hash // Convert to 32bit integer
  }
  return Math.abs(hash) % gradientSchemes.length
}
export const getGradientColor = (str: string): { from: string; to: string } => {
  const gradientScheme = gradientSchemes[getColorIndex(str)]
  return {
    background: `linear-gradient(135deg, ${gradientScheme.from} 0%, ${gradientScheme.to} 100%)`,
  }
}
/**
 * 获取应用的渐变色
 * @param appName
 */
export const getAppGradient = (appName: string): string => {
  const colorIndex = getColorIndex(appName)
  const scheme = gradientSchemes[colorIndex]
  return `linear-gradient(135deg, ${scheme.from} 0%, ${scheme.to} 100%)`
}
