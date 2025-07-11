using NetVizor.Common.Utils;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading;

namespace Common.Test
{
    [TestFixture]
    public class NetworkInfoUtilTest
    {
        [Test]
        public void TestGetAllInterfaces()
        {
            TestContext.Progress.WriteLine("--- Network Interface List ---");

            // 1. Get all network interface information
            var interfaces = NetworkInfoUtil.GetAllInterfaces();
            Assert.IsNotNull(interfaces);
            Assert.IsTrue(interfaces.Count > 0, "No network interfaces found.");

            foreach (var iface in interfaces)
            {
                TestContext.Progress.WriteLine($"Index: {iface.Index}, Alias: {iface.Alias}, Status: {iface.Status}");
                TestContext.Progress.WriteLine($"  Description: {iface.Description}");
                TestContext.Progress.WriteLine($"  MAC: {iface.MacAddress}, Type: {iface.Type}");
                TestContext.Progress.WriteLine(
                    $"  Bytes Sent: {iface.BytesSent}, Bytes Received: {iface.BytesReceived}\n");
            }
        }

        [Test]
        public void TestMonitorActiveInterfaceSpeed()
        {
            var interfaces = NetworkInfoUtil.GetAllInterfaces();

            // 2. Select an active interface to monitor real-time speed
            var activeInterface = interfaces.FirstOrDefault(i =>
                i.Status == System.Net.NetworkInformation.OperationalStatus.Up
                && i.Type != System.Net.NetworkInformation.NetworkInterfaceType.Loopback
                && i.Type != System.Net.NetworkInformation.NetworkInterfaceType.Tunnel);

            if (activeInterface == null)
            {
                Assert.Inconclusive("No active, non-loopback interface found to monitor. Test skipped.");
                return; // Skip test if no suitable interface is found
            }

            TestContext.Progress.WriteLine($"--- Monitoring real-time speed for: {activeInterface.Alias} ---");
            var speedMonitor = new NetworkSpeedMonitor(activeInterface);

            // 3. Periodically update and display the network speed
            TestContext.Progress.WriteLine("Monitoring for 5 seconds...");
            for (int i = 0; i < 5; i++)
            {
                Thread.Sleep(1000); // Update every second
                speedMonitor.Update();

                string downSpeed = FormatSpeed(speedMonitor.DownloadSpeedBps);
                string upSpeed = FormatSpeed(speedMonitor.UploadSpeedBps);

                TestContext.Progress.WriteLine(
                    $"Time: {DateTime.Now:HH:mm:ss} | Download: {downSpeed} | Upload: {upSpeed}");
            }

            Assert.IsTrue(speedMonitor.DownloadSpeedBps >= 0);
            Assert.IsTrue(speedMonitor.UploadSpeedBps >= 0);
        }

        private static string FormatSpeed(double bytesPerSecond)
        {
            if (bytesPerSecond < 1024)
                return $"{bytesPerSecond:F2} B/s";
            if (bytesPerSecond < 1024 * 1024)
                return $"{bytesPerSecond / 1024:F2} KB/s";
            return $"{bytesPerSecond / (1024 * 1024):F2} MB/s";
        }
    }
}