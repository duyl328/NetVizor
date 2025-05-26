# UniversalLogger - 通用日志记录库

一个基于Serilog构建的功能完整的C#日志记录库，支持多种输出方式、自动维护、配置灵活等特性。

## 主要功能

✅ **多种输出方式**: 支持控制台、文件输出，可单独或同时使用  
✅ **按日期分割**: 自动按日期创建日志文件  
✅ **文件大小控制**: 超过指定大小自动创建新文件  
✅ **自动压缩**: 指定天数后自动压缩旧日志文件  
✅ **自动清理**: 超过保留期限自动删除旧日志  
✅ **配置灵活**: 支持代码配置、JSON文件配置  
✅ **功能完整**: 支持各级别日志、异常堆栈、上下文信息  
✅ **性能优化**: 基于Serilog高性能日志框架  
✅ **易于使用**: 提供静态类和实例两种使用方式

## 安装依赖

在你的项目中添加以下NuGet包：

```xml
<PackageReference Include="Serilog" Version="3.1.1" />
<PackageReference Include="Serilog.Sinks.Console" Version="5.0.1" />
<PackageReference Include="Serilog.Sinks.File" Version="5.0.0" />
<PackageReference Include="Serilog.Formatting.Json" Version="1.0.0" />
<PackageReference Include="Microsoft.Extensions.Configuration" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="8.0.0" />
```

## 快速开始

### 1. 使用默认配置

```csharp
using UniversalLogger;

// 使用默认配置创建日志记录器
using var logger = new UniversalLogger(new LoggerConfig());

logger.LogInformation("Hello, World!");
logger.LogWarning("这是一个警告");
logger.LogError("这是一个错误");
```

### 2. 使用静态日志记录器

```csharp
// 初始化静态日志记录器
Log.Initialize();

// 直接使用
Log.Information("应用程序启动");
Log.Warning("警告信息");
Log.Error(exception, "发生错误: {Message}", exception.Message);
```

### 3. 从配置文件加载

创建 `appsettings.json` 文件：

```json
{
  "Logging": {
    "EnableConsole": true,
    "EnableFile": true,
    "LogPath": "logs",
    "MaxFileSizeMB": 1,
    "RetentionDays": 30,
    "CompressDaysThreshold": 3,
    "MinimumLevel": "Information",
    "OutputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
    "UseJsonFormat": false,
    "CompressCheckIntervalHours": 24
  }
}
```

使用配置文件：

```csharp
// 从配置文件初始化
Log.InitializeFromJsonFile("appsettings.json");

Log.Information("使用配置文件的日志记录器");
```

## 配置选项

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `EnableConsole` | bool | true | 是否启用控制台输出 |
| `EnableFile` | bool | true | 是否启用文件输出 |
| `LogPath` | string | "logs" | 日志文件存储路径 |
| `MaxFileSizeMB` | int | 1 | 单个日志文件最大大小(MB) |
| `RetentionDays` | int | 30 | 日志保留天
