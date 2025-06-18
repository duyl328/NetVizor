/**
 * Time:2024/7/27 下午1:00 23
 * Name:BuiltInInstructionsView.type.ts
 * Path:src/types/views/dev/vue
 * ProjectName:utopia-front-vue
 * Author:charlatans
 *
 *  Il n'ya qu'un héroïsme au monde :
 *     c'est de voir le monde tel qu'il est et de l'aimer.
 */

/**
 * 子路由
 */
export type subRoute = {
  /**
   * 展示名字
   */
  displayName: string
  /**
   * 跳转路径
   */
  path: string
}

/**
 * 子路由列表
 */
export type subRouteList = subRoute[]
