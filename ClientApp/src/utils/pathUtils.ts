/**
 * Time:2024/7/27 上午11:25 48
 * Name:pathUtils.ts
 * Path:src/utils
 * ProjectName:utopia-front-vue
 * Author:charlatans
 *
 *  Il n'ya qu'un héroïsme au monde :
 *     c'est de voir le monde tel qu'il est et de l'aimer.
 */

export default class PathUtils {
  /**
   * 使用平台特定的分隔符将所有给定的路径段连接在一起.
   * @param {...string[]} paths - 要连接的路径段。
   */
  static join (...paths: string[]): string {
    const s = paths.join('/').replace(/\/+/g, '/').trim()
    return s === '/' ? '' : s
  }
  
  /**
   * 返回路径的目录名称.
   * @param {string} path - 完整路径
   */
  static dirname (path: string): string {
    return path.replace(/\/[^/]*$/, '')
  }
  
  /**
   * 返回路径的扩展名，从最后一个“.”到路径最后一部分的字符串结尾.
   * @param {string} path - 需要转换的路径
   */
  static extname (path: string): string {
    const match = path.match(/(\.[^/]*)$/)
    return match ? match[0] : ''
  }
  
  /**
   * 返回文件名.
   * @param {string} path - The path to evaluate.
   * @returns {string} The basename.
   */
  static basename (path: string): string {
    return path.replace(/^.*[/\\]/, '')
  }
  
}
