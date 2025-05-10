/**
 * Time:2024/12/20 19:52 50
 * Name:nameUtil.ts
 * Path:src/utils
 * ProjectName:argus-src
 * Author:charlatans
 *
 *  Il n'ya qu'un héroïsme au monde :
 *     c'est de voir le monde tel qu'il est et de l'aimer.
 */


export function toCamelCase(str: string): string {
  return str
    .replace(/_./g, (match: string) => match.charAt(1).toUpperCase())
    .replace(/^./, (match: string) => match.toLowerCase())
}
