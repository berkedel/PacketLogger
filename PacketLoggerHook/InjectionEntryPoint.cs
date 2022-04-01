using System;
using System.Runtime.InteropServices;
using System.Threading;
using EasyHook;

namespace PacketLoggerHook
{
    public class InjectionEntryPoint : IEntryPoint
    {
        private readonly ServerInterface _server;
        private readonly string _channelName;
        private LocalHook _wsaSendHook;
        private LocalHook _wsaRecvHook;

        public InjectionEntryPoint(RemoteHooking.IContext context, string channelName)
        {
            _server = RemoteHooking.IpcConnectClient<ServerInterface>(channelName);
            _channelName = channelName;
        }

        public void Run(RemoteHooking.IContext context, string channelName)
        {
            _server.IsInstalled(RemoteHooking.GetCurrentProcessId());

            _wsaSendHook = LocalHook.Create(
                LocalHook.GetProcAddress("Ws2_32.dll", "WSASend"),
                new WSASend_Delegate(WSASend_Hook),
                this
            );
            _wsaRecvHook = LocalHook.Create(
                LocalHook.GetProcAddress("Ws2_32.dll", "WSARecv"),
                new WSARecv_Delegate(WSARecv_Hook),
                this
            );

            _wsaSendHook.ThreadACL.SetExclusiveACL(new[] {0});
            _wsaRecvHook.ThreadACL.SetExclusiveACL(new[] {0});

            _server.Log("Hook WSASend installed");

            RemoteHooking.WakeUpProcess();

            while (true)
            {
                Thread.Sleep(500);
            }
        }

        public void Stop()
        {
            _wsaSendHook.Dispose();
            _wsaRecvHook.Dispose();
            LocalHook.Release();
        }

        #region WSASend Hook

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        delegate int WSASend_Delegate(
            IntPtr s,
            IntPtr lpBuffers,
            uint dwBufferCount,
            IntPtr lpNumberOfBytesSent,
            uint dwFlags,
            uint lpOverlapped,
            uint lpCompletionRoutine
        );

        [DllImport("Ws2_32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern int WSASend(
            IntPtr s,
            IntPtr lpBuffers,
            uint dwBufferCount,
            IntPtr lpNumberOfBytesSent,
            uint dwFlags,
            uint lpOverlapped,
            uint lpCompletionRoutine
        );

        int WSASend_Hook(
            IntPtr s,
            IntPtr lpBuffers,
            uint dwBufferCount,
            IntPtr lpNumberOfBytesSent,
            uint dwFlags,
            uint lpOverlapped,
            uint lpCompletionRoutine
        )
        {
            var result = WSASend(s, lpBuffers, dwBufferCount, lpNumberOfBytesSent, dwFlags, lpOverlapped,
                lpCompletionRoutine);

            var respSize = Marshal.ReadInt32(lpNumberOfBytesSent);
            if (respSize > 0)
            {
                var data = new byte[respSize];
                Marshal.Copy(Marshal.ReadIntPtr(lpBuffers, 4), data, 0, respSize);
                _server.Log("Send", data);
            }

            return result;
        }

        #endregion

        #region WSARecv Hook

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        delegate int WSARecv_Delegate(
            IntPtr s,
            IntPtr lpBuffers,
            uint dwBufferCount,
            IntPtr lpNumberOfBytesRecvd,
            uint lpFlags,
            uint lpOverlapped,
            uint lpCompletionRoutine
        );

        [DllImport("Ws2_32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern int WSARecv(
            IntPtr s,
            IntPtr lpBuffers,
            uint dwBufferCount,
            IntPtr lpNumberOfBytesRecvd,
            uint lpFlags,
            uint lpOverlapped,
            uint lpCompletionRoutine
        );

        int WSARecv_Hook(
            IntPtr s,
            IntPtr lpBuffers,
            uint dwBufferCount,
            IntPtr lpNumberOfBytesRecvd,
            uint lpFlags,
            uint lpOverlapped,
            uint lpCompletionRoutine)
        {
            var result = WSARecv(s, lpBuffers, dwBufferCount, lpNumberOfBytesRecvd, lpFlags, lpOverlapped,
                lpCompletionRoutine);

            var respSize = Marshal.ReadInt32(lpNumberOfBytesRecvd);
            if (respSize > 0)
            {
                var data = new byte[respSize];
                Marshal.Copy(Marshal.ReadIntPtr(lpBuffers, 4), data, 0, respSize);
                _server.Log("Recv", data);
            }

            return result;
        }

        #endregion
    }
}