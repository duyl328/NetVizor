/**
 * 软件信息类型
 */
type ApplicationType = {
  ProcessName: string
  ProcessIds: number[]
  StartTime: string
  HasExited: false
  ExitTime?: number
  ExitCode?: number
  UseMemory: number
  ThreadCount: number
  MainModulePath: string
  MainModuleName: string
  FileDescription: string
  ProductName: string
  CompanyName: string
  Version: string
  LegalCopyright: string
  IconBase64: string
  Id:string
}

export {ApplicationType}
