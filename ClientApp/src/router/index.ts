import {
  createRouter,
  createWebHistory,
  type RouteComponent,
  type RouteRecordRaw,
} from 'vue-router'
import HomeView from '../views/HomeView.vue'
import DevIndex from '@/views/dev/DevIndex.vue'
import StringUtils from '@/utils/stringUtils'
import PathUtils from '@/utils/pathUtils'
import NotFound from '@/views/NotFoundView.vue'
import AboutView from '@/views/AboutView.vue'

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
      redirect: '/home',
    },
    {
      path: '/home',
      name: 'home',
      component: HomeView,
      children: [
        {
          path: '/', // 捕获所有其他路径
          component: NotFound, // 任何其他路径都跳转到默认页面
        },
      ],
    },

    {
      path: '/dev',
      name: 'dev',
      component: DevIndex,
      children: [...devPageViews],
    },
    {
      path: '/about',
      name: 'about',
      component: AboutView,
    },
    {
      path: '/:pathMatch(.*)*', // 通配符路由
      name: 'NotFound',
      component: NotFound,
    },
  ],
})

export default router
