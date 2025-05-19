namespace WinDivertNet.WinDivertWrapper;

using System;
using System.Runtime.InteropServices;

public static class PacketSniffer
{
    public static void Start1()
    {
        Console.WriteLine($"进入 12313 ");
        var handle = WinDivert.Open("true", WinDivert.WINDIVERT_LAYER_NETWORK, 0, WinDivert.WINDIVERT_FLAG_SNIFF);

        byte[] buffer = new byte[65535];
        WinDivert.WINDIVERT_ADDRESS addr = default;
        uint readLen = 0;

        while (true)
        {
            try
            {
                if (WinDivert.Recv(handle, buffer, (uint)buffer.Length, ref addr, ref readLen))
                {
                    ParsePacketAndLog(buffer, readLen, addr);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"接收数据包时出错: {ex.Message}");
            }
        }

        void ParsePacketAndLog(byte[] buffer, uint length, WinDivert.WINDIVERT_ADDRESS addr)
        {
            Console.WriteLine($"length: {length}");
            if (buffer == null || length == 0)
                return;

            try
            {
                // IPv4 数据包的最小长度为 20 字节
                if (length >= 20)
                {
                    // 检查版本 (IPv4 = 4, 位于第一个字节的高 4 位)
                    int version = (buffer[0] >> 4) & 0xF;

                    if (version == 4) // IPv4
                    {
                        int headerLength = (buffer[0] & 0xF) * 4; // IHL * 4 = 字节数
                        byte protocol = buffer[9]; // 协议字段

                        // 源 IP 和目标 IP
                        uint srcIp = (uint)((buffer[12]) | (buffer[13] << 8) | (buffer[14] << 16) | (buffer[15] << 24));
                        uint dstIp = (uint)((buffer[16]) | (buffer[17] << 8) | (buffer[18] << 16) | (buffer[19] << 24));

                        string srcIpStr = $"{buffer[12]}.{buffer[13]}.{buffer[14]}.{buffer[15]}";
                        string dstIpStr = $"{buffer[16]}.{buffer[17]}.{buffer[18]}.{buffer[19]}";

                        Console.WriteLine($"IPv4: Src={srcIpStr}, Dst={dstIpStr}, Protocol={protocol}");

                        // 如果有足够的数据，解析 TCP 或 UDP
                        if (length >= (uint)(headerLength + 4) && (protocol == 6 || protocol == 17))
                        {
                            // 获取端口 (TCP 和 UDP 的前 4 字节都是源端口和目标端口)
                            int offset = headerLength;
                            ushort srcPort = (ushort)((buffer[offset] << 8) | buffer[offset + 1]);
                            ushort dstPort = (ushort)((buffer[offset + 2] << 8) | buffer[offset + 3]);

                            if (protocol == 6) // TCP
                            {
                                Console.WriteLine($"TCP: SrcPort={srcPort}, DstPort={dstPort}");
                            }
                            else if (protocol == 17) // UDP
                            {
                                Console.WriteLine($"UDP: SrcPort={srcPort}, DstPort={dstPort}");
                            }
                        }
                    }
                    else if (version == 6 && length >= 40) // IPv6 (头部固定 40 字节)
                    {
                        // IPv6 解析代码...
                        // 这里略过，需要时可以添加
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"解析数据包时出错: {ex.Message}");
            }
        }
    }

    public static void Start()
    {
        Console.WriteLine("WinDivert调试工具启动...");

        // 检查是否以管理员身份运行
        bool isAdmin = new System.Security.Principal.WindowsPrincipal(
            System.Security.Principal.WindowsIdentity.GetCurrent()
        ).IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);

        if (!isAdmin)
        {
            Console.WriteLine("错误: 此程序需要管理员权限才能使用WinDivert！");
            Console.WriteLine("请右键点击程序，选择'以管理员身份运行'然后重试。");
            return;
        }

        Console.WriteLine("步骤1: 尝试打开WinDivert...");

        // 最简单的过滤器
        string filter = "true";
        IntPtr handle =
            WinDivert.WinDivertOpen(filter, WinDivert.WINDIVERT_LAYER_NETWORK, 0, WinDivert.WINDIVERT_FLAG_SNIFF);

        // 检查句柄是否有效
        if (handle == IntPtr.Zero)
        {
            int errorCode = Marshal.GetLastWin32Error();
            Console.WriteLine($"错误: 无法打开WinDivert，错误码: {errorCode}");
            PrintWin32Error(errorCode);
            return;
        }

        Console.WriteLine("成功: WinDivert句柄已打开");

        // 尝试接收一个数据包
        Console.WriteLine("\n步骤2: 尝试接收数据包...");

        // 为数据包分配缓冲区
        byte[] buffer = new byte[65535]; // 最大以太网帧大小

        // 设置接收地址结构
        WinDivert.WINDIVERT_ADDRESS addr = new WinDivert.WINDIVERT_ADDRESS
        {
            Reserved = new byte[8] // 确保Reserved被初始化
        };

        // 接收数据包
        uint readLen = 0;

        // 尝试直接使用基本的Recv函数
        Console.WriteLine("尝试WinDivertRecv...");
        bool recvResult = WinDivert.WinDivertRecv(handle, buffer, (uint)buffer.Length, ref addr, ref readLen);

        if (!recvResult)
        {
            int errorCode = Marshal.GetLastWin32Error();
            Console.WriteLine($"错误: WinDivertRecv失败，错误码: {errorCode}");
            PrintWin32Error(errorCode);
        }
        else
        {
            Console.WriteLine($"成功: 接收到数据包，大小: {readLen} 字节");

            // 打印数据包的前16个字节
            string hexData = BitConverter.ToString(buffer, 0, Math.Min((int)readLen, 16));
            Console.WriteLine($"数据包前16字节: {hexData}");
        }

        // 重置并尝试使用Ex版本
        readLen = 0;
        addr = new WinDivert.WINDIVERT_ADDRESS { Reserved = new byte[8] };

        Console.WriteLine("\n尝试WinDivertRecvEx...");
        bool recvExResult =
            WinDivert.WinDivertRecvEx(handle, buffer, (uint)buffer.Length, 0, ref addr, ref readLen, IntPtr.Zero);

        if (!recvExResult)
        {
            int errorCode = Marshal.GetLastWin32Error();
            Console.WriteLine($"错误: WinDivertRecvEx失败，错误码: {errorCode}");
            PrintWin32Error(errorCode);
        }
        else
        {
            Console.WriteLine($"成功: 接收到数据包，大小: {readLen} 字节");

            // 打印数据包的前16个字节
            string hexData = BitConverter.ToString(buffer, 0, Math.Min((int)readLen, 16));
            Console.WriteLine($"数据包前16字节: {hexData}");
        }

        // 最后关闭句柄
        WinDivert.WinDivertClose(handle);
        Console.WriteLine("\nWinDivert句柄已关闭");

        Console.WriteLine("\n调试信息收集完成。请检查以上输出以确定问题所在。");
    }

    // 辅助函数：打印Win32错误信息
    private static void PrintWin32Error(int errorCode)
    {
        switch (errorCode)
        {
            case 5:
                Console.WriteLine("访问被拒绝。请确认程序以管理员权限运行。");
                break;
            case 87:
                Console.WriteLine("参数错误。过滤器语法可能不正确。");
                break;
            case 123:
                Console.WriteLine("文件名、目录名或卷标语法不正确。请检查WinDivert.dll是否位于正确的路径。");
                break;
            case 126:
                Console.WriteLine("找不到指定的模块。无法找到WinDivert.dll文件。");
                break;
            case 193:
                Console.WriteLine("不是有效的Win32应用程序。WinDivert.dll可能是错误的版本(32位/64位)。");
                break;
            case 997:
                Console.WriteLine("正在进行重叠操作。WinDivert可能需要异步调用。");
                break;
            case 995:
                Console.WriteLine("由于线程退出或应用程序请求，已中止I/O操作。");
                break;
            case 998:
                Console.WriteLine("无效访问WinDivert。请检查是否有另一个进程正在使用相同的过滤器。");
                break;
            case 1450:
                Console.WriteLine("系统资源不足，无法完成请求的服务。");
                break;
            default:
                Console.WriteLine($"未知错误。Windows错误码: {errorCode}");
                break;
        }
    }
}
