import { fileURLToPath, URL } from 'node:url';
import { defineConfig, loadEnv } from 'vite';
import vue from '@vitejs/plugin-vue';
import AutoImport from 'unplugin-auto-import/vite';
import { NaiveUiResolver } from 'unplugin-vue-components/resolvers';
import Components from 'unplugin-vue-components/vite';
export default defineConfig(({ mode }) => {
    const env = loadEnv(mode, process.cwd());
    
    // 直接根据 mode 设置基础路径，确保在 GitHub Actions 中正确工作
    const baseUrl = mode === 'demo' ? '/NetVizor/' : '/';
    
    console.log('Build mode:', mode);
    console.log('Base URL:', baseUrl);
    
    return {
        // GitHub Pages 部署路径配置
        base: baseUrl,
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
                ]
            }),
            Components({
                resolvers: [NaiveUiResolver()]
            })
        ],
        // 防止 Vite 掩盖 Rust 错误
        clearScreen: false,
        server: {
            port: 3000,
            strictPort: true,
            host: "0.0.0.0",
            watch: {},
        },
        build: {},
        resolve: {
            alias: {
                '@': fileURLToPath(new URL('./src', import.meta.url))
            },
        },
    };
});
