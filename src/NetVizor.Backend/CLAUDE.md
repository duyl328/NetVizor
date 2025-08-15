# NetVizor 网络管理系统

这是一个基于C#开发的高级网络监控管理系统，通过ETW（Event Tracing for Windows）技术实现深度网络流量监控，集成Windows防火墙管理功能，提供HTTP和WebSocket接口对外提供数据服务，使用SQLite作为底层数据库存储，前端采用Vue.js并通过WebView2实现桌面应用界面。

## 系统概述

NetVizor是一个功能丰富的网络监控和管理工具，主要用于：
- 实时网络流量监控和分析
- 应用程序网络行为跟踪
- Windows防火墙规则管理
- 网络连接状态监控
- 流量统计和数据可视化

## 技术架构

### 后端架构（C#/.NET）
- **框架版本**: .NET 8.0
- **架构模式**: 分层架构 + 模块化设计
- **核心技术**: ETW、Windows API、COM接口、WebSocket、HTTP服务

### 前端技术栈
- **框架**: Vue.js 3
- **渲染引擎**: Microsoft WebView2
- **UI组件**: 自定义组件库
- **数据可视化**: Chart.js集成

### 数据存储
- **主数据库**: SQLite 3
- **ORM**: Dapper
- **数据结构**: 关系型数据库设计，支持分时聚合

## 项目结构详解

### 核心模块

#### 1. Shell 模块（主程序）
**位置**: `Shell/`
**功能**: 应用程序启动入口和主界面
- **App.xaml.cs**: 应用程序启动逻辑，全局异常处理，服务初始化
- **Views/**: WPF视图文件（主窗口、设置窗口、分析窗口等）
- **UserControls/**: 自定义用户控件
- **Services/**: 业务服务层
- **Utils/**: 界面相关工具类
- **wwwroot/**: Vue前端静态资源文件

#### 2. Data 模块（数据访问层）
**位置**: `Data/`
**功能**: 数据库操作和数据模型定义
- **NetVizorDbContext.cs**: 数据库上下文，表结构定义
- **Models/**: 数据模型类（AppInfo、AppNetwork、GlobalNetwork、AppSetting）
- **Repositories/**: 数据访问仓储类
- **Services/**: 数据服务层
- **Core/**: 核心数据管理类

**数据表结构**:
- `AppInfo`: 应用程序信息表
- `AppNetwork`: 应用网络记录表
- `GlobalNetwork`: 全局网络流量表
- `AppSetting`: 应用设置表
- 聚合表: 按小时/天/周/月的流量统计表

#### 3. Utils.ETW 模块（网络监控核心）
**位置**: `Utils.ETW/`
**功能**: 基于ETW的网络事件捕获和处理
- **Core/ETWNetworkCapture.cs**: ETW网络事件捕获器
- **Etw/EnhancedEtwNetworkManager.cs**: 增强型网络管理器
- **Models/**: 网络事件数据模型
- **Services/**: 网络监控服务

**监控能力**:
- TCP连接监控（建立、断开、发送、接收）
- UDP包监控
- DNS查询监控
- HTTP请求监控
- 进程网络行为关联

#### 4. Utils.Firewall 模块（防火墙管理）
**位置**: `Utils.Firewall/`
**功能**: Windows防火墙规则管理
- **WindowsFirewallApi.cs**: Windows防火墙COM API封装
- **FirewallService.cs**: 防火墙服务接口实现
- **FirewallModel.cs**: 防火墙规则模型定义

**管理功能**:
- 防火墙规则CRUD操作
- 防火墙状态管理
- 规则导入导出
- 批量操作支持
- 规则统计分析

#### 5. Common.Net 模块（网络通信）
**位置**: `Common.Net/`
**功能**: HTTP和WebSocket服务器实现
- **HttpConn/EmbeddedWebServer.cs**: 嵌入式HTTP服务器
- **WebSocketConn/WebSocketManager.cs**: WebSocket管理器
- **Models/**: 通信数据模型

**服务特性**:
- RESTful API接口
- WebSocket实时数据推送
- CORS跨域支持
- 请求日志记录
- 并发处理优化

#### 6. Infrastructure 模块（基础设施）
**位置**: `Infrastructure/`
**功能**: 系统级工具和缓存管理
- **Utils/**: 系统工具类（防火墙、图标、网络等）
- **Models/**: 基础设施模型
- **GlobalCaches/**: 全局缓存管理

#### 7. Common 模块（公共工具）
**位置**: `Common/`
**功能**: 通用工具和基础设施
- **Logger/**: 日志系统（基于Serilog）
- **Configuration/**: 配置管理
- **Utils/**: 网络工具、JSON工具等
- **ExpandFun/**: 扩展方法

### 工具类架构

#### 网络监控工具
- **NetworkHelper.cs**: 网络数据格式化和验证
- **NetworkInfoUtil.cs**: 网络接口监控
- **BasicNetworkMonitor.cs**: 基础网络监控
- **EnhancedNetworkMonitor.cs**: 增强网络监控

#### 系统工具
- **SysHelper.cs**: 系统辅助功能
- **SysInfoUtils.cs**: 系统信息获取
- **IconHelper.cs**: 图标提取工具

#### 数据处理工具
- **JsonHelper.cs**: JSON序列化工具
- **StringExpand.cs**: 字符串扩展方法
- **DatabaseManager.cs**: 数据库管理器

#### UI辅助工具
- **TrayIconManager.cs**: 系统托盘管理
- **TaskbarHelper.cs**: 任务栏工具
- **WebView2Helper.cs**: WebView2检测工具

## API接口文档

### HTTP API接口

#### 应用订阅接口
- `POST /api/subscribe-application`: 订阅应用程序网络信息
- `POST /api/subscribe-process`: 订阅进程网络信息  
- `POST /api/subscribe-appinfo`: 订阅应用详细信息
- `POST /api/unsubscribe`: 取消订阅

#### 防火墙管理接口
- `GET /api/firewall/rules`: 查询防火墙规则（支持分页和筛选）
- `POST /api/firewall/rules`: 创建防火墙规则
- `PUT /api/firewall/rules`: 更新防火墙规则
- `DELETE /api/firewall/rules`: 删除防火墙规则
- `GET /api/firewall/status`: 获取防火墙状态
- `GET /api/firewall/statistics`: 获取防火墙统计信息
- `POST /api/firewall/switch`: 防火墙开关控制

#### 网络统计接口
- `GET /api/statistics/interfaces`: 获取网络接口列表
- `GET /api/statistics/available-ranges`: 获取可用时间范围
- `POST /api/statistics/clear-cache`: 清理缓存
- `GET /api/traffic/trends`: 获取流量趋势数据
- `GET /api/traffic/top-apps`: 获取Top应用流量数据

#### 实时数据接口
- `GET /api/network/global/realtime`: 获取全局网络实时数据
- `GET /api/realtime/active-apps`: 获取实时活跃应用
- `GET /api/system/info`: 获取系统信息
- `GET /api/system/collection-stats`: 获取数据收集统计

#### 应用分析接口
- `GET /api/apps/top-traffic`: 获取软件流量排行榜
- `GET /api/apps/network-analysis`: 获取应用详细网络分析

### WebSocket接口
- **连接地址**: `ws://127.0.0.1:{动态端口}`
- **认证方式**: URL参数uuid
- **数据格式**: JSON格式，支持GZIP压缩
- **心跳机制**: ping/pong消息

## 配置文件说明

### appsettings.json
```json
{
  "AppSettings": {
    "AppName": "NetVizor",
    "MaxConnection": 444,
    "Version": "1.0.0",
    "Logging": {
      "EnableConsole": true,
      "EnableFile": true,
      "LogPath": "logs",
      "MaxFileSizeMB": 1,
      "RetentionDays": 30,
      "CompressDaysThreshold": 3,
      "MinimumLevel": "Information"
    }
  }
}
```

### global.json
```json
{
  "sdk": {
    "version": "8.0.0",
    "rollForward": "latestMajor",
    "allowPrerelease": true
  }
}
```

## 部署要求

### 系统要求
- **操作系统**: Windows 10/11 (x64)
- **运行时**: .NET 8.0 Runtime
- **WebView2**: Microsoft Edge WebView2 Runtime
- **权限**: 管理员权限（ETW监控需要）

### 依赖组件
- **Microsoft.Diagnostics.Tracing**: ETW事件处理
- **NetFwTypeLib**: Windows防火墙COM接口
- **Serilog**: 日志记录
- **Dapper**: ORM框架
- **Fleck**: WebSocket服务器

## 开发环境配置

我所有的开发环境并非全部在默认位置安装，比如部分环境如 Git 之类的在 D:\Env 下

### 开发工具
- Visual Studio 2022 或 JetBrains Rider
- .NET 8.0 SDK
- Node.js（前端开发）
- SQLite管理工具

### 构建命令
```bash
# 还原包
dotnet restore

# 构建项目
dotnet build

# 发布应用
dotnet publish -c Release --self-contained true -r win-x64
```

## 任务管理

你可以使用TODO.md文档来管理你的任务，或者记录任何开发相关的内容。该文件内容完全由你管理，我不会干预，如果有内容你可以直接清空，它就像你的开发草稿纸。

## 注意事项

1. **管理员权限**: ETW网络监控功能需要管理员权限才能正常工作
2. **防火墙权限**: 防火墙规则管理需要管理员权限
3. **WebView2依赖**: 确保系统已安装WebView2 Runtime
4. **数据库权限**: SQLite数据库文件需要读写权限
5. **端口冲突**: 系统会自动选择可用端口，避免冲突








    