/**
 * Time:2025/5/3 19:12 15
 * Name:index.ts
 * Path:src/commands
 * ProjectName:net-src
 * Author:charlatans
 *
 *  Il n'ya qu'un héroïsme au monde :
 *     c'est de voir le monde tel qu'il est et de l'aimer.
 */
import { invoke } from '@tauri-apps/api/core'

import { greetCommand } from '@/constants/command'

/**
 * 问候
 * @param name
 */
export function greet(name: string) {
  return invoke<string>(greetCommand, { name })
}
