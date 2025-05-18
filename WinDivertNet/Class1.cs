using System.Text;
using WinDivertNet.WinDivertWrapper;

namespace WinDivertNet;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("WinDivert 捕获启动...");
        PacketSniffer.Start();
    }

    static void Main1(string[] args)
    {
        // 创建 WinDivert 控制器
        using (WinDivertController controller = new WinDivertController())
        {
            // 检查过滤器语法
            var checkResult = controller.CheckFilter("tcp.DstPort == 80");
            if (!checkResult.IsValid)
            {
                Console.WriteLine($"过滤器语法错误: {checkResult.ErrorMessage}");
                return;
            }

            // 打开 WinDivert 句柄
            bool success = controller.Open(
                "tcp.DstPort == 80", // 过滤器
                WinDivert.WINDIVERT_LAYER_NETWORK, // 网络层
                0, // 优先级
                WinDivert.WINDIVERT_FLAG_SNIFF // 嗅探模式
            );

            if (!success)
            {
                Console.WriteLine("打开 WinDivert 失败！");
                return;
            }

            Console.WriteLine("开始监听 HTTP 流量...");
            Console.WriteLine("按 ESC 键退出");

            // 设置队列长度
            controller.SetParam(WinDivert.WINDIVERT_PARAM_QUEUE_LENGTH, 8192);

            byte[] packet = new byte[65535];
            WinDivert.WINDIVERT_ADDRESS addr = new WinDivert.WINDIVERT_ADDRESS();

            // 创建线程处理数据包
            Thread thread = new Thread(() =>
            {
                while (controller.IsValid)
                {
                    uint readLen = controller.Receive(packet, ref addr);
                    if (readLen > 0)
                    {
                        // 解析数据包
                        PacketHeaders headers = controller.ParsePacket(packet);
                        if (headers.Success)
                        {
                            if (headers.HasTcpHeader && headers.HasIpHeader)
                            {
                                string srcIp = WinDivert.FormatIPv4Address(headers.IpHeader.SrcAddr);
                                string dstIp = WinDivert.FormatIPv4Address(headers.IpHeader.DstAddr);
                                ushort srcPort = headers.TcpHeader.SrcPort;
                                ushort dstPort = headers.TcpHeader.DstPort;

                                Console.WriteLine($"捕获到 TCP 数据包: {srcIp}:{srcPort} -> {dstIp}:{dstPort}");

                                // 如果有数据，则尝试输出前 100 个字节
                                if (headers.Data != null && headers.DataLength > 0)
                                {
                                    int len = (int)Math.Min(headers.DataLength, 100);
                                    string data = Encoding.ASCII.GetString(headers.Data, 0, len);
                                    Console.WriteLine($"数据内容: {data}");
                                }
                            }
                        }
                    }
                }
            });

            thread.IsBackground = true;
            thread.Start();

            // 等待 ESC 键退出
            while (Console.ReadKey(true).Key != ConsoleKey.Escape)
            {
            }

            // 关闭 WinDivert
            controller.Close();
            Console.WriteLine("监听结束！");
        }
    }
}
