/**
 * Time:2024/8/31 下午4:46 46
 * Name:logEnum.ts
 * Path:src/utils/logHelper
 * ProjectName:utopia-front-vue
 * Author:charlatans
 *
 *  Il n'ya qu'un héroïsme au monde :
 *     c'est de voir le monde tel qu'il est et de l'aimer.
 */

/**
 * 日志等级列表，重要等级越高数字越小
 */
export enum LogLevelEnum {
  /**
   *  v.
   */
  VERBOSE = 40,
  
  /**
   *  d.
   */
  DEBUG = 30,
  
  /**
   *  i.
   */
  INFO = 20,
  
  /**
   *  w.
   */
  WARN = 10,
  
  /**
   *  e.
   */
  ERROR = 0,
}

/**
 * 日志等级名称
 */
export const LevelName = {
  VERBOSE: 'VERBOSE',
  DEBUG: 'DEBUG',
  INFO: 'INFO',
  WARN: 'WARN',
  ERROR: 'ERROR'
}

/**
 * 日志样式
 */
enum logStyle {
  BackgroundColor = 10,
  TextColor = 20,
  BackgroundAndTextColor = 30
}

/**
 * 日志输出颜色
 */
enum logColor {

}

/**
 * 获取指定等级名称
 * @param level
 */
export function getLogLevelName (level: LogLevelEnum): string {
  switch (level) {
    case LogLevelEnum.VERBOSE:
      return LevelName.VERBOSE
    case LogLevelEnum.DEBUG:
      return LevelName.DEBUG
    case LogLevelEnum.INFO:
      return LevelName.INFO
    case LogLevelEnum.WARN:
      return LevelName.WARN
    case LogLevelEnum.ERROR:
      return LevelName.ERROR
  }
}
