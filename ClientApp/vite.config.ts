import { fileURLToPath, URL } from 'node:url'

import { defineConfig ,loadEnv} from 'vite'
import vue from '@vitejs/plugin-vue'
import vueJsx from '@vitejs/plugin-vue-jsx'
import vueDevTools from 'vite-plugin-vue-devtools'


export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd())
  const host = env.VITE_TAURI_DEV_HOST;
  
  return {
    plugins: [
      vue(),
      vueJsx(),
      vueDevTools(),
    ],
    // 防止 Vite 掩盖 Rust 错误
    clearScreen: false,
    server: {
      port: 3000,
      // Tauri 需要一个固定端口，如果该端口不可用，则失败
      strictPort: true,
      // 如果设置了 Tauri 期望的主机，请使用它
      host: host || false,
      hmr: host
        ? {
          protocol: 'ws',
          host,
          port: 3000,
        }
        : undefined,
      
      watch: {
        // 告诉 Vite 忽略关注 'src-tauri'
        ignored: ['**/src-tauri/**'],
      },
    },
    // 以 'envPrefix' 项开头的 env 变量将通过 'import.meta.env' 暴露在 tauri 的源代码中。
    envPrefix: ['VITE_', 'TAURI_ENV_*'],
    build: {
      // Tauri 在 Windows 上使用 Chromium，在 macOS 和 Linux 上使用 WebKit
      target:
        process.env.TAURI_ENV_PLATFORM == 'windows'
          ? 'chrome105'
          : 'safari13',
      // 不要缩小调试版本
      minify: !process.env.TAURI_ENV_DEBUG ? 'esbuild' : false,
      // 生成用于调试版本的 SourceMap
      sourcemap: !!process.env.TAURI_ENV_DEBUG,
    },
    resolve: {
      alias: {
        '@': fileURLToPath(new URL('./src', import.meta.url))
      },
    },
  }
})
