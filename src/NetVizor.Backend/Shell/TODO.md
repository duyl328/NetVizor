# NetVizor Frontend - API 定义文档

## 1. 网络接口API

### 1.1 获取网络接口列表（在全局网速数据表中，把存在的网卡都展示出来，其中需要传递timeRange，以表示拿哪里的数据，day指的是1天的数据，你应该去 GlobalNetworkHourly 中寻找，因为其内部最小单位为小时。如果是 week 则应该去 GlobalNetworkDaily 因为这个表的时间为天，mouth 也对应的是GlobalNetworkDaily，如果是 hour 则应该去 GlobalNetwork 找，他内部的时间是 5 秒。这个值一共4中可能，hour,day,week,mouth，默认 hour
目前数据库存储的有对应的 网卡ID，需要查询到所有的网卡ID再单独获取对应的网卡信息并返回 Common\Utils\NetworkAdapterHelper.cs 提供了网卡信息的获取方法
```
GET /api/statistics/interfaces?timeRange=day
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
