using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;

namespace Utils.ETW;

public static class Class1
{
    public static void Main()
    {
        Console.WriteLine($"这是ue安徽委会aweifawejiofawoi");

        // if (TraceEventSession.IsElevated() == true)
        // {
        //     Console.WriteLine("请以管理员身份运行此程序！");
        //     return;
        // }

        using (var session = new TraceEventSession("MyNetSession"))
        {
            // 启用网络内核事件（包括 TCP/UDP）
            session.EnableKernelProvider(KernelTraceEventParser.Keywords.NetworkTCPIP);

            session.Source.Kernel.TcpIpRecv += data =>
            {
                Console.WriteLine(
                    $"[RECV] PID: {data.ProcessID}  {data.saddr}:{data.sport} → {data.daddr}:{data.dport}  {data.size} bytes");
            };

            session.Source.Kernel.TcpIpSend += data =>
            {
                Console.WriteLine(
                    $"[SEND] PID: {data.ProcessID}  {data.saddr}:{data.sport} → {data.daddr}:{data.dport}  {data.size} bytes");
            };

            Console.WriteLine("监听中... 按 Ctrl+C 退出");
            session.Source.Process(); // 开始监听事件（阻塞）
        }
    }

    public static void Main2()
    {
        // Create real-time user session
        using (var session = new TraceEventSession("MyKernelSession"))
        {
            // Enable Kernel Network events
            session.EnableKernelProvider(
                KernelTraceEventParser.Keywords.NetworkTCPIP |
                KernelTraceEventParser.Keywords.Process |
                KernelTraceEventParser.Keywords.ImageLoad);

            // Hook into real-time events
            session.Source.Kernel.TcpIpRecv += data =>
            {
                Console.WriteLine(
                    $">>> TCP RECV: {data.ProcessID} -> {data.daddr}:{data.dport} [{data.size} bytes]");
            };

            session.Source.Kernel.TcpIpSend += data =>
            {
                Console.WriteLine(
                    $">>> TCP SEND: {data.ProcessID} -> {data.daddr}:{data.dport} [{data.size} bytes]");
            };

            session.Source.Kernel.UdpIpRecv += data =>
            {
                Console.WriteLine(
                    $">>> UDP RECV: {data.ProcessID} -> {data.daddr}:{data.dport} [{data.size} bytes]");
            };

            session.Source.Kernel.UdpIpSend += data =>
            {
                Console.WriteLine(
                    $">>> UDP SEND: {data.ProcessID} -> {data.daddr}:{data.dport} [{data.size} bytes]");
            };

            /*
             * 当 新进程启动 时触发。
                🔧 事件信息：
                data.ProcessID：启动的进程 ID。
                data.ImageFileName：可执行文件的名称（例如 chrome.exe）。
                data.CommandLine：启动进程时使用的完整命令行参数。
             */
            session.Source.Kernel.ProcessStart += data =>
            {
                Console.WriteLine(
                    $"[PROCESS START] PID={data.ProcessID} Name={data.ImageFileName} Cmd={data.CommandLine}");
            };
            /*
             * 当 进程结束 时触发。
            🔧 事件信息：
            data.ProcessID：终止的进程 ID。
            data.ImageFileName：进程对应的可执行文件名。
            ✅ 应用场景：
            用于记录进程的退出事件，可以用来做进程生命周期分析。
             */
            session.Source.Kernel.ProcessStop += data =>
            {
                Console.WriteLine($"[PROCESS STOP] PID={data.ProcessID} Name={data.ImageFileName}");
            };

            /*
             当某个模块（例如 DLL 或 EXE）被加载到某个进程时触发。
            🔧 事件信息：
            data.ProcessID：加载模块的进程 ID。
            data.FileName：被加载的模块路径（例如 C:\Windows\System32\kernel32.dll）。
            ✅ 应用场景：
            可以监控某些敏感模块是否被加载，如某些反调试或注入行为。
             */
            session.Source.Kernel.ImageLoad += data =>
            {
                Console.WriteLine($"[MODULE LOAD] PID={data.ProcessID} Image={data.FileName}");
            };

            Console.CancelKeyPress += (o, e) => session.Dispose();

            session.Source.Process();
        }
    }
}
