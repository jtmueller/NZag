using System;
using Xunit;
using static NZag.Core.Tests.Helpers;

namespace NZag.Core.Tests
{
    public class TextTests
    {
        [Fact]
        public void Zork1_4ED1()
        {
            var memory = GameMemory(Zork1);
            var reader = new ZTextReader(memory);
            var s = reader.ReadString(0x4ED1);
            Assert.Equal("The grating is closed!", s);
        }

        [Fact]
        public void Zork1_1154A()
        {
            var memory = GameMemory(Zork1);
            var reader = new ZTextReader(memory);
            var s = reader.ReadString(0x1154A);
            Assert.Equal("There is a suspicious-looking individual, holding a large bag, leaning against one wall. He is armed with a deadly stiletto.", s);
        }
    }
}
