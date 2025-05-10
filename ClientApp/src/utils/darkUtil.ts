/**
 * Time:2024/12/10 16:29 48
 * Name:darkUtil.ts
 * Path:src/utils
 * ProjectName:argus-src
 * Author:charlatans
 *
 *  Il n'ya qu'un héroïsme au monde :
 *     c'est de voir le monde tel qu'il est et de l'aimer.
 */
import { useDark, useToggle } from "@vueuse/core";
export const isDark = useDark();
export const toggleDark = useToggle(isDark)

export function changedTheme() {
  console.log(isDark.value);
  document.documentElement.removeAttribute("data-theme")
  if (isDark.value){
    document.documentElement.setAttribute("data-theme","light");
  }else{
    document.documentElement.setAttribute("data-theme","dark");
  }

  toggleDark()
}
