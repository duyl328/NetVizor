import {
  createRouter,
  createWebHistory,
  type RouteComponent,
  type RouteRecordRaw,
} from 'vue-router'
import StringUtils from '@/utils/stringUtils'
import PathUtils from '@/utils/pathUtils'

// 导入所有 .vue 文件
const routes = import.meta.glob('@/views/dev/**/*.vue')

// 自动生成路由配置
const devPageViews: RouteRecordRaw[] = Object.keys(routes)
  .map((path) => {
    if ('.vue' === PathUtils.extname(path).trim()) {
      const fileName: string = PathUtils.basename(path).split('.')[0]

      const componentView: RouteComponent = () => routes[path]() // 动态导入组件
      return {
        path: '/dev-index' + `/${StringUtils.toCustomCase(fileName).toLowerCase()}`, // 生成路径
        component: componentView,
        name: fileName,
      }
    }

    return null
  })
  .filter(Boolean) as RouteRecordRaw[] // 过滤掉 null 值

console.log('自动生成路由', devPageViews)

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      // name: '/',
      // component: HomeView,
      // children: children,
      // 重定向到主页
      redirect: '/monitor',
    },
    {
      path: '/home',
      name: 'home',
      component: () => import('../views/HomeView.vue'),
      children: [
        {
          path: '/', // 捕获所有其他路径
          component: () => import('@/views/NotFoundView.vue'), // 任何其他路径都跳转到默认页面
        },
      ],
    },
    // 实时监控
    {
      path: '/monitor',
      name: 'monitor',
      component: () => import('@/views/MonitorView/index.vue'),
      meta: { layout: 'main' },
    },
    // 防火墙
    {
      path: '/firewall',
      name: 'firewall',
      component: () => import('@/views/FirewallView.vue'),
      meta: { layout: 'main' },
    },
    // 网络分析
    {
      path: '/analyse',
      name: 'analyse',
      component: () => import('@/views/AnalyseView/AnalyseView.vue'),
      meta: { layout: 'main' },
    },

    {
      path: '/dev',
      name: 'dev',
      component: () => import('@/views/dev/DevIndex.vue'),
      children: [...devPageViews],
    },
    {
      path: '/about',
      name: 'about',
      component: () => import('@/views/AboutView.vue'),
    },
    {
      path: '/:pathMatch(.*)*', // 通配符路由
      name: 'NotFound',
      component: () => import('@/views/NotFoundView.vue'),
    },
  ],
})

export default router
