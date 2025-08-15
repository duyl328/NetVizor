# NetVizor - 网络监控管理系统

这是一个基于Vue 3 + TypeScript的网络管理监控项目，通过WebView2套壳实现桌面应用，集成防火墙API，提供实时网络监控、流量分析和防火墙管理功能。

## 技术栈
- **前端框架**: Vue 3 + TypeScript + Vite
- **UI组件库**: Naive UI
- **状态管理**: Pinia
- **路由管理**: Vue Router 4
- **图表组件**: ECharts + vue-echarts
- **虚拟列表**: vue-virtual-scroller (主要用于大数据列表渲染)
- **HTTP客户端**: Axios
- **动画库**: GSAP
- **工具库**: Lodash

## 项目环境配置
- 开发环境配置: `.env.development`
- API接口地址: `http://localhost:8268/api`
- 开发服务器端口: 3000
- 环境工具路径: `D:\Env` (Git等工具非默认位置安装)

## 项目结构

### 核心模块

#### 1. 实时监控模块 (/monitor)
- **主要功能**: 实时网络连接监控、流量监控、进程监控
- **核心组件**:
  - `MonitorSidebar.vue`: 监控侧边栏，显示过滤器和统计信息
  - `MonitorMainPanel.vue`: 主监控面板，显示连接列表和实时数据
  - `MonitorInspector.vue`: 连接详情检查器
  - `UnifiedConnectionsList.vue`: 统一连接列表组件(虚拟滚动)
  - `TrafficChart.vue`: 流量图表组件
  - `ConnectionsTable.vue`: 连接表格组件
- **布局特点**: 三栏可拖拽调整布局，支持响应式设计

#### 2. 防火墙管理模块 (/firewall)
- **主要功能**: 防火墙规则配置、状态管理、规则统计
- **核心组件**:
  - `FirewallView.vue`: 主防火墙管理界面
  - `FirewallRuleForm.vue`: 规则表单组件
  - `RuleItem.vue`: 单个规则项组件
- **功能特点**: 支持防火墙开关控制、规则增删改查、规则统计展示

#### 3. 网络分析模块 (/analyse)
- **主要功能**: 历史流量分析、应用使用统计、网络关系分析
- **核心组件**:
  - `AnalyseView.vue`: 分析主界面
  - `TrafficTrendChart.vue`: 流量趋势图表
  - `TopAppsChart.vue`: 应用排行图表
  - `NetworkAnalysisChart.vue`: 网络分析图表
  - `SoftwareRankingList.vue`: 软件排行列表
  - `TimelineView.vue`: 时间线视图
- **功能特点**: 多时间维度分析、应用程序流量统计、协议分析

### 工具类和服务层

#### 状态管理 (Stores)
- `websocketStore.ts`: WebSocket连接管理
- `processInfo.ts`: 进程信息状态管理
- `trafficStore.ts`: 流量数据状态管理
- `filterStore.ts`: 过滤器状态管理
- `theme.ts`: 主题管理(支持暗色模式)
- `layoutStore.ts`: 布局状态管理
- `application.ts`: 应用全局状态

#### 工具类 (Utils)
- `websocket.ts` / `websocketHelpers.ts`: WebSocket通信工具
- `http.ts`: HTTP请求封装
- `colorUtils.ts`: 颜色处理工具
- `arrayUtils.ts`: 数组操作工具
- `stringUtils.ts`: 字符串处理工具
- `pathUtils.ts`: 路径处理工具
- `darkUtil.ts`: 暗色主题工具
- `imageCacheManager.ts`: 图片缓存管理
- `logHelper/`: 日志处理工具

#### 通信桥接 (Correspond)
- `CSharpBridgeV2.ts`: C# WebView2通信桥接
- `bridge.ts`: 通信桥接基础实现

#### 类型定义 (Types)
- `websocket.ts`: WebSocket类型定义
- `process.d.ts`: 进程相关类型
- `firewall.ts`: 防火墙类型定义
- `http.ts`: HTTP请求类型
- `infoModel.ts`: 信息模型类型

### 页面路由结构
```
/ -> 重定向到 /monitor
/monitor -> 实时监控 (默认页面)
/firewall -> 防火墙管理
/analyse -> 网络分析
/dev -> 开发调试页面 (开发模式可见)
/about -> 关于页面
/setting -> 设置页面
```

### 开发调试功能
- 开发模式下显示路由导航栏(可拖拽)
- 自动生成开发页面路由
- WebSocket测试工具
- 图表动画测试页面

## 核心特性

### 1. 实时通信
- WebSocket实时数据推送
- C# WebView2双向通信
- 插件化WebSocket管理

### 2. 虚拟化渲染
- 使用vue-virtual-scroller处理大量数据列表
- 优化内存使用和渲染性能

### 3. 响应式设计
- 可拖拽调整的布局
- 自适应不同屏幕尺寸
- 主题切换支持

### 4. 性能优化
- 代码分割和懒加载
- 图片缓存管理
- 资源压缩和优化

## 构建命令
- `npm run dev`: 开发模式
- `npm run build`: 生产构建
- `npm run type-check`: 类型检查
- `npm run lint`: 代码检查
- `npm run format`: 代码格式化

## 开发注意事项
- 主要使用虚拟列表组件处理大数据量
- WebView2环境下运行，注意桥接通信
- 支持开发时显示调试导航(IS_SHOW_GENERATE_ROUTER控制)
- 使用TODO.md作为开发任务管理工具








