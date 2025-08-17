# NetVizor 演示模式部署指南

这个文档介绍如何部署 NetVizor 的演示版本到 GitHub Pages。

## 快速开始

### 本地演示模式开发

```bash
# 启动演示模式开发服务器
npm run dev:demo

# 构建演示版本
npm run build:demo

# 预览演示版本
npm run preview:demo
```

### GitHub Pages 自动部署

1. **推送代码到 GitHub**
   ```bash
   git add .
   git commit -m "Add demo mode support"
   git push origin main
   ```

2. **配置 GitHub Pages**
   - 前往项目的 GitHub 页面
   - 进入 Settings > Pages
   - Source 选择 "GitHub Actions"
   - 保存设置

3. **自动部署**
   - 推送到 `main` 分支会自动触发部署
   - 也可以在 Actions 页面手动触发部署

## 环境配置

### 演示模式环境变量

演示模式使用 `.env.demo` 配置文件：

```env
# 启用演示模式
VITE_DEMO_MODE=true

# 演示模式下不需要API地址
VITE_APP_API_URL=

# 应用标题
VITE_APP_TITLE=NetVizor - 网络监控系统演示

# 演示版本标识
VITE_APP_VERSION=demo

# 基础路径 (GitHub Pages可能需要)
VITE_BASE_URL=/NetVizor/
```

### 开发环境配置

开发模式使用 `.env.development` 配置文件：

```env
VITE_MODE='development'
VITE_APP_API_URL='http://localhost:8268/api'
VITE_DEMO_MODE=false
```

## 演示模式特性

### 自动环境检测

项目会自动检测运行环境：

- **WebView2 环境**: 使用真实数据和桥接通信
- **演示模式** (`VITE_DEMO_MODE=true`): 使用模拟数据
- **开发模式** (`VITE_DEMO_MODE=false`): 尝试连接开发API

### 模拟数据功能

演示模式提供以下模拟数据：

- ✅ 实时网络连接监控数据
- ✅ 防火墙规则和状态
- ✅ 流量统计与分析图表
- ✅ 协议分布可视化
- ✅ 安全事件和威胁检测
- ✅ 应用程序排行和统计
- ✅ 地理位置分布数据

### 用户界面增强

演示模式包含特殊的UI组件：

- **演示横幅**: 告知用户这是演示版本
- **状态指示器**: 显示当前运行在演示模式
- **数据活动指示器**: 模拟实时数据更新

## 构建配置

### Vite 配置

项目使用 Vite 作为构建工具，支持多环境配置：

```typescript
// vite.config.ts
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd())
  
  return {
    // 根据mode加载对应的.env文件
    // demo模式会自动加载.env.demo
    // development模式会加载.env.development
  }
})
```

### 包管理器脚本

```json
{
  "scripts": {
    "dev": "vite",                    // 开发模式
    "dev:demo": "vite --mode demo",   // 演示开发模式
    "build": "vite build",            // 生产构建
    "build:demo": "vite build --mode demo", // 演示构建
    "preview": "vite preview",        // 预览生产版本
    "preview:demo": "vite preview --mode demo" // 预览演示版本
  }
}
```

## 部署目标

### GitHub Pages

自动部署到 GitHub Pages：

- **URL 格式**: `https://username.github.io/repository-name/`
- **自动触发**: 推送到 main 分支
- **手动触发**: GitHub Actions 页面

### 其他平台

演示版本也可以部署到其他静态托管平台：

- **Netlify**: 拖拽 `dist` 文件夹或连接 GitHub
- **Vercel**: 导入 GitHub 项目，设置构建命令为 `npm run build:demo`
- **Surge.sh**: `npm install -g surge && surge dist`

## 测试和验证

### 本地测试

```bash
# 构建演示版本
npm run build:demo

# 启动本地服务器测试
npm run preview:demo

# 或使用其他静态服务器
npx serve dist
```

### 演示模式测试工具

项目包含内置的测试工具，在浏览器控制台运行：

```javascript
// 运行演示模式完整测试
await testDemoMode()
```

### 验证清单

部署前确认以下项目：

- [ ] 演示横幅正常显示
- [ ] 状态指示器显示"演示模式"
- [ ] 所有页面可以正常访问
- [ ] 模拟数据正常加载和更新
- [ ] 图表和可视化组件正常渲染
- [ ] 响应式设计在移动设备上正常

## 故障排除

### 常见问题

1. **页面空白**: 检查控制台错误，通常是路径配置问题
2. **资源404**: 确认 `VITE_BASE_URL` 配置正确
3. **演示模式未激活**: 验证 `VITE_DEMO_MODE=true` 设置

### 调试信息

演示模式会在控制台输出详细的调试信息：

```
[EnvironmentDetector] WebView2 detected: false
[EnvironmentDetector] Demo mode enabled: true
[EnvironmentDetector] Environment type: DEMO
[DataAdapter] 使用模拟进程数据
[WebSocketStore] 演示模式：跳过真实WebSocket连接
```

### 支持联系

如果遇到部署问题，请：

1. 检查 GitHub Actions 日志
2. 查看浏览器控制台错误信息
3. 在项目 Issues 页面报告问题

## 维护说明

### 更新模拟数据

模拟数据生成器位于 `src/utils/mockDataService.ts`，可以：

- 添加新的数据类型
- 优化数据生成逻辑
- 调整数据量和频率

### 更新演示UI

演示模式UI组件位于：

- `src/components/DemoBanner.vue`: 演示横幅
- `src/components/DemoModeIndicator.vue`: 状态指示器

### 环境配置更新

需要更新环境配置时：

1. 修改对应的 `.env.*` 文件
2. 更新 `environmentDetector.ts` 逻辑
3. 测试不同环境下的行为