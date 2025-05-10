import { globalIgnores } from 'eslint/config'
import { defineConfigWithVueTs, vueTsConfigs } from '@vue/eslint-config-typescript'
import pluginVue from 'eslint-plugin-vue'
import pluginVitest from '@vitest/eslint-plugin'
import skipFormatting from '@vue/eslint-config-prettier/skip-formatting'
import tsParser from '@typescript-eslint/parser'
import vuePlugin from 'eslint-plugin-vue'
import tsPlugin from '@typescript-eslint/eslint-plugin'
import prettier from 'eslint-config-prettier'

export default defineConfigWithVueTs(
  {
    name: 'app/files-to-lint',
    files: ['**/*.{ts,mts,tsx,vue}'],
  },

  globalIgnores(['**/dist/**', '**/dist-ssr/**', '**/coverage/**']),

  pluginVue.configs['flat/essential'],
  vueTsConfigs.recommended,

  // 重要：确保自定义规则配置优先级更高
  {
    rules: {
      // 完全禁用未使用变量的检查 - 更强力的方式
      '@typescript-eslint/no-unused-vars': 'off',
      'no-unused-vars': 'off',

      // 或者使用你之前的配置（二选一）
      /*
      '@typescript-eslint/no-unused-vars': ['warn', {
        'varsIgnorePattern': '^_',
        'argsIgnorePattern': '^_',
        // 添加更多宽松配置
        'ignoreRestSiblings': true,
        'caughtErrors': 'none'
      }],
      */
    },
  },

  {
    ...pluginVitest.configs.recommended,
    files: ['src/**/__tests__/*'],
    languageOptions: {
      parser: tsParser,
      ecmaVersion: 'latest',
      sourceType: 'module',
      globals: {
        window: 'readonly',
        document: 'readonly',
      },
    },
    plugins: {
      vue: vuePlugin,
      '@typescript-eslint': tsPlugin,
    },
    rules: {
      // ❗ 严重错误（保留）
      'no-undef': 'error',
      'vue/no-unused-components': 'warn',
      '@typescript-eslint/no-explicit-any': 'warn',

      // ✅ 风格类（宽松）
      'vue/multi-word-component-names': 'off',
      'vue/max-attributes-per-line': 'off',
      '@typescript-eslint/no-non-null-assertion': 'off',
      '@typescript-eslint/explicit-module-boundary-types': 'off',

      // ✅ 开发方便
      'no-console': 'off',
      'no-debugger': 'warn',
    },
  },
  {
    ...prettier,
  },
  skipFormatting,
)
