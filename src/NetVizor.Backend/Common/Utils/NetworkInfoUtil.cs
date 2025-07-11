using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using NetVizor.Common.Models;

namespace NetVizor.Common.Utils
{
    /// <summary>
    /// Provides utility methods to retrieve network interface information
    /// using the Windows IP Helper API (iphlpapi.dll).
    /// This class does not require administrator privileges.
    /// </summary>
    public static class NetworkInfoUtil
    {
        #region P/Invoke Definitions

        private const int ERROR_SUCCESS = 0;
        private const int MAX_INTERFACE_NAME_LEN = 256;
        private const int MAXLEN_PHYSADDR = 8;
        private const int MAXLEN_IFDESCR = 256;

        [DllImport("iphlpapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern uint GetIfTable2(out IntPtr pIfTable);

        [DllImport("iphlpapi.dll", SetLastError = true)]
        private static extern void FreeMibTable(IntPtr pMibTable);

        private enum TunnelType
        {
            TUNNEL_TYPE_NONE = 0,
            TUNNEL_TYPE_OTHER = 1,
            TUNNEL_TYPE_DIRECT = 2,
            TUNNEL_TYPE_6TO4 = 11,
            TUNNEL_TYPE_ISATAP = 13,
            TUNNEL_TYPE_TEREDO = 14,
            TUNNEL_TYPE_IPHTTPS = 15
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct MIB_IF_ROW2
        {
            public long InterfaceLuid;
            public uint InterfaceIndex;
            public Guid InterfaceGuid;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_INTERFACE_NAME_LEN + 1)]
            public string Alias;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAXLEN_IFDESCR + 1)]
            public string Description;

            public uint PhysicalAddressLength;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAXLEN_PHYSADDR)]
            public byte[] PhysicalAddress;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAXLEN_PHYSADDR)]
            public byte[] PermanentPhysicalAddress;

            public uint Mtu;
            public NetworkInterfaceType Type;
            public TunnelType TunnelType;
            public uint MediaConnectState;
            public uint MediaDuplexState;
            public uint InterfaceAndOperStatusFlags;
            public OperationalStatus OperStatus;
            public uint AdminStatus;
            public uint NetLuidIndex;
            public uint NdisMedium;
            public uint NdisPhysicalMedium;

            public uint HardareInterface;

            // --- Start of Corrected Fields ---
            public ulong ReceiveLinkSpeed;

            public ulong TransmitLinkSpeed;

            // --- End of Corrected Fields ---
            public ulong OutOctets;
            public ulong InOctets;
            public ulong OutUcastPkts;
            public ulong InUcastPkts;
            public ulong OutNUcastPkts;
            public ulong InNUcastPkts;
            public ulong OutDiscards;
            public ulong InDiscards;
            public ulong OutErrors;
            public ulong InErrors;
            public ulong OutUcastOctets;
            public ulong InUcastOctets;
            public ulong OutMulticastOctets;
            public ulong InMulticastOctets;
            public ulong OutBroadcastOctets;
            public ulong InBroadcastOctets;
            public uint OutQLen;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MIB_IF_TABLE2
        {
            public uint NumEntries;
            // The Table field is a placeholder here. We'll calculate its position manually.
        }

        #endregion

        /// <summary>
        /// Gets a list of all network interfaces on the system.
        /// </summary>
        /// <returns>A list of NetworkInterfaceInfo objects.</returns>
        /// <exception cref="Win32Exception">Thrown if the underlying Windows API call fails.</exception>
        public static List<NetworkInterfaceInfo> GetAllInterfaces()
        {
            IntPtr pIfTable = IntPtr.Zero;
            try
            {
                uint result = GetIfTable2(out pIfTable);
                if (result != ERROR_SUCCESS)
                {
                    throw new Win32Exception((int)result);
                }

                var ifTable = (MIB_IF_TABLE2)Marshal.PtrToStructure(pIfTable, typeof(MIB_IF_TABLE2));
                var interfaces = new List<NetworkInterfaceInfo>((int)ifTable.NumEntries);

                IntPtr currentPtr = new IntPtr(pIfTable.ToInt64() + Marshal.SizeOf(typeof(uint)));

                for (int i = 0; i < ifTable.NumEntries; i++)
                {
                    var row = (MIB_IF_ROW2)Marshal.PtrToStructure(currentPtr, typeof(MIB_IF_ROW2));
                    interfaces.Add(MapRowToInterfaceInfo(row));
                    currentPtr = new IntPtr(currentPtr.ToInt64() + Marshal.SizeOf(typeof(MIB_IF_ROW2)));
                }

                return interfaces;
            }
            finally
            {
                if (pIfTable != IntPtr.Zero)
                {
                    FreeMibTable(pIfTable);
                }
            }
        }

        /// <summary>
        /// Gets detailed information for a specific network interface by its index.
        /// </summary>
        /// <param name="interfaceIndex">The index of the interface to retrieve.</param>
        /// <returns>A NetworkInterfaceInfo object, or null if not found.</returns>
        public static NetworkInterfaceInfo? GetInterfaceByIndex(uint interfaceIndex)
        {
            var allInterfaces = GetAllInterfaces();
            foreach (var iface in allInterfaces)
            {
                if (iface.Index == interfaceIndex)
                {
                    return iface;
                }
            }

            return null;
        }

        private static NetworkInterfaceInfo MapRowToInterfaceInfo(MIB_IF_ROW2 row)
        {
            // Guard against invalid address length reported by the API
            int macAddrLen = (int)Math.Min(row.PhysicalAddressLength, (uint)MAXLEN_PHYSADDR);
            var macBytes = new byte[macAddrLen];
            if (macAddrLen > 0)
            {
                Array.Copy(row.PhysicalAddress, macBytes, macAddrLen);
            }

            return new NetworkInterfaceInfo
            {
                Index = row.InterfaceIndex,
                InterfaceId = row.InterfaceGuid.ToString(),
                Alias = row.Alias,
                Description = row.Description,
                Type = row.Type,
                Status = row.OperStatus,
                Speed = row.ReceiveLinkSpeed, // Correctly use the speed field from the struct
                MacAddress = macAddrLen > 0 ? BitConverter.ToString(macBytes).Replace('-', ':') : string.Empty,
                BytesSent = (long)row.OutOctets,
                BytesReceived = (long)row.InOctets
            };
        }
    }
}