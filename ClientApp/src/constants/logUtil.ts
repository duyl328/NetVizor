/**
 * Time:2024/8/30 下午2:28 44
 * Name:logUtil.ts
 * Path:src/constants
 * ProjectName:utopia-front-vue
 * Author:charlatans
 *
 *  Il n'ya qu'un héroïsme au monde :
 *     c'est de voir le monde tel qu'il est et de l'aimer.
 */

/**
 * 日志级别
 */
const level = {
  error: 0,
  warn: 1,
  info: 2,
  http: 3,
  verbose: 4,
  debug: 5,
  silly: 6
}

/**
 * 日志工具类常量
 */
const logUtil = {
  /**
   * 日志级别
   */
  level: level,
  ERROR: 'error',
  WARN: 'warn',
  INFO: 'info',
  HTTP: 'http',
  VERBOSE: 'verbose',
  DEBUG: 'debug',
  SILLY: 'silly'
  
}

export type Color =
  'primary'
  | 'success'
  | 'info'
  | 'warning'
  | 'danger'
  | 'error';

export const COLOR_MAP: Record<Color, string> = {
  primary: '#2d8cf0',
  success: '#19be6b',
  info: '#909399',
  warning: '#ff9900',
  danger: '#35495E',
  error: '#FF0000'
}
export default { logUtil, COLOR_MAP }
