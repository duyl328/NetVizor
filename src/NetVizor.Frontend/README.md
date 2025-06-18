# net-src

This template should help get you started developing with Vue 3 in Vite.

## Recommended IDE Setup

[VSCode](https://code.visualstudio.com/) + [Volar](https://marketplace.visualstudio.com/items?itemName=Vue.volar) (and disable Vetur).

## Type Support for `.vue` Imports in TS

TypeScript cannot handle type information for `.vue` imports by default, so we replace the `tsc` CLI with `vue-tsc` for type checking. In editors, we need [Volar](https://marketplace.visualstudio.com/items?itemName=Vue.volar) to make the TypeScript language service aware of `.vue` types.

## Customize configuration

See [Vite Configuration Reference](https://vite.dev/config/).

## Project Setup

```sh
npm install
```

### Compile and Hot-Reload for Development

```sh
npm run dev
```

### Type-Check, Compile and Minify for Production

```sh
npm run build
```

### Run Unit Tests with [Vitest](https://vitest.dev/)

```sh
npm run test:unit
```

### Lint with [ESLint](https://eslint.org/)

```sh
npm run lint
```


1. 流量图表：定时查询指定网络的信息并记录，根据线程为单位将内容记录下来
2. 进程网络限制：上传/下载拦截（未设置、询问、拦截、允许）
3. 限额：上传限额、下载限额
4. 获取网络列表（本地连接；以太网等 连接状态信息、IP信息）
5. 应用列表（列出所有活动或安装的软件，包括：软件状态、内存占用、CPU占用等信息）
6. TCP 和 UDP 的连接历史,对于目标端口第一次和最后一次连接时间
![img.png](img.png)
7. 连接日志
![img_1.png](img_1.png)
8. 流量统计。按照年、月、周、日、12小时、6小时、3小时、1小时、30分钟、5分钟(应用、主机、流量类型、国家等维度筛选显示) 进行分段；用户可选查看时间（日期和小时的选择可以滑动选择）；按照应用程序，端口、目标地址等内容信息进行展示和分类
![img_2.png](img_2.png)
![img_3.png](img_3.png)
9. 对于不同的 网络环境、GPS 地址、设备状态（电量、设备模式） 自动切换联网控制规则（公司的网络切换某些软件联网；个人网络则打开某些软件联网）
10. 配置文件可导入导出
11. 网络连接地图：不同软件联网的IP是那个区域和国家
12. 联网规则可设置有效时间（5分钟、10分钟、20分钟、30分钟、45分钟、1小时、3小时、6小时、12小时、1天、1周、自定义时间）、关机后重置、关闭软件后重置、永久有效
13. 弹出连接请求提示，可选择允许/拒绝，并可记忆成规则



所连接服务器的地理位置
