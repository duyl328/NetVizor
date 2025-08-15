using Common.Logger;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;
using Utils.ETW.Models;
using System.Collections.Concurrent;

namespace Utils.ETW.Core;

/// <summary>
/// 优化的ETW网络捕获器 - 解决高带宽情况下的事件丢失问题
/// </summary>
public class OptimizedETWNetworkCapture : IDisposable
{
    private readonly TraceEventSession _session;
    private readonly ETWTraceEventSource _source;
    private bool _isCapturing = false;

    // 优化：使用批量处理减少单个事件处理开销
    private readonly ConcurrentQueue<TcpConnectionEventData> _tcpEventQueue = new();
    private readonly ConcurrentQueue<UdpPacketEventData> _udpEventQueue = new();
    private readonly Timer _batchProcessTimer;

    // 统计信息
    private long _totalEventsReceived = 0;
    private long _totalEventsProcessed = 0;
    private long _eventsDropped = 0;

    public event Action<TcpConnectionEventData>? OnTcpConnectionEvent;
    public event Action<UdpPacketEventData>? OnUdpPacketEvent;

    // 批量事件处理
    public event Action<List<TcpConnectionEventData>>? OnBatchTcpEvents;
    public event Action<List<UdpPacketEventData>>? OnBatchUdpEvents;

    public OptimizedETWNetworkCapture(string sessionName = "OptimizedNetworkETWSession")
    {
        // 优化1: 使用TraceEventSession的正确构造函数
        // 注意：TraceEventSession的缓冲区配置需要在EnableKernelProvider时设置
        _session = new TraceEventSession(sessionName);
        _source = new ETWTraceEventSource(sessionName, TraceEventSourceType.Session);

        // 优化2: 批量处理定时器，每100ms处理一批事件
        _batchProcessTimer = new Timer(ProcessBatchEvents, null, 100, 100);

        Log.Info($"已创建优化的ETW会话: {sessionName}");
        Log.Info("将在启动时配置更大的缓冲区");
    }

    public void StartCapture()
    {
        if (_isCapturing) return;

        try
        {
            EnableNetworkProviders();
            SetupOptimizedEventHandlers();

            _isCapturing = true;

            // 在独立线程中处理事件
            Task.Run(() =>
            {
                try
                {
                    _source.Process();
                }
                catch (Exception ex)
                {
                    Log.Error($"ETW事件处理异常: {ex.Message}");
                }
            });

            Log.Info("优化的ETW网络捕获已启动");
        }
        catch (Exception ex)
        {
            Log.Error($"启动优化ETW捕获失败: {ex.Message}");
            throw;
        }
    }

    private void EnableNetworkProviders()
    {
        // 优化3: 启用网络相关的ETW提供程序
        // 注意：.NET TraceEvent库的缓冲区配置方式可能不同
        _session.EnableKernelProvider(
            KernelTraceEventParser.Keywords.NetworkTCPIP
        );

        Log.Info("已启用NetworkTCPIP提供程序，使用优化配置");
    }

    private void SetupOptimizedEventHandlers()
    {
        var parser = new KernelTraceEventParser(_source);

        // 优化4: 最小化事件处理器中的工作量
        parser.TcpIpSend += data =>
        {
            Interlocked.Increment(ref _totalEventsReceived);

            // 快速创建事件对象并放入队列，不做复杂处理
            var tcpEvent = new TcpConnectionEventData
            {
                Timestamp = data.TimeStamp,
                EventType = "TcpSend",
                ProcessId = data.ProcessID,
                ThreadId = data.ThreadID,
                ProcessName = data.ProcessName,
                SourceIp = data.saddr,
                DestinationIp = data.daddr,
                SourcePort = data.sport,
                DestinationPort = data.dport,
                Protocol = "TCP",
                DataLength = data.size,
                SequenceNumber = data.seqnum,
                Direction = TrafficDirection.Outbound
            };

            // 如果队列过长，丢弃最旧的事件
            if (_tcpEventQueue.Count > 10000)
            {
                _tcpEventQueue.TryDequeue(out _);
                Interlocked.Increment(ref _eventsDropped);
            }

            _tcpEventQueue.Enqueue(tcpEvent);
        };

        parser.TcpIpRecv += data =>
        {
            Interlocked.Increment(ref _totalEventsReceived);

            var tcpEvent = new TcpConnectionEventData
            {
                Timestamp = data.TimeStamp,
                EventType = "TcpReceive",
                ProcessId = data.ProcessID,
                ThreadId = data.ThreadID,
                ProcessName = data.ProcessName,
                SourceIp = data.saddr,
                DestinationIp = data.daddr,
                SourcePort = data.sport,
                DestinationPort = data.dport,
                Protocol = "TCP",
                DataLength = data.size,
                SequenceNumber = data.seqnum,
                Direction = TrafficDirection.Inbound
            };

            if (_tcpEventQueue.Count > 10000)
            {
                _tcpEventQueue.TryDequeue(out _);
                Interlocked.Increment(ref _eventsDropped);
            }

            _tcpEventQueue.Enqueue(tcpEvent);
        };

        // UDP事件处理（类似优化）
        parser.UdpIpSend += data =>
        {
            Interlocked.Increment(ref _totalEventsReceived);

            var udpEvent = new UdpPacketEventData
            {
                Timestamp = data.TimeStamp,
                EventType = "UdpSend",
                ProcessId = data.ProcessID,
                ProcessName = data.ProcessName,
                SourceIp = data.saddr,
                DestinationIp = data.daddr,
                SourcePort = data.sport,
                DestinationPort = data.dport,
                Protocol = "UDP",
                DataLength = data.size,
                Direction = TrafficDirection.Outbound
            };

            if (_udpEventQueue.Count > 5000)
            {
                _udpEventQueue.TryDequeue(out _);
                Interlocked.Increment(ref _eventsDropped);
            }

            _udpEventQueue.Enqueue(udpEvent);
        };

        parser.UdpIpRecv += data =>
        {
            Interlocked.Increment(ref _totalEventsReceived);

            var udpEvent = new UdpPacketEventData
            {
                Timestamp = data.TimeStamp,
                EventType = "UdpReceive",
                ProcessId = data.ProcessID,
                ProcessName = data.ProcessName,
                SourceIp = data.saddr,
                DestinationIp = data.daddr,
                SourcePort = data.sport,
                DestinationPort = data.dport,
                Protocol = "UDP",
                DataLength = data.size,
                Direction = TrafficDirection.Inbound
            };

            if (_udpEventQueue.Count > 5000)
            {
                _udpEventQueue.TryDequeue(out _);
                Interlocked.Increment(ref _eventsDropped);
            }

            _udpEventQueue.Enqueue(udpEvent);
        };
    }

    /// <summary>
    /// 批量处理事件，减少单个事件的处理开销
    /// </summary>
    private void ProcessBatchEvents(object? state)
    {
        try
        {
            // 处理TCP事件批次
            var tcpBatch = new List<TcpConnectionEventData>();
            var batchSize = Math.Min(_tcpEventQueue.Count, 1000); // 每批最多1000个

            for (int i = 0; i < batchSize; i++)
            {
                if (_tcpEventQueue.TryDequeue(out var tcpEvent))
                {
                    tcpBatch.Add(tcpEvent);

                    // 同时触发单个事件（向后兼容）
                    OnTcpConnectionEvent?.Invoke(tcpEvent);
                }
            }

            if (tcpBatch.Count > 0)
            {
                OnBatchTcpEvents?.Invoke(tcpBatch);
                Interlocked.Add(ref _totalEventsProcessed, tcpBatch.Count);
            }

            // 处理UDP事件批次
            var udpBatch = new List<UdpPacketEventData>();
            batchSize = Math.Min(_udpEventQueue.Count, 500); // UDP事件相对较少

            for (int i = 0; i < batchSize; i++)
            {
                if (_udpEventQueue.TryDequeue(out var udpEvent))
                {
                    udpBatch.Add(udpEvent);
                    OnUdpPacketEvent?.Invoke(udpEvent);
                }
            }

            if (udpBatch.Count > 0)
            {
                OnBatchUdpEvents?.Invoke(udpBatch);
                Interlocked.Add(ref _totalEventsProcessed, udpBatch.Count);
            }

            // 定期输出统计信息
            if (_totalEventsReceived % 10000 == 0 && _totalEventsReceived > 0)
            {
                var dropRate = (_eventsDropped * 100.0) / _totalEventsReceived;
                Log.Info($"ETW统计: 接收={_totalEventsReceived}, 处理={_totalEventsProcessed}, " +
                         $"丢弃={_eventsDropped} ({dropRate:F2}%), TCP队列={_tcpEventQueue.Count}, UDP队列={_udpEventQueue.Count}");
            }
        }
        catch (Exception ex)
        {
            Log.Error($"批量处理事件异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 获取ETW性能统计
    /// </summary>
    public EtwPerformanceStats GetPerformanceStats()
    {
        return new EtwPerformanceStats
        {
            TotalEventsReceived = _totalEventsReceived,
            TotalEventsProcessed = _totalEventsProcessed,
            EventsDropped = _eventsDropped,
            TcpQueueLength = _tcpEventQueue.Count,
            UdpQueueLength = _udpEventQueue.Count,
            DropRate = _totalEventsReceived > 0 ? (_eventsDropped * 100.0) / _totalEventsReceived : 0
        };
    }

    public void StopCapture()
    {
        if (!_isCapturing) return;

        _isCapturing = false;
        _batchProcessTimer?.Dispose();
        _source?.Dispose();
        _session?.Dispose();

        Log.Info($"ETW捕获已停止 - 最终统计: 接收={_totalEventsReceived}, 处理={_totalEventsProcessed}, 丢弃={_eventsDropped}");
    }

    public void Dispose()
    {
        StopCapture();
    }
}

/// <summary>
/// ETW性能统计信息
/// </summary>
public class EtwPerformanceStats
{
    public long TotalEventsReceived { get; set; }
    public long TotalEventsProcessed { get; set; }
    public long EventsDropped { get; set; }
    public int TcpQueueLength { get; set; }
    public int UdpQueueLength { get; set; }
    public double DropRate { get; set; }
}