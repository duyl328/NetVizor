/**
 * 模拟数据生成服务
 * 为演示模式提供逼真的网络监控数据
 */

import type { ProcessType, ConnectionInfo, IPEndPoint } from '@/types/process'
import type { FirewallRule, FirewallStatus, FirewallStatistics, DisplayRule } from '@/types/firewall'
import type { WebSocketResponse } from '@/types/websocket'
import { ConnectionState, ProtocolType, TrafficDirection } from '@/constants/enums'
import { FirewallProfile, RuleDirection, RuleAction } from '@/types/firewall'

class MockDataService {
  private static instance: MockDataService | null = null
  private processId = 1000
  private connectionId = 10000
  private ruleId = 1

  static getInstance(): MockDataService {
    if (!MockDataService.instance) {
      MockDataService.instance = new MockDataService()
    }
    return MockDataService.instance
  }

  // ================= 随机数据生成工具 =================

  private randomInt(min: number, max: number): number {
    return Math.floor(Math.random() * (max - min + 1)) + min
  }

  private randomFloat(min: number, max: number): number {
    return Math.random() * (max - min) + min
  }

  private randomChoice<T>(array: T[]): T {
    return array[Math.floor(Math.random() * array.length)]
  }

  private randomBoolean(probability = 0.5): boolean {
    return Math.random() < probability
  }

  private generateIPAddress(): string {
    // 生成常见的内网和外网IP
    const types = [
      () => `192.168.${this.randomInt(1, 255)}.${this.randomInt(1, 254)}`, // 内网
      () => `10.${this.randomInt(0, 255)}.${this.randomInt(1, 255)}.${this.randomInt(1, 254)}`, // 内网
      () => `172.${this.randomInt(16, 31)}.${this.randomInt(1, 255)}.${this.randomInt(1, 254)}`, // 内网
      () => `${this.randomInt(1, 223)}.${this.randomInt(0, 255)}.${this.randomInt(0, 255)}.${this.randomInt(1, 254)}`, // 公网
    ]
    return this.randomChoice(types)()
  }

  private generateProcessName(): string {
    const processes = [
      'chrome.exe', 'firefox.exe', 'msedge.exe', 'notepad.exe', 'explorer.exe',
      'code.exe', 'WeChat.exe', 'QQ.exe', 'Spotify.exe', 'Discord.exe',
      'steam.exe', 'obs64.exe', 'Photoshop.exe', 'winrar.exe', 'TeamViewer.exe',
      'node.exe', 'python.exe', 'java.exe', 'nginx.exe', 'httpd.exe',
      'svchost.exe', 'csrss.exe', 'dwm.exe', 'conhost.exe'
    ]
    return this.randomChoice(processes)
  }

  // 根据应用名称生成相关的进程名
  getRelatedProcessName(appName: string): string {
    const appProcessMap: { [key: string]: string[] } = {
      'chrome': ['chrome.exe', 'chrome_crashpad_handler.exe'],
      'google chrome': ['chrome.exe', 'chrome_crashpad_handler.exe'],
      'firefox': ['firefox.exe', 'firefox_helper.exe'],
      'mozilla firefox': ['firefox.exe', 'firefox_helper.exe'],
      'edge': ['msedge.exe', 'msedge_proxy.exe'],
      'microsoft edge': ['msedge.exe', 'msedge_proxy.exe'],
      'code': ['code.exe', 'code_helper.exe'],
      'visual studio code': ['code.exe', 'code_helper.exe'],
      'wechat': ['WeChat.exe', 'WeChatAppEx.exe'],
      'qq': ['QQ.exe', 'QQExternal.exe'],
      'discord': ['Discord.exe', 'discord_voice.exe'],
      'steam': ['steam.exe', 'steamwebhelper.exe'],
      'spotify': ['Spotify.exe', 'SpotifyWebHelper.exe']
    }

    const normalizedAppName = appName.toLowerCase().replace(/\s+/g, ' ')
    const relatedProcesses = appProcessMap[normalizedAppName] || [this.generateProcessName()]
    
    return this.randomChoice(relatedProcesses)
  }

  private generateDomainName(): string {
    const domains = [
      'www.google.com', 'www.baidu.com', 'api.github.com', 'cdn.jsdelivr.net',
      'www.microsoft.com', 'discord.com', 'open.spotify.com', 'www.youtube.com',
      'api.twitter.com', 'www.facebook.com', 'cdn.cloudflare.com', 'amazonaws.com',
      'azure.microsoft.com', 'googleapis.com', 'www.apple.com', 'developer.mozilla.org'
    ]
    return this.randomChoice(domains)
  }

  // ================= 网络连接数据生成 =================

  generateIPEndPoint(isLocal = false): IPEndPoint {
    return {
      address: isLocal ? '127.0.0.1' : this.generateIPAddress(),
      port: isLocal ? this.randomInt(1024, 65535) : this.randomInt(80, 65535),
      addressFamily: 2 // IPv4
    }
  }

  generateConnectionInfo(processId: number): ConnectionInfo {
    const protocol = this.randomChoice([ProtocolType.TCP, ProtocolType.UDP])
    const state = protocol === ProtocolType.TCP
      ? this.randomChoice([ConnectionState.ESTABLISHED, ConnectionState.LISTEN, ConnectionState.TIME_WAIT, ConnectionState.CLOSE_WAIT])
      : ConnectionState.Connected

    const startTime = new Date(Date.now() - this.randomInt(0, 3600000)) // 过去1小时内
    const lastActiveTime = new Date(Date.now() - this.randomInt(0, 300000)) // 过去5分钟内

    return {
      connectionKey: `${processId}_${this.connectionId++}`,
      processId,
      protocol,
      localEndpoint: this.generateIPEndPoint(true),
      remoteEndpoint: this.generateIPEndPoint(false),
      state,
      direction: this.randomChoice([TrafficDirection.Inbound, TrafficDirection.Outbound]),
      startTime,
      lastActiveTime,
      bytesSent: this.randomInt(1024, 1024 * 1024 * 100), // 1KB - 100MB
      bytesReceived: this.randomInt(1024, 1024 * 1024 * 100),
      currentSendSpeed: this.randomInt(0, 1024 * 100), // 0 - 100KB/s
      currentReceiveSpeed: this.randomInt(0, 1024 * 100),
      isActive: this.randomBoolean(0.7)
    }
  }

  generateProcessInfo(): ProcessType {
    const processName = this.generateProcessName()
    const startTime = new Date(Date.now() - this.randomInt(0, 86400000)) // 过去24小时内
    const connectionCount = this.randomInt(2, 15) // 确保每个进程至少有2个连接
    const connections: ConnectionInfo[] = []

    const pid = this.processId++

    // 生成连接
    for (let i = 0; i < connectionCount; i++) {
      connections.push(this.generateConnectionInfo(pid))
    }

    const totalUploaded = this.randomInt(1024, 1024 * 1024 * 1024) // 1KB - 1GB
    const totalDownloaded = this.randomInt(1024, 1024 * 1024 * 1024)

    return {
      processName,
      processId: pid,
      startTime,
      hasExited: this.randomBoolean(0.1),
      useMemory: this.randomInt(1024 * 1024, 1024 * 1024 * 512), // 1MB - 512MB
      threadCount: this.randomInt(1, 50),
      mainModulePath: `C:\\Program Files\\${processName.replace('.exe', '')}\\${processName}`,
      mainModuleName: processName,
      totalUploaded,
      totalDownloaded,
      uploadSpeed: this.randomInt(0, 1024 * 100), // 0 - 100KB/s
      downloadSpeed: this.randomInt(0, 1024 * 100),
      connections
    }
  }

  // ================= 防火墙数据生成 =================

  generateFirewallRule(): FirewallRule {
    const appNames = [
      'Google Chrome', 'Mozilla Firefox', 'Microsoft Edge', 'Visual Studio Code',
      'Steam', 'Discord', 'Spotify', 'WeChat', 'QQ', 'TeamViewer'
    ]

    const directions = [RuleDirection.Inbound, RuleDirection.Outbound]
    const protocols = [ProtocolType.TCP, ProtocolType.UDP, ProtocolType.Any]
    const actions = [RuleAction.Allow, RuleAction.Block]

    const app = this.randomChoice(appNames)
    const direction = this.randomChoice(directions)
    const protocol = this.randomChoice(protocols)
    const action = this.randomChoice(actions)
    const enabled = this.randomBoolean(0.8)

    const ruleName = `${app} - ${direction === RuleDirection.Inbound ? '入站' : '出站'} - ${action === RuleAction.Allow ? '允许' : '阻止'}`

    return {
      name: ruleName,
      description: `${app}的${direction === RuleDirection.Inbound ? '入站' : '出站'}连接规则`,
      applicationName: `C:\\Program Files\\${app}\\${app.replace(' ', '')}.exe`,
      serviceName: '',
      protocol,
      localPorts: protocol === ProtocolType.TCP ? this.randomChoice(['80', '443', '8080', '3000', 'Any']) : 'Any',
      remotePorts: 'Any',
      localAddresses: 'Any',
      remoteAddresses: 'Any',
      icmpTypesAndCodes: '',
      direction,
      enabled,
      profiles: FirewallProfile.All,
      edgeTraversal: false,
      action,
      grouping: `${app} 应用程序`,
      interfaceTypes: 'All',
      interfaces: [],
      creationDate: new Date(Date.now() - this.randomInt(0, 30 * 24 * 3600000)).toISOString(),
      modificationDate: new Date(Date.now() - this.randomInt(0, 7 * 24 * 3600000)).toISOString(),
      edgeTraversalAllowed: false,
      looseSourceMapping: false,
      localOnlyMapping: false,
      remoteMachineAuthorizationList: '',
      remoteUserAuthorizationList: '',
      embeddedContext: '',
      flags: 0,
      secureFlags: false
    }
  }

  generateFirewallStatus(): FirewallStatus {
    const totalRules = this.randomInt(50, 200)
    const enabledRules = Math.floor(totalRules * this.randomFloat(0.6, 0.9))

    return {
      isEnabled: true,
      profileStatuses: {
        [FirewallProfile.Domain]: {
          profile: FirewallProfile.Domain,
          isEnabled: true,
          blockAllInboundTraffic: false,
          notifyOnListen: true,
          unicastResponsesDisabled: false,
          defaultInboundAction: RuleAction.Block,
          defaultOutboundAction: RuleAction.Allow
        },
        [FirewallProfile.Private]: {
          profile: FirewallProfile.Private,
          isEnabled: true,
          blockAllInboundTraffic: false,
          notifyOnListen: true,
          unicastResponsesDisabled: false,
          defaultInboundAction: RuleAction.Block,
          defaultOutboundAction: RuleAction.Allow
        },
        [FirewallProfile.Public]: {
          profile: FirewallProfile.Public,
          isEnabled: true,
          blockAllInboundTraffic: true,
          notifyOnListen: true,
          unicastResponsesDisabled: false,
          defaultInboundAction: RuleAction.Block,
          defaultOutboundAction: RuleAction.Allow
        },
        [FirewallProfile.All]: {
          profile: FirewallProfile.All,
          isEnabled: true,
          blockAllInboundTraffic: false,
          notifyOnListen: true,
          unicastResponsesDisabled: false,
          defaultInboundAction: RuleAction.Block,
          defaultOutboundAction: RuleAction.Allow
        }
      },
      totalRules,
      enabledRules,
      inboundRules: Math.floor(totalRules * 0.4),
      outboundRules: Math.floor(totalRules * 0.6),
      lastModified: new Date().toISOString()
    }
  }

  // ================= 批量数据生成 =================

  generateProcessList(count = 20): ProcessType[] {
    const processes: ProcessType[] = []
    for (let i = 0; i < count; i++) {
      processes.push(this.generateProcessInfo())
    }
    return processes
  }

  generateFirewallRules(count = 30): FirewallRule[] {
    const rules: FirewallRule[] = []
    for (let i = 0; i < count; i++) {
      rules.push(this.generateFirewallRule())
    }
    return rules
  }

  // ================= 实时数据更新模拟 =================

  generateRealtimeUpdate(): WebSocketResponse {
    const updateTypes = [
      'process_update',
      'connection_update',
      'traffic_update',
      'firewall_event'
    ]

    const type = this.randomChoice(updateTypes)
    const timestamp = new Date().toISOString()

    switch (type) {
      case 'process_update':
        return {
          type,
          data: this.generateProcessInfo(),
          timestamp
        }

      case 'connection_update':
        return {
          type,
          data: this.generateConnectionInfo(this.randomInt(1000, 2000)),
          timestamp
        }

      case 'traffic_update':
        return {
          type,
          data: {
            totalBytesIn: this.randomInt(1024 * 1024, 1024 * 1024 * 1024),
            totalBytesOut: this.randomInt(1024 * 1024, 1024 * 1024 * 1024),
            currentSpeedIn: this.randomInt(0, 1024 * 1024), // 0 - 1MB/s
            currentSpeedOut: this.randomInt(0, 1024 * 1024),
            timestamp: Date.now()
          },
          timestamp
        }

      case 'firewall_event':
        return {
          type,
          data: {
            action: this.randomChoice(['blocked', 'allowed']),
            protocol: this.randomChoice(['TCP', 'UDP']),
            sourceIP: this.generateIPAddress(),
            destPort: this.randomInt(80, 65535),
            process: this.generateProcessName(),
            timestamp: Date.now()
          },
          timestamp
        }

      default:
        return {
          type: 'unknown',
          timestamp
        }
    }
  }

  // ================= 统计数据生成 =================

  generateTrafficStats(hours = 24) {
    const stats = []
    const now = Date.now()

    for (let i = hours; i >= 0; i--) {
      const timestamp = now - (i * 3600000) // 每小时

      // 模拟一天中的流量变化模式（工作时间流量更高）
      const hour = new Date(timestamp).getHours()
      let trafficMultiplier = 1

      if (hour >= 9 && hour <= 18) {
        trafficMultiplier = this.randomFloat(1.5, 2.5) // 工作时间流量更高
      } else if (hour >= 19 && hour <= 23) {
        trafficMultiplier = this.randomFloat(1.2, 1.8) // 晚上娱乐时间
      } else {
        trafficMultiplier = this.randomFloat(0.3, 0.8) // 深夜和凌晨较低
      }

      const baseBytesIn = this.randomInt(1024 * 1024 * 10, 1024 * 1024 * 50)
      const baseBytesOut = this.randomInt(1024 * 1024 * 5, 1024 * 1024 * 25)

      stats.push({
        timestamp,
        bytesIn: Math.floor(baseBytesIn * trafficMultiplier),
        bytesOut: Math.floor(baseBytesOut * trafficMultiplier),
        packetsIn: this.randomInt(1000 * trafficMultiplier, 10000 * trafficMultiplier),
        packetsOut: this.randomInt(500 * trafficMultiplier, 5000 * trafficMultiplier),
        connections: this.randomInt(10, Math.floor(100 * trafficMultiplier)),
        hour
      })
    }

    return stats
  }

  generateTopApplications(count = 10) {
    const apps = [
      { name: 'Google Chrome', bytes: this.randomInt(1024 * 1024 * 100, 1024 * 1024 * 1024) },
      { name: 'Steam', bytes: this.randomInt(1024 * 1024 * 50, 1024 * 1024 * 500) },
      { name: 'Discord', bytes: this.randomInt(1024 * 1024 * 10, 1024 * 1024 * 100) },
      { name: 'Visual Studio Code', bytes: this.randomInt(1024 * 1024 * 5, 1024 * 1024 * 50) },
      { name: 'WeChat', bytes: this.randomInt(1024 * 1024 * 5, 1024 * 1024 * 30) },
      { name: 'Spotify', bytes: this.randomInt(1024 * 1024 * 10, 1024 * 1024 * 80) },
      { name: 'Microsoft Edge', bytes: this.randomInt(1024 * 1024 * 20, 1024 * 1024 * 200) },
      { name: 'QQ', bytes: this.randomInt(1024 * 1024 * 3, 1024 * 1024 * 20) },
      { name: 'TeamViewer', bytes: this.randomInt(1024 * 1024 * 1, 1024 * 1024 * 10) },
      { name: 'Node.js', bytes: this.randomInt(1024 * 1024 * 2, 1024 * 1024 * 15) }
    ]

    return apps
      .sort((a, b) => b.bytes - a.bytes)
      .slice(0, count)
  }

  // ================= 专门为网络分析生成的数据 =================

  /**
   * 生成网络协议分布数据
   */
  generateProtocolDistribution() {
    const total = this.randomInt(1000, 5000)
    const tcpPercent = this.randomFloat(0.6, 0.8)
    const udpPercent = this.randomFloat(0.15, 0.3)
    const otherPercent = 1 - tcpPercent - udpPercent

    return {
      TCP: Math.floor(total * tcpPercent),
      UDP: Math.floor(total * udpPercent),
      ICMP: Math.floor(total * otherPercent * 0.8),
      Other: Math.floor(total * otherPercent * 0.2)
    }
  }

  /**
   * 生成端口使用统计
   */
  generatePortStats() {
    const commonPorts = [
      { port: 80, name: 'HTTP', connections: this.randomInt(50, 200) },
      { port: 443, name: 'HTTPS', connections: this.randomInt(100, 300) },
      { port: 53, name: 'DNS', connections: this.randomInt(20, 80) },
      { port: 22, name: 'SSH', connections: this.randomInt(1, 10) },
      { port: 21, name: 'FTP', connections: this.randomInt(0, 5) },
      { port: 25, name: 'SMTP', connections: this.randomInt(5, 20) },
      { port: 993, name: 'IMAPS', connections: this.randomInt(3, 15) },
      { port: 3389, name: 'RDP', connections: this.randomInt(0, 3) },
      { port: 8080, name: 'HTTP-Alt', connections: this.randomInt(10, 50) },
      { port: 3001, name: 'Dev-Server', connections: this.randomInt(1, 10) }
    ]

    return commonPorts.sort((a, b) => b.connections - a.connections)
  }

  /**
   * 生成地理位置分布数据
   */
  generateGeoDistribution() {
    const locations = [
      { country: '中国', city: '北京', connections: this.randomInt(100, 300), flag: '🇨🇳' },
      { country: '美国', city: '洛杉矶', connections: this.randomInt(50, 150), flag: '🇺🇸' },
      { country: '日本', city: '东京', connections: this.randomInt(20, 80), flag: '🇯🇵' },
      { country: '德国', city: '法兰克福', connections: this.randomInt(15, 60), flag: '🇩🇪' },
      { country: '新加坡', city: '新加坡', connections: this.randomInt(10, 40), flag: '🇸🇬' },
      { country: '英国', city: '伦敦', connections: this.randomInt(8, 35), flag: '🇬🇧' },
      { country: '法国', city: '巴黎', connections: this.randomInt(5, 25), flag: '🇫🇷' },
      { country: '韩国', city: '首尔', connections: this.randomInt(12, 45), flag: '🇰🇷' }
    ]

    return locations.sort((a, b) => b.connections - a.connections)
  }

  /**
   * 生成网络威胁事件
   */
  generateSecurityEvents(count = 10) {
    const threatTypes = [
      '端口扫描', '暴力破解', '恶意软件通信', 'DDoS攻击',
      '异常流量', '可疑连接', '数据泄露尝试', '未授权访问'
    ]

    const severityLevels = ['低', '中', '高', '严重']
    const events = []

    for (let i = 0; i < count; i++) {
      const timestamp = Date.now() - this.randomInt(0, 86400000) // 过去24小时
      events.push({
        id: `event_${timestamp}_${i}`,
        type: this.randomChoice(threatTypes),
        severity: this.randomChoice(severityLevels),
        sourceIP: this.generateIPAddress(),
        targetPort: this.randomInt(1, 65535),
        timestamp,
        blocked: this.randomBoolean(0.7),
        description: `检测到${this.randomChoice(threatTypes)}行为`
      })
    }

    return events.sort((a, b) => b.timestamp - a.timestamp)
  }

  /**
   * 生成网络性能指标
   */
  generateNetworkMetrics() {
    return {
      latency: this.randomInt(10, 100), // ms
      packetLoss: this.randomFloat(0, 2), // %
      jitter: this.randomInt(1, 20), // ms
      throughput: this.randomInt(50, 1000), // Mbps
      bandwidth: this.randomInt(100, 1000), // Mbps
      uptime: this.randomFloat(99.5, 99.99), // %
      errorRate: this.randomFloat(0, 0.5) // %
    }
  }

  /**
   * 生成实时网络流量图表数据
   */
  generateRealtimeTrafficChart(points = 60) {
    const data = []
    const now = Date.now()

    for (let i = points; i >= 0; i--) {
      const timestamp = now - (i * 1000) // 每秒一个点
      const hour = new Date(timestamp).getHours()

      // 根据时间调整基础流量
      let baseTraffic = 1
      if (hour >= 9 && hour <= 18) baseTraffic = 2
      else if (hour >= 19 && hour <= 23) baseTraffic = 1.5
      else baseTraffic = 0.5

      // 添加一些随机波动
      const variation = this.randomFloat(0.7, 1.3)

      data.push({
        timestamp,
        uploadSpeed: Math.floor(this.randomInt(1024 * 10, 1024 * 100) * baseTraffic * variation), // KB/s
        downloadSpeed: Math.floor(this.randomInt(1024 * 50, 1024 * 500) * baseTraffic * variation), // KB/s
        totalConnections: this.randomInt(10, Math.floor(50 * baseTraffic)),
        activeConnections: this.randomInt(5, Math.floor(25 * baseTraffic))
      })
    }

    return data
  }

  /**
   * 生成应用程序列表
   */
  generateApplicationList(count = 15) {
    const applications = []

    const sampleApps = [
      {
        name: 'Google Chrome',
        process: 'chrome.exe',
        company: 'Google LLC',
        version: '120.0.6099.109',
        description: 'Google Chrome'
      },
      {
        name: 'Visual Studio Code',
        process: 'Code.exe',
        company: 'Microsoft Corporation',
        version: '1.85.1',
        description: 'Visual Studio Code'
      },
      {
        name: 'Steam',
        process: 'steam.exe',
        company: 'Valve Corporation',
        version: '3.4.15.7',
        description: 'Steam'
      },
      {
        name: 'Discord',
        process: 'Discord.exe',
        company: 'Discord Inc.',
        version: '1.0.9016',
        description: 'Discord'
      },
      {
        name: 'Spotify',
        process: 'Spotify.exe',
        company: 'Spotify AB',
        version: '1.2.25.1011',
        description: 'Spotify'
      },
      {
        name: 'Microsoft Edge',
        process: 'msedge.exe',
        company: 'Microsoft Corporation',
        version: '120.0.2210.89',
        description: 'Microsoft Edge'
      },
      {
        name: 'Windows Explorer',
        process: 'explorer.exe',
        company: 'Microsoft Corporation',
        version: '10.0.19041.3636',
        description: 'Windows Explorer'
      },
      {
        name: 'Notepad++',
        process: 'notepad++.exe',
        company: 'Notepad++ Team',
        version: '8.5.8',
        description: 'Notepad++'
      }
    ]

    for (let i = 0; i < Math.min(count, sampleApps.length); i++) {
      const app = sampleApps[i]
      const processCount = Math.floor(Math.random() * 5) + 1
      const processIds = Array.from({length: processCount}, () => Math.floor(Math.random() * 9999) + 1000)

      applications.push({
        id: `app_${i + 1}_${Date.now()}`,
        productName: app.name,
        processName: app.process,
        processIds: processIds,
        startTime: new Date(Date.now() - Math.random() * 86400000).toISOString(),
        hasExited: false,
        exitTime: undefined,
        exitCode: undefined,
        useMemory: Math.floor(Math.random() * 500000000) + 50000000, // 50MB - 550MB
        threadCount: Math.floor(Math.random() * 50) + 5,
        mainModulePath: `C:\\Program Files\\${app.name}\\${app.process}`,
        mainModuleName: app.process,
        fileDescription: app.description,
        companyName: app.company,
        version: app.version,
        legalCopyright: `© ${app.company}`,
        iconBase64: '', // 空字符串，会使用fallback图标
        name: app.name // 添加name属性用于显示
      })
    }

    return applications
  }

  // ================= 组合数据生成（为各个模块提供完整数据集） =================

  /**
   * 生成监控模块完整数据
   */
  generateMonitorModuleData() {
    return {
      processes: this.generateProcessList(25),
      connections: [], // 会从processes中提取
      realtimeChart: this.generateRealtimeTrafficChart(),
      networkMetrics: this.generateNetworkMetrics(),
      protocolStats: this.generateProtocolDistribution()
    }
  }

  /**
   * 生成防火墙模块完整数据
   */
  generateFirewallModuleData() {
    return {
      status: this.generateFirewallStatus(),
      rules: this.generateFirewallRules(40),
      securityEvents: this.generateSecurityEvents(15),
      portStats: this.generatePortStats()
    }
  }

  /**
   * 生成分析模块完整数据
   */
  generateAnalysisModuleData() {
    return {
      trafficStats: this.generateTrafficStats(48), // 48小时数据
      topApplications: this.generateTopApplications(15),
      protocolDistribution: this.generateProtocolDistribution(),
      geoDistribution: this.generateGeoDistribution(),
      portStats: this.generatePortStats(),
      securityEvents: this.generateSecurityEvents(20),
      networkMetrics: this.generateNetworkMetrics()
    }
  }
}

// 导出单例实例
export const mockDataService = MockDataService.getInstance()
export default mockDataService
