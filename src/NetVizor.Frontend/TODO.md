# NetVizor Frontend - API 定义文档

## 1. 网络接口API

### 1.1 获取网络接口列表（在全局网速数据表中，把存在的网卡都展示出来，其中需要传递timeType，以表示拿哪里的数据，day指的是1天的数据，你应该去 GlobalNetworkHourly 中寻找，因为其内部最小单位为小时。如果是 week 则应该去 GlobalNetworkDaily 因为这个表的时间为天，mouth 也对应的是GlobalNetworkDaily，如果是 hour 则应该去 GlobalNetwork 找，他内部的时间是 5 秒。这个值一共4中可能，hour,day,week,mouth，默认 hour 
目前数据库存储的有对应的 网卡ID，需要查询到所有的网卡ID再单独获取对应的网卡信息并返回 Common\Utils\NetworkAdapterHelper.cs 提供了网卡信息的获取方法
```
GET /api/statistics/interfaces?timeType=day
```
**返回:**
```json
{
  "data": [
    {
      "id": "eth0",
      "name": "以太网",
      "displayName": "Realtek PCIe GbE Family Controller",
      "isActive": true,
      "macAddress": "00:11:22:33:44:55"
    },
    {
      "id": "wifi0", 
      "name": "WiFi",
      "displayName": "Intel Wi-Fi 6 AX200",
      "isActive": false,
      "macAddress": "66:77:88:99:AA:BB"
    }
  ]
}
```

### 1.2 获取可用时间范围（这里的逻辑是是否有对应的数据，因为我们的数据库会定时的收集和整理信息，如果用户刚安装软件，肯定不会有月数据，也不会有周数据，所以需要据此来确定用户能展示的数据范围，同样分为 hour，day,week,mouth 返回这四个等级就行，hour只要用户安装超过10秒就有数据了，所以最低返回 hour 就行
```
GET /api/statistics/available-ranges
```
**返回:**
```json
{
  "data": {
    "type": "hour",
    "name": "1小时",
    "available": true,
    "startTime": "2025-01-08T15:00:00Z"
  }
}
```

## 2. 流量数据API

### 2.1 获取流量趋势数据
```
GET /api/traffic/trends
```
**参数:**
- `timeRange`: 时间范围 (1hour|1day|7days|30days)
- `interfaceId`: 网卡ID，可选，默认为"all"表示所有网卡

**返回:**
```json
{
  "data": {
    "interface": "all",
    "timeRange": "1hour",
    "points": [
      {
        "timestamp": "1754565364",
        "uploadSpeed": 1024000,
        "downloadSpeed": 5120000
      },{
        "timestamp": "1754565364",
        "uploadSpeed": 1024000,
        "downloadSpeed": 5120000
      },{
        "timestamp": "1754565364",
        "uploadSpeed": 1024000,
        "downloadSpeed": 5120000
      },{
        "timestamp": "1754565364",
        "uploadSpeed": 1024000,
        "downloadSpeed": 5120000
      }
    ]
  }
}
```

### 2.2 获取Top应用流量数据 
```
GET /api/traffic/top-apps
```
**参数:**
- `timeRange`: 时间范围
- `limit`: 返回数量，默认10

**返回:**
```json
{
  "data": [
    {
      "processName": "chrome.exe",
      "displayName": "Google Chrome",
      "icon": "+Uj0Iu09gH8tBUABUAB2HeA3IEEdYdHLY0EAAAAASUVORK5CYII=",
      "totalBytes": 1073741824
    },{
      "processName": "chrome.exe",
      "displayName": "Google Chrome",
      "icon": "+Uj0Iu09gH8tBUABUAB2HeA3IEEdYdHLY0EAAAAASUVORK5CYII=",
      "totalBytes": 1073741824
    },{
      "processName": "chrome.exe",
      "displayName": "Google Chrome",
      "icon": "+Uj0Iu09gH8tBUABUAB2HeA3IEEdYdHLY0EAAAAASUVORK5CYII=",
      "totalBytes": 1073741824
    }
  ]
}
```

## 3. 软件排行榜API

### 3.1 获取软件流量TOP100排行
```
GET /api/apps/top-traffic
```
**参数:**
- `timeRange`: 时间范围 (1hour|1day|7days|30days)
- `page`: 页码，默认1
- `pageSize`: 每页数量，默认100

**返回:**
```json
{
  "data": {
    "total": 156,
    "page": 1,
    "pageSize": 100,
    "items": [
      {
        "rank": 1,
        "processName": "chrome.exe", 
        "displayName": "Google Chrome",
        "processPath": "C:\\Program Files\\Google\\Chrome\\chrome.exe",
        "icon": "H8tBUABUAB2HeA3IEEdYdHLY0EAAAAASUVORK5CYII",
        "totalBytes": 1073741824,
        "uploadBytes": 104857600,
        "connectionCount": 23
      },{
        "rank": 2,
        "processName": "chrome.exe1", 
        "displayName": "Google Chrome1",
        "processPath": "C:\\Program Files\\Google\\Chrome\\chrome.exe",
        "icon": "H8tBUABUAB2HeA3IEEdYdHLY0EAAAAASUVORK5CYII",
        "totalBytes": 1073741824,
        "uploadBytes": 104857600,
        "connectionCount": 32
      }
    ]
  }
}
```

## 4. 软件详情弹窗API（整合所有数据）

### 4.1 获取软件完整详情信息（新接口）
```
GET /api/apps/{processName}/complete-details
```
**参数:**
- `timeRange`: 时间范围

**返回（包含弹窗所需的所有数据）:**
```json
{
  "data": {
    "basicInfo": {
      "processName": "chrome.exe",
      "displayName": "Google Chrome",
      "version": "119.0.6045.160",
      "company": "Google LLC",
      "processPath": "C:\\Program Files\\Google\\Chrome\\chrome.exe",
      "icon": "/path/to/icon.ico",
      "fileSize": 2467840,
      "createdTime": "2023-11-15T10:30:00Z",
      "modifiedTime": "2023-11-15T10:30:00Z"
    },
    "trafficStats": {
      "totalBytes": 1073741824,
      "uploadBytes": 104857600,
      "downloadBytes": 968884224,
      "percentage": 45.2,
      "connectionCount": 23,
      "averageSpeed": 298666
    },
    "protocolDistribution": [
      {
        "protocol": "HTTPS",
        "bytes": 1610612736,
        "percentage": 37.5,
        "color": "#3b82f6"
      },
      {
        "protocol": "HTTP", 
        "bytes": 1073741824,
        "percentage": 25.0,
        "color": "#10b981"
      },
      {
        "protocol": "DNS",
        "bytes": 751619276,
        "percentage": 17.5,
        "color": "#f59e0b"
      },
      {
        "protocol": "其他",
        "bytes": 858993459,
        "percentage": 20.0,
        "color": "#ef4444"
      }
    ],
    "networkRelations": {
      "nodes": [
        {
          "id": "app_chrome",
          "name": "Chrome",
          "type": "application",
          "size": 45.2,
          "category": 0
        },
        {
          "id": "port_443",
          "name": "443/TCP", 
          "type": "port",
          "size": 30.5,
          "category": 1
        },
        {
          "id": "host_google",
          "name": "google.com",
          "type": "remote_host", 
          "size": 25.8,
          "category": 2
        }
      ],
      "links": [
        {
          "source": "app_chrome",
          "target": "port_443",
          "value": 30.5,
          "label": "30.5MB"
        },
        {
          "source": "port_443", 
          "target": "host_google",
          "value": 25.8,
          "label": "25.8MB"
        }
      ]
    },
    "portStats": [
      {
        "port": 443,
        "protocol": "TCP", 
        "connectionCount": 15,
        "totalBytes": 52428800,
        "remoteHosts": [
          "142.251.42.227",
          "172.217.164.46"
        ]
      },
      {
        "port": 80,
        "protocol": "TCP", 
        "connectionCount": 5,
        "totalBytes": 10485760,
        "remoteHosts": [
          "93.184.216.34"
        ]
      }
    ],
    "connections": [
      {
        "id": "conn_001",
        "localAddress": "192.168.1.100", 
        "localPort": 52341,
        "remoteAddress": "142.251.42.227",
        "remotePort": 443,
        "protocol": "TCP",
        "state": "ESTABLISHED",
        "uploadBytes": 2048000,
        "downloadBytes": 8192000,
        "duration": 1800,
        "lastActivity": "2025-01-08T15:45:00Z"
      }
    ]
  }
}
```

## 5. 仪表盘指标API

### 5.1 获取关键指标数据
```
GET /api/dashboard/metrics
```
**参数:**
- `timeRange`: 时间范围

**返回:**
```json
{
  "data": {
    "totalTraffic": {
      "value": "2.45 TB",
      "trend": 12.5,
      "trendDirection": "up"
    },
    "threatDetection": {
      "value": 156,
      "trend": -8.2,
      "trendDirection": "down"
    },
    "averageLatency": {
      "value": "12.3 ms",
      "trend": 0.0,
      "trendDirection": "stable"
    },
    "packetLoss": {
      "value": "0.02%",
      "trend": 0.01,
      "trendDirection": "up"
    }
  }
}
```

## 说明

### 重要更新：
1. **整合弹窗API**: 新增 `/api/apps/{processName}/complete-details` 接口，一次性返回弹窗所需的所有数据（基本信息、协议分布、网络关系、端口统计等）
2. **简化交互**: 主页面只显示软件列表，详细信息通过弹窗展示
3. **数据优化**: 减少页面初始加载的数据量，按需加载详细信息

### Mock数据说明：
所有API接口在开发阶段使用Mock数据进行测试，生产环境对接真实的RESTful API。成功响应状态码200，失败时返回对应错误码和错误信息。

错误响应格式：
```json
{
  "error": {
    "code": "ERROR_CODE",
    "message": "错误信息",
    "details": {}
  }
}
```
