using System;
using System.IO;

namespace PacketLoggerHook
{
    public class ServerInterface : MarshalByRefObject
    {
        private readonly HexStringFormatter _formatter = new HexStringFormatter();
        private readonly TextWriter _writer = Console.Out;

        public void IsInstalled(int clientPid)
        {
            _formatter.Output = _writer;
            Console.WriteLine("PacketLogger has injected PacketLoggerHook into process {0}.\r\n", clientPid);
        }

        public void Log(string message)
        {
            Console.WriteLine(DateTime.Now + ":" + message);
        }

        public void Log(string tag, byte[] message)
        {
            _writer.WriteLine(DateTime.Now + ":" + tag);
            _formatter.ConvertToString(message);
            _writer.WriteLine();
        }
    }
}