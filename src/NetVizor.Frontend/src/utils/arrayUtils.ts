/**
 * Time:2024/7/27 上午10:36 15
 * Name:arrayUtils.ts
 * Path:src/utils
 * ProjectName:utopia-front-vue
 * Author:charlatans
 *
 *  Il n'ya qu'un héroïsme au monde :
 *     c'est de voir le monde tel qu'il est et de l'aimer.
 */
import type { arrayUtilsNotEmptyCallback } from '@/types/utils'

export default class ArrayUtils {
  
  /**
   * 指定数组或未知元素是否为 `null` 或 `empty`
   * @param arr
   */
  static isEmpty (arr: any[] | null): boolean {
    return arr === null || arr.length === 0
  }
}
