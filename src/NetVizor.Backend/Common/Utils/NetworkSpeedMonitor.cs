using System;
using System.Diagnostics;
using System.Threading.Tasks;
using NetVizor.Common.Models;

namespace NetVizor.Common.Utils
{
    /// <summary>
    /// Monitors the real-time network speed (upload/download) for a specific network interface.
    /// </summary>
    public class NetworkSpeedMonitor
    {
        private readonly uint _interfaceIndex;
        private long _lastBytesSent;
        private long _lastBytesReceived;
        private DateTime _lastUpdateTime;

        /// <summary>
        /// Gets the real-time download speed in bytes per second.
        /// </summary>
        public double DownloadSpeedBps { get; private set; }

        /// <summary>
        /// Gets the real-time upload speed in bytes per second.
        /// </summary>
        public double UploadSpeedBps { get; private set; }

        /// <summary>
        /// Initializes a new instance of the NetworkSpeedMonitor for a given interface.
        /// </summary>
        /// <param name="interfaceInfo">The network interface to monitor.</param>
        public NetworkSpeedMonitor(NetworkInterfaceInfo interfaceInfo)
        {
            _interfaceIndex = interfaceInfo.Index;
            _lastBytesSent = interfaceInfo.BytesSent;
            _lastBytesReceived = interfaceInfo.BytesReceived;
            _lastUpdateTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Updates the speed calculation. This method should be called periodically.
        /// </summary>
        public void Update()
        {
            var currentInterfaceInfo = NetworkInfoUtil.GetInterfaceByIndex(_interfaceIndex);
            if (currentInterfaceInfo == null)
            {
                DownloadSpeedBps = 0;
                UploadSpeedBps = 0;
                return;
            }

            var currentTime = DateTime.UtcNow;
            var timeSpan = (currentTime - _lastUpdateTime).TotalSeconds;

            if (timeSpan <= 0)
            {
                return; // Avoid division by zero if called too quickly
            }

            long bytesSent = currentInterfaceInfo.BytesSent;
            long bytesReceived = currentInterfaceInfo.BytesReceived;

            UploadSpeedBps = (bytesSent - _lastBytesSent) / timeSpan;
            DownloadSpeedBps = (bytesReceived - _lastBytesReceived) / timeSpan;

            _lastBytesSent = bytesSent;
            _lastBytesReceived = bytesReceived;
            _lastUpdateTime = currentTime;
        }
    }
}