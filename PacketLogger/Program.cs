using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.Remoting;
using EasyHook;
using PacketLoggerHook;

namespace PacketLogger
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.Error.WriteLine("Usage: PacketLogger.exe <process_name>");
                return;
            }

            string channelName = null;

            RemoteHooking.IpcCreateServer<ServerInterface>(ref channelName, WellKnownObjectMode.Singleton);
            var injectionLibrary =
                Path.Combine(
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ??
                    throw new InvalidOperationException(),
                    "PacketLoggerHook.dll");

            foreach (var process in Process.GetProcesses())
            {
                if (process.ProcessName == args[0])
                {
                    Console.WriteLine("Found a process: {0}", process.ProcessName);
                    RemoteHooking.Inject(
                        process.Id,
                        injectionLibrary,
                        injectionLibrary,
                        channelName
                    );
                    break;
                }
            }

            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }
    }
}