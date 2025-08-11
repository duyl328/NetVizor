import { fileURLToPath, URL } from 'node:url'

import { defineConfig ,loadEnv} from 'vite'
import vue from '@vitejs/plugin-vue'
import vueJsx from '@vitejs/plugin-vue-jsx'
import vueDevTools from 'vite-plugin-vue-devtools'
import AutoImport from 'unplugin-auto-import/vite'
import { NaiveUiResolver } from 'unplugin-vue-components/resolvers'
import Components from 'unplugin-vue-components/vite'


export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd())

  return {
    plugins: [
      vue(),
      AutoImport({
        imports: [
          'vue',
          {
            'naive-ui': [
              'useDialog',
              'useMessage',
              'useNotification',
              'useLoadingBar'
            ]
          }
        ],
        // 生成类型声明文件
        dts: true
      }),
      Components({
        resolvers: [NaiveUiResolver()],
        // 生成类型声明文件
        dts: true
      })
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
      // 打包优化配置
      rollupOptions: {
        output: {
          // 手动分割 chunks
          manualChunks: (id) => {
            // node_modules 中的第三方库
            if (id.includes('node_modules')) {
              // Vue 生态
              if (id.includes('vue') || id.includes('vue-router') || id.includes('pinia')) {
                return 'vue-vendor'
              }
              // UI 库
              if (id.includes('naive-ui')) {
                return 'ui-vendor'
              }
              // 图表相关
              if (id.includes('echarts') || id.includes('vue-echarts')) {
                return 'charts-vendor'
              }
              // 图标库
              if (id.includes('@vicons') || id.includes('vicons')) {
                return 'icons-vendor'
              }
              // 其他第三方库
              if (id.includes('axios') || id.includes('lodash') || id.includes('gsap') || id.includes('pako')) {
                return 'utils-vendor'
              }
              // 其他 node_modules
              return 'vendor'
            }
            // 业务代码分割
            if (id.includes('src/views')) {
              return 'views'
            }
            if (id.includes('src/components')) {
              return 'components'
            }
          },
          // 资产文件名称策略
          assetFileNames: (assetInfo) => {
            const info = assetInfo.name.split('.')
            const ext = info[info.length - 1]
            if (/\.(png|jpe?g|gif|svg|webp|ico)$/.test(assetInfo.name)) {
              return 'assets/images/[name].[hash][extname]'
            } else if (/\.(woff2?|eot|ttf|otf)$/.test(assetInfo.name)) {
              return 'assets/fonts/[name].[hash][extname]'
            }
            return `assets/[ext]/[name].[hash][extname]`
          },
          // 入口文件名称
          entryFileNames: 'assets/js/[name].[hash].js',
          // chunk 文件名称
          chunkFileNames: 'assets/js/[name].[hash].js'
        }
      },
      // 压缩配置
      minify: 'esbuild',
      // 移除控制台输出
      esbuild: {
        drop: ['console', 'debugger']
      },
      // 警告阈值
      chunkSizeWarningLimit: 1000,
      // CSS 代码分割
      cssCodeSplit: true,
      // 生成 source map
      sourcemap: false,
      // 资产内联阈值
      assetsInlineLimit: 4096
    },
    resolve: {
      alias: {
        '@': fileURLToPath(new URL('./src', import.meta.url))
      },
    },
  }
})
