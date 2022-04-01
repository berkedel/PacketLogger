using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PacketLoggerHook
{
    public class HexStringFormatter
    {
        public int BytesPerLine;
        public bool ShowHeader;
        public bool ShowOffset;
        public bool ShowAscii;
        public TextWriter Output;

        public HexStringFormatter()
        {
            BytesPerLine = 16;
            ShowHeader = true;
            ShowOffset = true;
            ShowAscii = true;
            Output = Console.Out;
        }

        public void ConvertToString(IEnumerable<byte> data)
        {
            if (ShowHeader)
            {
                WriteHeader();
            }

            WriteBody(data);
        }

        private void WriteHeader()
        {
            if (ShowOffset)
            {
                Output.Write("Offset(h)  ");
            }

            for (var i = 0; i < BytesPerLine; i++)
            {
                Output.Write($"{i & 0xFF:X2}");
                if (i + 1 < BytesPerLine)
                {
                    Output.Write(" ");
                }
            }

            Output.WriteLine();
        }

        private void WriteBody(IEnumerable<byte> data)
        {
            var index = 0;
            while (index < data.Count())
            {
                if (index % BytesPerLine == 0)
                {
                    if (index > 0)
                    {
                        if (ShowAscii)
                        {
                            WriteAscii(index, data);
                        }

                        Output.WriteLine();
                    }

                    if (ShowOffset)
                    {
                        WriteOffset(index);
                    }
                }

                WriteByte(index, data);
                index++;

                if (index % BytesPerLine != 0 && index < data.Count())
                {
                    Output.Write(" ");
                }
            }

            if (ShowAscii)
            {
                WriteAscii(index, data);
            }
            
            Output.WriteLine();
        }

        private void WriteOffset(int index)
        {
            Output.Write($"{index:X8}   ");
        }

        private void WriteByte(int index, IEnumerable<byte> bytes)
        {
            Output.Write($"{bytes.ElementAt(index):X2}");
        }

        private void WriteAscii(int index, IEnumerable<byte> bytes)
        {
            var backtrack = ((index - 1) / BytesPerLine) * BytesPerLine;
            var length = index - backtrack;

            // This is to fill up last string of the dump if it's shorter than _bytesPerLine
            Output.Write(new string(' ', (BytesPerLine - length) * 3));

            Output.Write("   ");
            for (var i = 0; i < length; i++)
            {
                Output.Write(Translate(bytes.ElementAt(backtrack + i)));
            }
        }

        private string Translate(byte b)
        {
            return b < 32 ? "." : Encoding.ASCII.GetString(new[] {b});
        }
    }
}