/**
 * Time:2025/5/18 13:38 35
 * Name:uno.config.ts
 * Path:
 * ProjectName:ClientApp
 * Author:charlatans
 *
 *  Il n'ya qu'un héroïsme au monde :
 *     c'est de voir le monde tel qu'il est et de l'aimer.
 */
import { defineConfig } from 'unocss'
import presetMini from '@unocss/preset-mini'

export default defineConfig({
  theme: {
    colors: {
      // ...
    }
  },
  presets: [
    // 基础预设
    presetMini(),
    // ...other presets
  ],
  rules: [
    // border-x, border-y, border-t/b/r/l
    [/^border$/, () => ({ border: '1px solid #ccc' })],
    [/^border-(\d+)$/, ([, d]) => ({ border: `${d}px solid #ccc` })],
    [/^border-([tblr])$/, ([, dir]) => ({ [`border-${dir}`]: '1px solid #ccc' })],
    [/^border-(solid|dashed|dotted)$/, ([, style]) => ({ 'border-style': style })],

    // border-style
    [/^border-(solid|dashed|dotted)$/, ([, style]) => ({ 'border-style': style })],

    // Shadow rules
    [/^shadow$/, () => ({ 'box-shadow': '0 1px 3px rgba(0, 0, 0, 0.1)' })],
    [/^shadow-(sm|md|lg|xl|2xl)$/, ([, size]) => {
      const shadows = {
        sm: '0 1px 2px rgba(0, 0, 0, 0.05)',
        md: '0 4px 6px rgba(0, 0, 0, 0.1)',
        lg: '0 10px 15px rgba(0, 0, 0, 0.15)',
        xl: '0 20px 25px rgba(0, 0, 0, 0.2)',
        '2xl': '0 25px 50px rgba(0, 0, 0, 0.25)',
      }
      return { 'box-shadow': shadows[size] }
    }],
    [/^shadow-inner$/, () => ({ 'box-shadow': 'inset 0 2px 4px rgba(0, 0, 0, 0.06)' })],
    [/^shadow-none$/, () => ({ 'box-shadow': 'none' })],

  ],
  shortcuts: [
    ['border-box', 'border border-gray-300 rounded-md'],
    ['card-shadow', 'shadow-md rounded-lg bg-white p-4'],
  ],
})
