/**
 * 软件信息类型
 */
type ApplicationType = {
  productName: string
  processIds: number[]
  startTime: string
  hasExited: false
  exitTime?: number
  exitCode?: number
  useMemory: number
  threadCount: number
  mainModulePath: string
  mainModuleName: string
  fileDescription: string
  processName: string
  companyName: string
  version: string
  legalCopyright: string
  iconBase64: string
  id: string
}

export { ApplicationType }
