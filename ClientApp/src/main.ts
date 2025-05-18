import './assets/main.css'
import 'virtual:uno.css'
import { createApp } from 'vue'
import { createPinia } from 'pinia'

import App from './App.vue'
import router from './router'


const app = createApp(App)

import ElementPlus from 'element-plus'

import { presetMini } from 'unocss'

app.use(createPinia())
app.use(router)
app.use(ElementPlus)

app.mount('#app')

// 注册 C# 函数
window.externalFunctions = {};
