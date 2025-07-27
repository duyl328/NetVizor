/**
 * Time:2025/3/19 21:50 33
 * Name:enums.ts
 * Path:src/constants
 * ProjectName:argus-src
 * Author:charlatans
 *
 *  Il n'ya qu'un héroïsme au monde :
 *     c'est de voir le monde tel qu'il est et de l'aimer.
 */
/**
 * 文件大小单位
 */
export enum FILE_SIZE_UNIT_ENUM {
  B = 'B',
  KB = 'KB',
  MB = 'MB',
  GB = 'GB',
  TB = 'TB'
}

/**
 * 连接状态枚举
 */
export enum ConnectionState {
  /** 已建立连接 */
  ESTABLISHED = 'ESTABLISHED',
  /** 正在监听 */
  LISTEN = 'LISTEN',
  /** 等待关闭 */
  TIME_WAIT = 'TIME_WAIT',
  /** 等待远程关闭 */
  CLOSE_WAIT = 'CLOSE_WAIT',
  /** 正在关闭 */
  CLOSING = 'CLOSING',
  /** 已关闭 */
  CLOSED = 'CLOSED',
  /** 正在尝试连接 */
  SYN_SENT = 'SYN_SENT',
  /** 连接请求已收到 */
  SYN_RECEIVED = 'SYN_RECEIVED',
  /** 最后的ACK等待 */
  LAST_ACK = 'LAST_ACK',
  /** 连接中 */
  Connecting = 'Connecting',
  /** 当前连接活跃（发送/接收中） */
  Connected = 'Connected',
  /** 已断开或未活跃 */
  Disconnected = 'Disconnected',
  /** 监听状态（如本地服务器） */
  Listening = 'Listening',
  /** 无法判断状态 */
  Unknown = 'Unknown',
}

/**
 * 协议类型枚举
 */
export enum ProtocolType {
  TCP = 'TCP',
  UDP = 'UDP',
  /** 可选 */
  ICMP = 'ICMP',
  Unknown = 'Unknown',
}

/**
 * 进出站类型枚举
 */
export enum TrafficDirection {
  /** 入站 */
  Inbound = 'Inbound',
  /** 出站 */
  Outbound = 'Outbound',
  // Unknown
}

