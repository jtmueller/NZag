
using NZag.CSharp.MiscUtil;
using System;
using Xunit;

namespace NZag.Core.Tests.MiscUtil
{
    public class TestBigEndianBitConverter
    {
        [Fact]
        public void GetBytesShort()
        {
            CheckBytes(new byte[] { 0, 0 }, EndianBitConverter.Big.GetBytes((short)0));
            CheckBytes(new byte[] { 0, 1 }, EndianBitConverter.Big.GetBytes((short)1));
            CheckBytes(new byte[] { 1, 0 }, EndianBitConverter.Big.GetBytes((short)256));
            CheckBytes(new byte[] { 0xff, 0xff }, EndianBitConverter.Big.GetBytes((short)-1));
            CheckBytes(new byte[] { 1, 1 }, EndianBitConverter.Big.GetBytes((short)257));
        }

        [Fact]
        public void GetBytesUShort()
        {
            CheckBytes(new byte[] { 0, 0 }, EndianBitConverter.Big.GetBytes((ushort)0));
            CheckBytes(new byte[] { 0, 1 }, EndianBitConverter.Big.GetBytes((ushort)1));
            CheckBytes(new byte[] { 1, 0 }, EndianBitConverter.Big.GetBytes((ushort)256));
            CheckBytes(new byte[] { 0xff, 0xff }, EndianBitConverter.Big.GetBytes(UInt16.MaxValue));
            CheckBytes(new byte[] { 1, 1 }, EndianBitConverter.Big.GetBytes((ushort)257));
        }

        [Fact]
        public void GetBytesInt()
        {
            CheckBytes(new byte[] { 0, 0, 0, 0 }, EndianBitConverter.Big.GetBytes(0));
            CheckBytes(new byte[] { 0, 0, 0, 1 }, EndianBitConverter.Big.GetBytes(1));
            CheckBytes(new byte[] { 0, 0, 1, 0 }, EndianBitConverter.Big.GetBytes(256));
            CheckBytes(new byte[] { 0, 1, 0, 0 }, EndianBitConverter.Big.GetBytes(65536));
            CheckBytes(new byte[] { 1, 0, 0, 0 }, EndianBitConverter.Big.GetBytes(16777216));
            CheckBytes(new byte[] { 0xff, 0xff, 0xff, 0xff }, EndianBitConverter.Big.GetBytes(-1));
            CheckBytes(new byte[] { 0, 0, 1, 1 }, EndianBitConverter.Big.GetBytes(257));
        }

        [Fact]
        public void GetBytesUInt()
        {
            CheckBytes(new byte[] { 0, 0, 0, 0 }, EndianBitConverter.Big.GetBytes((uint)0));
            CheckBytes(new byte[] { 0, 0, 0, 1 }, EndianBitConverter.Big.GetBytes((uint)1));
            CheckBytes(new byte[] { 0, 0, 1, 0 }, EndianBitConverter.Big.GetBytes((uint)256));
            CheckBytes(new byte[] { 0, 1, 0, 0 }, EndianBitConverter.Big.GetBytes((uint)65536));
            CheckBytes(new byte[] { 1, 0, 0, 0 }, EndianBitConverter.Big.GetBytes((uint)16777216));
            CheckBytes(new byte[] { 0xff, 0xff, 0xff, 0xff }, EndianBitConverter.Big.GetBytes(UInt32.MaxValue));
            CheckBytes(new byte[] { 0, 0, 1, 1 }, EndianBitConverter.Big.GetBytes((uint)257));
        }

        [Fact]
        public void GetBytesLong()
        {
            CheckBytes(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, EndianBitConverter.Big.GetBytes(0L));
            CheckBytes(new byte[] { 0, 0, 0, 0, 0, 0, 0, 1 }, EndianBitConverter.Big.GetBytes(1L));
            CheckBytes(new byte[] { 0, 0, 0, 0, 0, 0, 1, 0 }, EndianBitConverter.Big.GetBytes(256L));
            CheckBytes(new byte[] { 0, 0, 0, 0, 0, 1, 0, 0 }, EndianBitConverter.Big.GetBytes(65536L));
            CheckBytes(new byte[] { 0, 0, 0, 0, 1, 0, 0, 0 }, EndianBitConverter.Big.GetBytes(16777216L));
            CheckBytes(new byte[] { 0, 0, 0, 1, 0, 0, 0, 0 }, EndianBitConverter.Big.GetBytes(4294967296L));
            CheckBytes(new byte[] { 0, 0, 1, 0, 0, 0, 0, 0 }, EndianBitConverter.Big.GetBytes(1099511627776L));
            CheckBytes(new byte[] { 0, 1, 0, 0, 0, 0, 0, 0 }, EndianBitConverter.Big.GetBytes(1099511627776L * 256));
            CheckBytes(new byte[] { 1, 0, 0, 0, 0, 0, 0, 0 }, EndianBitConverter.Big.GetBytes(1099511627776L * 256 * 256));
            CheckBytes(new byte[] { 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff }, EndianBitConverter.Big.GetBytes(-1L));
            CheckBytes(new byte[] { 0, 0, 0, 0, 0, 0, 1, 1 }, EndianBitConverter.Big.GetBytes(257L));
        }

        [Fact]
        public void GetBytesULong()
        {
            CheckBytes(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, EndianBitConverter.Big.GetBytes(0UL));
            CheckBytes(new byte[] { 0, 0, 0, 0, 0, 0, 0, 1 }, EndianBitConverter.Big.GetBytes(1UL));
            CheckBytes(new byte[] { 0, 0, 0, 0, 0, 0, 1, 0 }, EndianBitConverter.Big.GetBytes(256UL));
            CheckBytes(new byte[] { 0, 0, 0, 0, 0, 1, 0, 0 }, EndianBitConverter.Big.GetBytes(65536UL));
            CheckBytes(new byte[] { 0, 0, 0, 0, 1, 0, 0, 0 }, EndianBitConverter.Big.GetBytes(16777216UL));
            CheckBytes(new byte[] { 0, 0, 0, 1, 0, 0, 0, 0 }, EndianBitConverter.Big.GetBytes(4294967296UL));
            CheckBytes(new byte[] { 0, 0, 1, 0, 0, 0, 0, 0 }, EndianBitConverter.Big.GetBytes(1099511627776UL));
            CheckBytes(new byte[] { 0, 1, 0, 0, 0, 0, 0, 0 }, EndianBitConverter.Big.GetBytes(1099511627776UL * 256));
            CheckBytes(new byte[] { 1, 0, 0, 0, 0, 0, 0, 0 }, EndianBitConverter.Big.GetBytes(1099511627776UL * 256 * 256));
            CheckBytes(new byte[] { 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff }, EndianBitConverter.Big.GetBytes(UInt64.MaxValue));
            CheckBytes(new byte[] { 0, 0, 0, 0, 0, 0, 1, 1 }, EndianBitConverter.Big.GetBytes(257UL));
        }

        private void CheckBytes(Span<byte> expected, ReadOnlySpan<byte> actual) 
            => Assert.True(expected.SequenceEqual(actual), "Actual bytes did not match expected bytes.");
    }
}
