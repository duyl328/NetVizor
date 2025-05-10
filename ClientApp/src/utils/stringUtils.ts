/**
 * Time:2024/7/26 下午9:56 48
 * Name:stringExtension.ts
 * Path:src/utils
 * ProjectName:utopia-front-vue
 * Author:charlatans
 * @description 字符串工具类
 *
 *  Il n'ya qu'un héroïsme au monde :
 *     c'est de voir le monde tel qu'il est et de l'aimer.
 */

export default class StringUtils {
  /**
   * 是否为 `null` 或 `空字符串`
   * @param str
   */
  static isBlank(str: string | null | undefined): boolean {
    if (str === null || str === undefined) return true
    return StringUtils.isEmpty(str) || str!.trim().length === 0
  }

  /**
   * 此字符序列为空（不包含任何字符），则返回 `true`
   * @param str
   */
  static isEmpty(str: string | null | undefined) {
    return str === null || str === undefined || str.length === 0
  }

  /**
   * 将字符串转换为小驼峰命名（camelCase）
   * @param str
   */
  static toCamelCase(str: string): string {
    return str
      .replace(/^\w|[A-Z]|\b\w|\s+/g, (match: string, index: number) => {
        if (index === 0 || match.trim().length > 0) {
          return match.toUpperCase()
        }
        return ''
      })
      .replace(/^[A-Z]/, (match) => match.toLowerCase())
  }

  /**
   * 将字符串转换为大驼峰命名（PascalCase）
   * @param str
   */
  static toPascalCase(str: string): string {
    return (
      str
        // 使用正则表达式处理非中文字符
        .replace(/^\w|[A-Z]|\b\w|\s+/g, (match: string, index: number) => {
          // 处理首字母大写，保留中文字符不变
          if (index === 0 || match.trim().length > 0) {
            return match.toUpperCase()
          }
          return ''
        })
        .replace(/^[a-z]/, (match: string) => match.toUpperCase())
    ) // 确保首字母大写
  }

  /**
   * 将字符串转换为指定字符连接的格式
   * @param str
   * @param separator 连接字符，例如 "_" 或 "-"
   */
  static toCustomCase(str: string, separator: string = '-'): string {
    if (StringUtils.isEmpty(str)) return ''
    if (StringUtils.isBlank(str)) return str
    return str
      .replace(/([a-z])([A-Z])/g, `$1${separator}$2`)
      .replace(/([A-Z])([A-Z][a-z])/g, `$1${separator}$2`)
  }

  /**
   * 对比两个文本字符是否相同
   *
   * 原生的 === 字符串对比
   * 和 计算哈希值后对比，原生性能非常高，100w 汉字差距在10倍以上
   */
  static equals(str1: string, str2: string): boolean {
    console.log(str1.length)
    return str1 === str2
    const s1 = StringUtils.hashString(str1)
    const s2 = StringUtils.hashString(str2)
    return s1 === s2
  }

  static hashString(str: string): number {
    let hash = 0
    for (let i = 0; i < str.length; i++) {
      hash = (hash << 5) - hash + str.charCodeAt(i)
      hash |= 0 // Convert to 32bit integer
    }
    return hash
  }
}
