import { fileURLToPath, URL } from 'node:url'

import { defineConfig ,loadEnv} from 'vite'
import vue from '@vitejs/plugin-vue'
import vueJsx from '@vitejs/plugin-vue-jsx'
import vueDevTools from 'vite-plugin-vue-devtools'
import AutoImport from 'unplugin-auto-import/vite'
import Components from 'unplugin-vue-components/vite'
import { ElementPlusResolver } from 'unplugin-vue-components/resolvers'
import UnoCSS from 'unocss/vite'


export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd())

  return {
    plugins: [
      vue(),
      vueJsx(),
      vueDevTools(),
      // Element 自动导入
      AutoImport({
        resolvers: [ElementPlusResolver()],
      }),
      Components({
        resolvers: [ElementPlusResolver()],
      }),
      UnoCSS()
    ],
    // 防止 Vite 掩盖 Rust 错误
    clearScreen: false,
    server: {
      port: 3000,
      strictPort: true,
      host: "0.0.0.0",
      watch: {
      },
    },
    build: {
    },
    resolve: {
      alias: {
        '@': fileURLToPath(new URL('./src', import.meta.url))
      },
    },
  }
})
