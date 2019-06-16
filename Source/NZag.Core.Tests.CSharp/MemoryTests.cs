using System;
using Xunit;
using static NZag.Core.Tests.Helpers;

namespace NZag.Core.Tests
{
    public class MemoryTests
    {
        private const int s_memorySize = 0x30_000;
        private const int s_writeLen = s_memorySize - 0x40;

        [Fact]
        public void ReadByte()
        {
            var memory = CreateMemory(8, s_memorySize);

            // write bytes
            for (int i = 0; i < s_writeLen; i++)
            {
                int a = 0x40 + i;
                memory.WriteByte(a, (byte)(i % Byte.MaxValue));
            }

            // read bytes
            for (int i = 0; i < s_writeLen; i++)
            {
                int a = 0x40 + i;
                int b = (byte)(i % Byte.MaxValue);
                byte v = memory.ReadByte(a);
                Assert.Equal(b, v);
            }
        }

        [Fact]
        public void Read()
        {
            var memory = CreateMemory(8, s_memorySize);

            // write bytes
            for (int i = 0; i < s_writeLen; i++)
            {
                int a = 0x40 + i;
                memory.WriteByte(a, (byte)(i % Byte.MaxValue));
            }

            // read bytes
            byte[] bytes = new byte[s_writeLen];
            memory.Read(bytes, 0, s_writeLen, 0x40);

            Assert.Equal(s_writeLen, bytes.Length);

            for (int i = 0; i < s_writeLen; i++)
            {
                byte b = (byte)(i % Byte.MaxValue);
                byte v = bytes[i];
                Assert.Equal(b, v);
            }
        }

        [Fact]
        public void ReadBytes()
        {
            var memory = CreateMemory(8, s_memorySize);

            // write bytes
            for (int i = 0; i < s_writeLen; i++)
            {
                int a = 0x40 + i;
                memory.WriteByte(a, (byte)(i % Byte.MaxValue));
            }

            // read bytes
            var bytes = memory.ReadBytes(0x40, s_writeLen);

            Assert.Equal(s_writeLen, bytes.Length);

            for (int i = 0; i < s_writeLen; i++)
            {
                byte b = (byte)(i % Byte.MaxValue);
                byte v = bytes[i];
                Assert.Equal(b, v);
            }
        }

        [Fact]
        public void ReadWord()
        {
            var memory = CreateMemory(8, s_memorySize);

            // Write words
            for (int i = 0; i < s_writeLen / 2; i += 2)
            {
                int a = 0x40 + i;
                memory.WriteWord(a, (ushort)(i % UInt16.MaxValue));
            }

            // read words
            for (int i = 0; i < s_writeLen / 2; i += 2)
            {
                int a = 0x40 + i;
                ushort w = (ushort)(i % UInt16.MaxValue);
                ushort v = memory.ReadWord(a);
                Assert.Equal(w, v);
            }
        }

        [Fact]
        public void ReadDWord()
        {
            var memory = CreateMemory(8, s_memorySize);

            // write dwords
            for (int i = 0; i < s_writeLen / 4; i += 4)
            {
                int a = 0x40 + i;
                memory.WriteDWord(a, (uint)(i % UInt32.MaxValue));
            }

            // read dwords
            for (int i = 0; i < s_writeLen / 4; i += 4)
            {
                int a = 0x40 + i;
                uint dw = (uint)(i % UInt32.MaxValue);
                uint v = memory.ReadDWord(a);
                Assert.Equal(dw, v);
            }
        }

        [Fact]
        public void MemoryRead_NextByte()
        {
            var memory = CreateMemory(8, s_memorySize);

            // write bytes
            for (int i = 0; i < s_writeLen; i++)
            {
                int a = 0x40 + i;
                memory.WriteByte(a, (byte)(i % Byte.MaxValue));
            }

            // read bytes
            var reader = memory.CreateMemoryReader(0x40);
            for (int i = 0; i < s_writeLen; i++)
            {
                byte b = (byte)(i % Byte.MaxValue);
                byte v = reader.NextByte();
                Assert.Equal(b, v);
            }
        }

        [Fact]
        public void MemoryReader_NextBytes()
        {
            var memory = CreateMemory(8, s_memorySize);

            // write bytes
            for (int i = 0; i < s_writeLen; i++)
            {
                int a = 0x40 + i;
                memory.WriteByte(a, (byte)(i % Byte.MaxValue));
            }

            // read bytes
            var reader = memory.CreateMemoryReader(0x40);
            var bytes = reader.NextBytes(s_writeLen);

            Assert.Equal(s_writeLen, bytes.Length);

            for (int i = 0; i < s_writeLen; i++)
            {
                byte b = (byte)(i % Byte.MaxValue);
                byte v = bytes[i];
                Assert.Equal(b, v);
            }
        }

        [Fact]
        public void MemoryReader_NextWord()
        {
            var memory = CreateMemory(8, s_memorySize);

            // write words
            for (int i = 0; i < s_writeLen / 2; i += 2)
            {
                int a = 0x40 + i;
                memory.WriteWord(a, (ushort)(i % UInt16.MaxValue));
            }

            // read words
            var reader = memory.CreateMemoryReader(0x40);
            for (int i = 0; i < s_writeLen / 2; i += 2)
            {
                ushort w = (ushort)(i % UInt16.MaxValue);
                ushort v = reader.NextWord();
                Assert.Equal(w, v);
            }
        }

        [Fact]
        public void MemoryReader_NextDWord()
        {
            var memory = CreateMemory(8, s_memorySize);

            // write dwords
            for (int i = 0; i < s_writeLen / 4; i += 4)
            {
                int a = 0x40 + i;
                memory.WriteDWord(a, (uint)(i % UInt32.MaxValue));
            }

            // read dwords
            var reader = memory.CreateMemoryReader(0x40);
            for (int i = 0; i < s_writeLen / 4; i += 4)
            {
                uint dw = (uint)(i % UInt32.MaxValue);
                uint v = reader.NextDWord();
                Assert.Equal(dw, v);
            }
        }

        [Fact]
        public void BadVersionNumber()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                CreateMemory(0, Span<byte>.Empty);
            });

            for (byte i = 1; i <= 8; i++)
            {
                byte v = i;
                var m = CreateMemory(v, Span<byte>.Empty);
                Assert.NotNull(m);
            }

            Assert.Throws<InvalidOperationException>(() =>
            {
                CreateMemory(9, Span<byte>.Empty);
            });
        }

        [Fact]
        public void WriteBytes()
        {
            var memory = CreateMemory(8, s_memorySize);

            // initially, write memory with zeroes
            for (int i = 0; i < s_writeLen; i++)
            {
                int a = 0x40 + i;
                memory.WriteByte(a, 0);
            }

            // create value to write
            byte[] value = new byte[s_writeLen];
            for (int i = 0; i < s_writeLen; i++)
            {
                value[i] = (byte)(i % Byte.MaxValue);
            }

            // actually write the bytes
            memory.WriteBytes(0x40, value);

            // read the bytes back
            for (int i = 0; i < s_writeLen; i++)
            {
                int a = 0x40 + i;
                byte b = (byte)(i % Byte.MaxValue);
                byte v = memory.ReadByte(a);
                Assert.Equal(b, v);
            }
        }
    }
}
