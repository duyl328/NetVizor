/**
 * Time:2025/6/15 20:23 36
 * Name:http.ts
 * Path:src/plugins
 * ProjectName:argus-front
 * Author:charlatans
 *
 *  Il n'ya qu'un héroïsme au monde :
 *     c'est de voir le monde tel qu'il est et de l'aimer.
 */
import { type App } from 'vue';
import { httpConfig } from '@/config/httpConfig.ts'
import { httpClient } from '@/utils/http.ts'

export default {
  install(app: App) {
    // 更新HTTP配置
    const config = httpConfig.getConfig();
    httpClient.updateConfig(config);

    // 注册全局属性
    app.config.globalProperties.$http = httpClient;
    app.config.globalProperties.$httpConfig = httpConfig;

    // 提供注入
    app.provide('$http', httpClient);
    app.provide('$httpConfig', httpConfig);
  }
};
