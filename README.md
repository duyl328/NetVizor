# NetVizor

<div align="center">
  <img src="logo.png" alt="NetVizor Logo" width="128" height="128">

[![License](https://img.shields.io/github/license/duyl328/NetVizor)](LICENSE)
[![Stars](https://img.shields.io/github/stars/duyl328/NetVizor)](https://github.com/duyl328/NetVizor/stargazers)
[![Issues](https://img.shields.io/github/issues/duyl328/NetVizor)](https://github.com/duyl328/NetVizor/issues)
[![Release](https://img.shields.io/github/v/release/duyl328/NetVizor)](https://github.com/duyl328/NetVizor/releases)

<h3>A Modern Windows Network Monitoring and Firewall Management Tool</h3>
<h3>现代化的 Windows 网络监控与防火墙管理工具</h3>

[English](#english) | [中文](#chinese)
</div>

目前项目正在积极开发阶段, 在 `1.0` 版本前不保证项目可用性.

The project is currently in active development, and there is no guarantee of project availability until the '1.0' release.

---

<a name="english"></a>
## English

### 🚀 Overview

NetVizor is a modern, lightweight Windows network monitoring and firewall management tool that combines the simplicity of TinyWall with the visualization capabilities of GlassWire. Built with performance and user experience in mind, it provides real-time network insights and intuitive firewall control.

### ✨ Features

#### Core Monitoring
- **Real-time Process Monitoring** - Track network connections for each process
- **Live Traffic Statistics** - Monitor upload/download speeds per process
- **Connection Details** - View protocols, ports, and connection states
- **DNS Resolution** - Map IP addresses to domain names
- **Historical Data** - Track network usage over time

#### Firewall Management
- **Easy Rule Creation** - Quick wizards for creating firewall rules
- **One-click Block/Allow** - Instantly control application access
- **Rule Templates** - Pre-configured rules for common applications
- **Windows Firewall Integration** - Built on Windows Firewall APIs

#### Visualization
- **Real-time Charts** - Beautiful traffic graphs powered by ECharts
- **Process Traffic Distribution** - Visual breakdown of bandwidth usage
- **Connection Maps** - Geographic visualization of connections
- **Modern UI** - Clean, responsive interface built with Vue 3

#### Smart Features
- **Learning Mode** - Automatically suggest rules based on behavior
- **Anomaly Detection** - Alert on suspicious network activity
- **Gaming Mode** - Temporary rule relaxation for gaming
- **Bandwidth Predictions** - Forecast future usage trends

### 🛠️ Technology Stack

- **Backend**: C# (.NET 6+)
    - ETW (Event Tracing for Windows) for network monitoring
    - Windows Firewall with Advanced Security APIs
    - High-performance data processing

- **Frontend**: Vue 3 + TypeScript
    - WebView2 for native integration
    - ECharts for data visualization
    - Tailwind CSS for modern styling

- **Communication**: WebSocket for real-time updates

### 📋 Requirements

- Windows 10 version 1809 or later
- .NET 6.0 Runtime
- WebView2 Runtime (auto-installed if missing)
- Administrator privileges (for firewall management)

[//]: # (### 🚀 Quick Start)

[//]: # ()
[//]: # (1. Download the latest release from [Releases]&#40;https://github.com/duyl328/NetVizor/releases&#41;)

[//]: # (2. Run the installer as Administrator)

[//]: # (3. Launch NetVizor from the Start Menu)

[//]: # (4. Follow the initial setup wizard)

[//]: # ()
[//]: # (### 🔧 Building from Source)

[//]: # ()
[//]: # (```bash)

[//]: # (# Clone the repository)

[//]: # (git clone https://github.com/duyl328/NetVizor.git)

[//]: # (cd NetVizor)

[//]: # ()
[//]: # (# Build backend)

[//]: # (cd src/NetVizor.Backend)

[//]: # (dotnet build)

[//]: # ()
[//]: # (# Build frontend)

[//]: # (cd ../NetVizor.Frontend)

[//]: # (npm install)

[//]: # (npm run build)

[//]: # ()
[//]: # (# Run the application)

[//]: # (cd ../NetVizor.Backend)

[//]: # (dotnet run)

[//]: # (```)

### 🤝 Contributing

We welcome contributions! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for details.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

### 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

### 🙏 Acknowledgments

- Inspired by TinyWall and GlassWire
- Built with Windows ETW and Firewall APIs
- UI powered by Vue.js and ECharts

---

<a name="chinese"></a>
## 中文

### 🚀 概述

NetVizor 是一个现代化、轻量级的 Windows 网络监控和防火墙管理工具，它结合了 TinyWall 的简洁性和 GlassWire 的可视化能力。以性能和用户体验为核心设计理念，提供实时网络洞察和直观的防火墙控制。

### ✨ 功能特性

#### 核心监控
- **实时进程监控** - 追踪每个进程的网络连接
- **实时流量统计** - 监控每个进程的上传/下载速度
- **连接详情** - 查看协议、端口和连接状态
- **DNS 解析** - 将 IP 地址映射为域名
- **历史数据** - 追踪网络使用历史

#### 防火墙管理
- **便捷规则创建** - 快速向导创建防火墙规则
- **一键阻止/允许** - 即时控制应用程序访问
- **规则模板** - 常见应用程序的预配置规则
- **Windows 防火墙集成** - 基于 Windows 防火墙 API 构建

#### 可视化
- **实时图表** - 由 ECharts 驱动的精美流量图
- **进程流量分布** - 带宽使用的可视化分解
- **连接地图** - 连接的地理位置可视化
- **现代化 UI** - 使用 Vue 3 构建的简洁、响应式界面

#### 智能功能
- **学习模式** - 基于行为自动建议规则
- **异常检测** - 对可疑网络活动发出警报
- **游戏模式** - 游戏时临时放松规则
- **带宽预测** - 预测未来使用趋势

### 🛠️ 技术栈

- **后端**: C# (.NET 6+)
    - ETW (Windows 事件跟踪) 用于网络监控
    - Windows 高级安全防火墙 API
    - 高性能数据处理

- **前端**: Vue 3 + TypeScript
    - WebView2 原生集成
    - ECharts 数据可视化
    - Tailwind CSS 现代化样式

- **通信**: WebSocket 实时更新

### 📋 系统要求

- Windows 10 版本 1809 或更高版本
- .NET 6.0 运行时
- WebView2 运行时（如果缺失会自动安装）
- 管理员权限（用于防火墙管理）

### 🚀 快速开始

[//]: # ()
[//]: # (1. 从 [Releases]&#40;https://github.com/duyl328/NetVizor/releases&#41; 下载最新版本)

[//]: # (2. 以管理员身份运行安装程序)

[//]: # (3. 从开始菜单启动 NetVizor)

[//]: # (4. 按照初始设置向导操作)

[//]: # (### 🔧 从源码构建)

[//]: # ()
[//]: # (```bash)

[//]: # (# 克隆仓库)

[//]: # (git clone https://github.com/duyl328/NetVizor.git)

[//]: # (cd NetVizor)

[//]: # ()
[//]: # (# 构建后端)

[//]: # (cd src/NetVizor.Backend)

[//]: # (dotnet build)

[//]: # ()
[//]: # (# 构建前端)

[//]: # (cd ../NetVizor.Frontend)

[//]: # (npm install)

[//]: # (npm run build)

[//]: # ()
[//]: # (# 运行应用程序)

[//]: # (cd ../NetVizor.Backend)

[//]: # (dotnet run)

[//]: # (```)

### 🤝 贡献

我们欢迎贡献！请查看 [CONTRIBUTING.md](CONTRIBUTING.md) 了解详情。

1. Fork 本仓库
2. 创建您的功能分支 (`git checkout -b feature/AmazingFeature`)
3. 提交您的更改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 打开一个 Pull Request

### 📝 许可证

本项目采用 GPL3.0 许可证 - 查看 [LICENSE](LICENSE) 文件了解详情。

### 🙏 致谢

- 灵感来自 TinyWall 和 GlassWire
- 基于 Windows ETW 和防火墙 API 构建
- UI 由 Vue.js 和 ECharts 驱动

### 🌟 Star History

[![Star History Chart](https://api.star-history.com/svg?repos=duyl328/NetVizor&type=Date)](https://star-history.com/#duyl328/NetVizor&Date)

### 📞 联系我们

- Issues: [GitHub Issues](https://github.com/duyl328/NetVizor/issues)
- Discussions: [GitHub Discussions](https://github.com/duyl328/NetVizor/discussions)
- Email: yylyou333@gmail.com

### 🗺️ 路线图

- [ ] 基础网络监控功能
- [ ] Windows 防火墙集成
- [ ] 实时流量可视化
- [ ] 学习模式和智能规则建议
- [ ] 多语言支持
- [ ] 插件系统
- [ ] 云同步功能
- [ ] 移动端配套应用

### 💖 支持项目

如果您觉得这个项目有帮助，请考虑：
- ⭐ 给项目加星
- 🐛 报告问题
- 💡 建议新功能
- 🤝 贡献代码
