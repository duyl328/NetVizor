import { ProtocolType, TrafficDirection, ConnectionState } from '@/constants/enums'

/**
 * 进程信息
 */
export type ProcessType = {
  /**
   * 进程名称
   */
  processName: string

  /**
   * 进程ID
   */
  processId: number

  /**
   * 启动时间
   */
  startTime: Date

  /**
   * 是否退出
   */
  hasExited: boolean

  /**
   * 退出时间
   */
  exitTime?: Date

  /**
   * 退出代码
   */
  exitCode?: number

  /**
   * 占用内存
   */
  useMemory: number

  /**
   * 线程数
   */
  threadCount: number

  /**
   * 主模块路径
   */
  mainModulePath?: string

  /**
   * 启动文件名
   */
  mainModuleName?: string

  /**
   * 上行总量
   */
  totalUploaded: number
  /**
   * 下行总量
   */
  totalDownloaded: number
  /**
   * 上行速度
   */
  uploadSpeed: number

  /**
   * 下行速度
   */
  downloadSpeed: number

  /**
   * 所有连接
   */
  connections: ConnectionInfo[]
}

/**
 * IP终结点接口
 */
export interface IPEndPoint {
  /** IP地址 */
  address: string
  /** 端口号 */
  port: number

  addressFamily: number
}

/**
 * 连接详细信息接口
 */
export interface ConnectionInfo {
  /** 连接唯一标识键 */
  connectionKey: string
  /** 进程ID */
  processId: number
  /** 协议类型 */
  protocol: ProtocolType
  /** 本地终结点 */
  localEndpoint: IPEndPoint
  /** 目标终结点 */
  remoteEndpoint: IPEndPoint
  /** 连接状态 */
  state: ConnectionState
  /** 流量方向 */
  direction: TrafficDirection
  /** 连接开始时间 */
  startTime: Date
  /** 最后活跃时间 */
  lastActiveTime: Date
  /** 已发送字节数 */
  bytesSent: number
  /** 已接收字节数 */
  bytesReceived: number
  /** 当前发送速度（字节/秒） */
  currentSendSpeed: number
  /** 当前接收速度（字节/秒） */
  currentReceiveSpeed: number
  /** 是否活跃 */
  isActive: boolean
}
