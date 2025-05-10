/**
 * Time:2025/3/19 21:09 52
 * Name:fileUtil.ts
 * Path:src/utils
 * ProjectName:argus-src
 * Author:charlatans
 *
 *  Il n'ya qu'un héroïsme au monde :
 *     c'est de voir le monde tel qu'il est et de l'aimer.
 */

import { FILE_SIZE_UNIT_SIZE } from '@/constants/constants'
import { FILE_SIZE_UNIT_ENUM } from '@/constants/enums'
import type { FILE_SIZE_UNIT_STR } from '@/types/utils'

/**
 * 将字节数转换合适的单位
 * @param size 字节数
 * @param unit 单位
 * @param decimalPlaces 小数位数
 * @returns { size: number, unit: string } 转换后的大小和单位
 */
export const convertFileSize = (
  size: number,
  unit: FILE_SIZE_UNIT_STR | FILE_SIZE_UNIT_ENUM,
  decimalPlaces = 2
): { size: number; unit: string } => {
  if (size < 0) throw new Error('文件大小不能为负数')
  // 重新计算字节数
  const element = FILE_SIZE_UNIT_SIZE[FILE_SIZE_UNIT_ENUM[unit]]
  const values = Object.values(FILE_SIZE_UNIT_SIZE)

  // 选择合适单位
  let bestUnitIndex = 0
  let bestSize = element.bytes * size

  while (bestSize >= 1024 && bestUnitIndex < values.length - 1) {
    bestSize /= 1024
    bestUnitIndex++
  }

  const bestUnit = values[bestUnitIndex]
  const formattedNumber = parseFloat(bestSize.toFixed(decimalPlaces))

  return { size: formattedNumber, unit: bestUnit.name }
}
