/**
 * Time:2025/3/19 21:59 41
 * Name:constants.ts
 * Path:src/constants
 * ProjectName:argus-src
 * Author:charlatans
 *
 *  Il n'ya qu'un héroïsme au monde :
 *     c'est de voir le monde tel qu'il est et de l'aimer.
 */

import { FILE_SIZE_UNIT_ENUM } from '@/constants/enums'


/**
 * 文件大小单位和字节的对应关系
 * @param name 单位名称
 * @param bytes 字节
 */
export const FILE_SIZE_UNIT_SIZE: Record<string, { name: string; bytes: number }> = {
  B: { name: FILE_SIZE_UNIT_ENUM.B, bytes: 1 },
  KB: { name: FILE_SIZE_UNIT_ENUM.KB, bytes: 1024 },
  MB: { name: FILE_SIZE_UNIT_ENUM.MB, bytes: 1024 * 1024 },
  GB: { name: FILE_SIZE_UNIT_ENUM.GB, bytes: 1024 * 1024 * 1024 },
  TB: { name: FILE_SIZE_UNIT_ENUM.TB, bytes: 1024 * 1024 * 1024 * 1024 }
} as const
