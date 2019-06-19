
using System;

namespace NZag.Utilities
{
    /// <summary>
    /// Implementation of EndianBitConverter which converts to/from little-endian
    /// byte arrays.
    /// </summary>
    public sealed class LittleEndianBitConverter : EndianBitConverter
    {
        /// <summary>
        /// Indicates the byte order ("endianess") in which data is converted using this class.
        /// </summary>
        /// <remarks>
        /// Different computer architectures store data using different byte orders. "Big-endian"
        /// means the most significant byte is on the left end of a word. "Little-endian" means the 
        /// most significant byte is on the right end of a word.
        /// </remarks>
        /// <returns>true if this converter is little-endian, false otherwise.</returns>
        public sealed override bool IsLittleEndian => true;

        /// <summary>
        /// Indicates the byte order ("endianess") in which data is converted using this class.
        /// </summary>
        public sealed override Endianness Endianness => Endianness.LittleEndian;

        /// <summary>
        /// Copies the specified number of bytes from value to buffer, starting at index.
        /// </summary>
        /// <param name="value">The value to copy</param>
        /// <param name="bytes">The number of bytes to copy</param>
        /// <param name="buffer">The buffer to copy the bytes into</param>
        protected override void CopyBytesImpl(long value, int bytes, Span<byte> buffer)
        {
            for (int i = 0; i < bytes; i++)
            {
                buffer[i] = unchecked((byte)(value & 0xff));
                value >>= 8;
            }
        }

        /// <summary>
        /// Returns a value built from the specified number of bytes from the given buffer,
        /// starting at index.
        /// </summary>
        /// <param name="buffer">The data in byte array format</param>
        /// <param name="bytesToConvert">The number of bytes to use</param>
        /// <returns>The value built from the given bytes</returns>
        protected override long FromBytes(ReadOnlySpan<byte> buffer, int bytesToConvert)
        {
            long ret = 0;
            for (int i = 0; i < bytesToConvert; i++)
            {
                ret = unchecked((ret << 8) | buffer[bytesToConvert - 1 - i]);
            }
            return ret;
        }
    }
}
