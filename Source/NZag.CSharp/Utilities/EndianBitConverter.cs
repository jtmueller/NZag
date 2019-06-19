using System;
using System.Buffers;
using System.Runtime.InteropServices;

namespace NZag.Utilities
{
    /// <summary>
    /// Equivalent of System.BitConverter, but with either endianness.
    /// </summary>
    public abstract class EndianBitConverter
    {
        #region Endianness of this converter
        /// <summary>
        /// Indicates the byte order ("endianess") in which data is converted using this class.
        /// </summary>
        /// <remarks>
        /// Different computer architectures store data using different byte orders. "Big-endian"
        /// means the most significant byte is on the left end of a word. "Little-endian" means the 
        /// most significant byte is on the right end of a word.
        /// </remarks>
        /// <returns>true if this converter is little-endian, false otherwise.</returns>
        public abstract bool IsLittleEndian { get; }

        /// <summary>
        /// Indicates the byte order ("endianess") in which data is converted using this class.
        /// </summary>
        public abstract Endianness Endianness { get; }
        #endregion

        #region Factory properties
        private static readonly LittleEndianBitConverter s_little = new LittleEndianBitConverter();
        /// <summary>
        /// Returns a little-endian bit converter instance. The same instance is
        /// always returned.
        /// </summary>
        public static LittleEndianBitConverter Little => s_little;

        private static readonly BigEndianBitConverter s_big = new BigEndianBitConverter();
        /// <summary>
        /// Returns a big-endian bit converter instance. The same instance is
        /// always returned.
        /// </summary>
        public static BigEndianBitConverter Big => s_big;
        #endregion

        #region Double/primitive conversions
        /// <summary>
        /// Converts the specified double-precision floating point number to a 
        /// 64-bit signed integer. Note: the endianness of this converter does not
        /// affect the returned value.
        /// </summary>
        /// <param name="value">The number to convert. </param>
        /// <returns>A 64-bit signed integer whose value is equivalent to value.</returns>
        public long DoubleToInt64Bits(double value) => BitConverter.DoubleToInt64Bits(value);

        /// <summary>
        /// Converts the specified 64-bit signed integer to a double-precision 
        /// floating point number. Note: the endianness of this converter does not
        /// affect the returned value.
        /// </summary>
        /// <param name="value">The number to convert. </param>
        /// <returns>A double-precision floating point number whose value is equivalent to value.</returns>
        public double Int64BitsToDouble(long value) => BitConverter.Int64BitsToDouble(value);

        /// <summary>
        /// Converts the specified single-precision floating point number to a 
        /// 32-bit signed integer. Note: the endianness of this converter does not
        /// affect the returned value.
        /// </summary>
        /// <param name="value">The number to convert. </param>
        /// <returns>A 32-bit signed integer whose value is equivalent to value.</returns>
        public int SingleToInt32Bits(float value) => new Int32SingleUnion(value).AsInt32;

        /// <summary>
        /// Converts the specified 32-bit signed integer to a single-precision floating point 
        /// number. Note: the endianness of this converter does not
        /// affect the returned value.
        /// </summary>
        /// <param name="value">The number to convert. </param>
        /// <returns>A single-precision floating point number whose value is equivalent to value.</returns>
        public float Int32BitsToSingle(int value) => new Int32SingleUnion(value).AsSingle;
        #endregion

        #region To(PrimitiveType) conversions
        /// <summary>
        /// Returns a Boolean value converted from one byte at a specified position in a byte array.
        /// </summary>
        /// <param name="value">An array of bytes.</param>
        /// <returns>true if the byte at startIndex in value is nonzero; otherwise, false.</returns>
        public bool ToBoolean(ReadOnlySpan<byte> value)
        {
            CheckByteArgument(value, 1);
            return BitConverter.ToBoolean(value);
        }

        /// <summary>
        /// Returns a Unicode character converted from two bytes at a specified position in a byte array.
        /// </summary>
        /// <param name="value">An array of bytes.</param>
        /// <returns>A character formed by two bytes beginning at startIndex.</returns>
        public char ToChar(ReadOnlySpan<byte> value) => unchecked((char)CheckedFromBytes(value, 2));

        /// <summary>
        /// Returns a double-precision floating point number converted from eight bytes 
        /// at a specified position in a byte array.
        /// </summary>
        /// <param name="value">An array of bytes.</param>
        /// <returns>A double precision floating point number formed by eight bytes beginning at startIndex.</returns>
        public double ToDouble(ReadOnlySpan<byte> value) => Int64BitsToDouble(ToInt64(value));

        /// <summary>
        /// Returns a single-precision floating point number converted from four bytes 
        /// at a specified position in a byte array.
        /// </summary>
        /// <param name="value">An array of bytes.</param>
        /// <returns>A single precision floating point number formed by four bytes beginning at startIndex.</returns>
        public float ToSingle(ReadOnlySpan<byte> value) => Int32BitsToSingle(ToInt32(value));

        /// <summary>
        /// Returns a 16-bit signed integer converted from two bytes at a specified position in a byte array.
        /// </summary>
        /// <param name="value">An array of bytes.</param>
        /// <returns>A 16-bit signed integer formed by two bytes beginning at startIndex.</returns>
        public short ToInt16(ReadOnlySpan<byte> value) => unchecked((short)CheckedFromBytes(value, 2));

        /// <summary>
        /// Returns a 32-bit signed integer converted from four bytes at a specified position in a byte array.
        /// </summary>
        /// <param name="value">An array of bytes.</param>
        /// <returns>A 32-bit signed integer formed by four bytes beginning at startIndex.</returns>
        public int ToInt32(ReadOnlySpan<byte> value) => unchecked((int)CheckedFromBytes(value, 4));

        /// <summary>
        /// Returns a 64-bit signed integer converted from eight bytes at a specified position in a byte array.
        /// </summary>
        /// <param name="value">An array of bytes.</param>
        /// <returns>A 64-bit signed integer formed by eight bytes beginning at startIndex.</returns>
        public long ToInt64(ReadOnlySpan<byte> value) => CheckedFromBytes(value, 8);

        /// <summary>
        /// Returns a 16-bit unsigned integer converted from two bytes at a specified position in a byte array.
        /// </summary>
        /// <param name="value">An array of bytes.</param>
        /// <returns>A 16-bit unsigned integer formed by two bytes beginning at startIndex.</returns>
        public ushort ToUInt16(ReadOnlySpan<byte> value) => unchecked((ushort)CheckedFromBytes(value, 2));

        /// <summary>
        /// Returns a 32-bit unsigned integer converted from four bytes at a specified position in a byte array.
        /// </summary>
        /// <param name="value">An array of bytes.</param>
        /// <returns>A 32-bit unsigned integer formed by four bytes beginning at startIndex.</returns>
        public uint ToUInt32(ReadOnlySpan<byte> value) => unchecked((uint)CheckedFromBytes(value, 4));

        /// <summary>
        /// Returns a 64-bit unsigned integer converted from eight bytes at a specified position in a byte array.
        /// </summary>
        /// <param name="value">An array of bytes.</param>
        /// <returns>A 64-bit unsigned integer formed by eight bytes beginning at startIndex.</returns>
        public ulong ToUInt64(ReadOnlySpan<byte> value) => unchecked((ulong)CheckedFromBytes(value, 8));

        /// <summary>
        /// Checks the given argument for validity.
        /// </summary>
        /// <param name="value">The byte array passed in</param>
        /// <param name="bytesRequired">The number of bytes required</param>
        /// <exception cref="ArgumentNullException">value is a null reference</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// startIndex is less than zero or greater than the length of value minus bytesRequired.
        /// </exception>
        private static void CheckByteArgument(ReadOnlySpan<byte> value, int bytesRequired)
        {
            if (value.Length < bytesRequired)
            {
                throw new ArgumentException("Span does not contain enough bytes.");
            }
        }

        /// <summary>
        /// Checks the arguments for validity before calling FromBytes
        /// (which can therefore assume the arguments are valid).
        /// </summary>
        /// <param name="value">The bytes to convert after checking</param>
        /// <param name="bytesToConvert">The number of bytes to convert</param>
        /// <returns></returns>
        private long CheckedFromBytes(ReadOnlySpan<byte> value, int bytesToConvert)
        {
            CheckByteArgument(value, bytesToConvert);
            return FromBytes(value, bytesToConvert);
        }

        /// <summary>
        /// Convert the given number of bytes from the given array, from the given start
        /// position, into a long, using the bytes as the least significant part of the long.
        /// By the time this is called, the arguments have been checked for validity.
        /// </summary>
        /// <param name="value">The bytes to convert</param>
        /// <param name="bytesToConvert">The number of bytes to use in the conversion</param>
        /// <returns>The converted number</returns>
        protected abstract long FromBytes(ReadOnlySpan<byte> value, int bytesToConvert);
        #endregion

        #region ToString conversions
        /// <summary>
        /// Returns a String converted from the elements of a byte array.
        /// </summary>
        /// <param name="value">An array of bytes.</param>
        /// <remarks>All the elements of value are converted.</remarks>
        /// <returns>
        /// A String of hexadecimal pairs separated by hyphens, where each pair 
        /// represents the corresponding element in value; for example, "7F-2C-4A".
        /// </returns>
        public static string ToString(ReadOnlySpan<byte> value)
        {
            var outputLen = (value.Length * 2) + (value.Length - 1);
            var buffer = ArrayPool<char>.Shared.Rent(outputLen);
            try
            {
                var chars = buffer.AsSpan();
                for (int i = 0; i < value.Length; i++)
                {
                    if (value[i].TryFormat(chars, out int charsWritten, "X2"))
                    {
                        if (i < value.Length)
                        {
                            chars[charsWritten] = '-';
                            chars = chars.Slice(charsWritten + 1);
                        }
                    }
                }
                return buffer.AsSpan(..outputLen).ToString();
            }
            finally
            {
                ArrayPool<char>.Shared.Return(buffer);
            }
        }
        #endregion

        #region	Decimal conversions
        /// <summary>
        /// Returns a decimal value converted from sixteen bytes 
        /// at a specified position in a byte array.
        /// </summary>
        /// <param name="value">An array of bytes.</param>
        /// <returns>A decimal  formed by sixteen bytes beginning at startIndex.</returns>
        public decimal ToDecimal(ReadOnlySpan<byte> value)
        {
            // HACK: This always assumes four parts, each in their own endianness,
            // starting with the first part at the start of the byte array.
            // On the other hand, there's no real format specified...
            int[] parts = new int[4];
            for (int i = 0; i < 4; i++)
            {
                parts[i] = ToInt32(value.Slice(i * 4));
            }
            return new decimal(parts);
        }

        /// <summary>
        /// Returns the specified decimal value as an array of bytes.
        /// </summary>
        /// <param name="value">The number to convert.</param>
        /// <returns>An array of bytes with length 16.</returns>
        public byte[] GetBytes(decimal value)
        {
            byte[] bytes = new byte[16];
            int[] parts = Decimal.GetBits(value);
            for (int i = 0; i < 4; i++)
            {
                CopyBytesImpl(parts[i], 4, bytes.AsSpan(i * 4));
            }
            return bytes;
        }

        /// <summary>
        /// Copies the specified decimal value into the specified byte array,
        /// beginning at the specified index.
        /// </summary>
        /// <param name="value">A character to convert.</param>
        /// <param name="buffer">The byte array to copy the bytes into</param>
        /// <param name="index">The first index into the array to copy the bytes into</param>
        public void CopyBytes(decimal value, Span<byte> buffer)
        {
            if (buffer.Length < 16)
                throw new ArgumentOutOfRangeException(nameof(buffer), "Buffer too small to hold 16 bytes.");

            int[] parts = Decimal.GetBits(value);
            for (int i = 0; i < 4; i++)
            {
                CopyBytesImpl(parts[i], 4, buffer.Slice(i * 4));
            }
        }
        #endregion

        #region GetBytes conversions
        /// <summary>
        /// Returns an array with the given number of bytes formed
        /// from the least significant bytes of the specified value.
        /// This is used to implement the other GetBytes methods.
        /// </summary>
        /// <param name="value">The value to get bytes for</param>
        /// <param name="bytes">The number of significant bytes to return</param>
        private byte[] GetBytes(long value, int bytes)
        {
            var buffer = new byte[bytes];
            CopyBytes(value, bytes, buffer);
            return buffer;
        }

        /// <summary>
        /// Returns the specified Boolean value as an array of bytes.
        /// </summary>
        /// <param name="value">A Boolean value.</param>
        /// <returns>An array of bytes with length 1.</returns>
        public byte[] GetBytes(bool value) => BitConverter.GetBytes(value);

        /// <summary>
        /// Returns the specified Unicode character value as an array of bytes.
        /// </summary>
        /// <param name="value">A character to convert.</param>
        /// <returns>An array of bytes with length 2.</returns>
        public byte[] GetBytes(char value) => GetBytes(value, 2);

        /// <summary>
        /// Returns the specified double-precision floating point value as an array of bytes.
        /// </summary>
        /// <param name="value">The number to convert.</param>
        /// <returns>An array of bytes with length 8.</returns>
        public byte[] GetBytes(double value) => GetBytes(DoubleToInt64Bits(value), 8);

        /// <summary>
        /// Returns the specified 16-bit signed integer value as an array of bytes.
        /// </summary>
        /// <param name="value">The number to convert.</param>
        /// <returns>An array of bytes with length 2.</returns>
        public byte[] GetBytes(short value) => GetBytes(value, 2);

        /// <summary>
        /// Returns the specified 32-bit signed integer value as an array of bytes.
        /// </summary>
        /// <param name="value">The number to convert.</param>
        /// <returns>An array of bytes with length 4.</returns>
        public byte[] GetBytes(int value) => GetBytes(value, 4);

        /// <summary>
        /// Returns the specified 64-bit signed integer value as an array of bytes.
        /// </summary>
        /// <param name="value">The number to convert.</param>
        /// <returns>An array of bytes with length 8.</returns>
        public byte[] GetBytes(long value) => GetBytes(value, 8);

        /// <summary>
        /// Returns the specified single-precision floating point value as an array of bytes.
        /// </summary>
        /// <param name="value">The number to convert.</param>
        /// <returns>An array of bytes with length 4.</returns>
        public byte[] GetBytes(float value) => GetBytes(SingleToInt32Bits(value), 4);

        /// <summary>
        /// Returns the specified 16-bit unsigned integer value as an array of bytes.
        /// </summary>
        /// <param name="value">The number to convert.</param>
        /// <returns>An array of bytes with length 2.</returns>
        public byte[] GetBytes(ushort value) => GetBytes(value, 2);

        /// <summary>
        /// Returns the specified 32-bit unsigned integer value as an array of bytes.
        /// </summary>
        /// <param name="value">The number to convert.</param>
        /// <returns>An array of bytes with length 4.</returns>
        public byte[] GetBytes(uint value) => GetBytes(value, 4);

        /// <summary>
        /// Returns the specified 64-bit unsigned integer value as an array of bytes.
        /// </summary>
        /// <param name="value">The number to convert.</param>
        /// <returns>An array of bytes with length 8.</returns>
        public byte[] GetBytes(ulong value) => GetBytes(unchecked((long)value), 8);

        #endregion

        #region CopyBytes conversions
        /// <summary>
        /// Copies the given number of bytes from the least-specific
        /// end of the specified value into the specified byte array, beginning
        /// at the specified index.
        /// This is used to implement the other CopyBytes methods.
        /// </summary>
        /// <param name="value">The value to copy bytes for</param>
        /// <param name="bytes">The number of significant bytes to copy</param>
        /// <param name="buffer">The byte array to copy the bytes into</param>
        private void CopyBytes(long value, int bytes, Span<byte> buffer)
        {
            if (buffer.Length < bytes)
            {
                throw new ArgumentOutOfRangeException("Buffer not big enough for value");
            }
            CopyBytesImpl(value, bytes, buffer);
        }

        /// <summary>
        /// Copies the given number of bytes from the least-specific
        /// end of the specified value into the specified byte array, beginning
        /// at the specified index.
        /// This must be implemented in concrete derived classes, but the implementation
        /// may assume that the value will fit into the buffer.
        /// </summary>
        /// <param name="value">The value to copy bytes for</param>
        /// <param name="bytes">The number of significant bytes to copy</param>
        /// <param name="buffer">The byte array to copy the bytes into</param>
        protected abstract void CopyBytesImpl(long value, int bytes, Span<byte> buffer);

        /// <summary>
        /// Copies the specified Boolean value into the specified byte array,
        /// beginning at the specified index.
        /// </summary>
        /// <param name="value">A Boolean value.</param>
        /// <param name="buffer">The byte array to copy the bytes into</param>
        public void CopyBytes(bool value, Span<byte> buffer) => CopyBytes(value ? 1 : 0, 1, buffer);

        /// <summary>
        /// Copies the specified Unicode character value into the specified byte array,
        /// beginning at the specified index.
        /// </summary>
        /// <param name="value">A character to convert.</param>
        /// <param name="buffer">The byte array to copy the bytes into</param>
        /// <param name="index">The first index into the array to copy the bytes into</param>
        public void CopyBytes(char value, Span<byte> buffer) => CopyBytes(value, 2, buffer);

        /// <summary>
        /// Copies the specified double-precision floating point value into the specified byte array,
        /// beginning at the specified index.
        /// </summary>
        /// <param name="value">The number to convert.</param>
        /// <param name="buffer">The byte array to copy the bytes into</param>
        public void CopyBytes(double value, Span<byte> buffer) => CopyBytes(DoubleToInt64Bits(value), 8, buffer);

        /// <summary>
        /// Copies the specified 16-bit signed integer value into the specified byte array,
        /// beginning at the specified index.
        /// </summary>
        /// <param name="value">The number to convert.</param>
        /// <param name="buffer">The byte array to copy the bytes into</param>
        public void CopyBytes(short value, Span<byte> buffer) => CopyBytes(value, 2, buffer);

        /// <summary>
        /// Copies the specified 32-bit signed integer value into the specified byte array,
        /// beginning at the specified index.
        /// </summary>
        /// <param name="value">The number to convert.</param>
        /// <param name="buffer">The byte array to copy the bytes into</param>
        public void CopyBytes(int value, Span<byte> buffer) => CopyBytes(value, 4, buffer);

        /// <summary>
        /// Copies the specified 64-bit signed integer value into the specified byte array,
        /// beginning at the specified index.
        /// </summary>
        /// <param name="value">The number to convert.</param>
        /// <param name="buffer">The byte array to copy the bytes into</param>
        public void CopyBytes(long value, Span<byte> buffer) => CopyBytes(value, 8, buffer);

        /// <summary>
        /// Copies the specified single-precision floating point value into the specified byte array,
        /// beginning at the specified index.
        /// </summary>
        /// <param name="value">The number to convert.</param>
        /// <param name="buffer">The byte array to copy the bytes into</param>
        public void CopyBytes(float value, Span<byte> buffer) => CopyBytes(SingleToInt32Bits(value), 4, buffer);

        /// <summary>
        /// Copies the specified 16-bit unsigned integer value into the specified byte array,
        /// beginning at the specified index.
        /// </summary>
        /// <param name="value">The number to convert.</param>
        /// <param name="buffer">The byte array to copy the bytes into</param>
        public void CopyBytes(ushort value, Span<byte> buffer) => CopyBytes(value, 2, buffer);

        /// <summary>
        /// Copies the specified 32-bit unsigned integer value into the specified byte array,
        /// beginning at the specified index.
        /// </summary>
        /// <param name="value">The number to convert.</param>
        /// <param name="buffer">The byte array to copy the bytes into</param>
        public void CopyBytes(uint value, Span<byte> buffer) => CopyBytes(value, 4, buffer);

        /// <summary>
        /// Copies the specified 64-bit unsigned integer value into the specified byte array,
        /// beginning at the specified index.
        /// </summary>
        /// <param name="value">The number to convert.</param>
        /// <param name="buffer">The byte array to copy the bytes into</param>
        public void CopyBytes(ulong value, Span<byte> buffer) => CopyBytes(unchecked((long)value), 8, buffer);

        #endregion

        #region Private struct used for Single/Int32 conversions
        /// <summary>
        /// Union used solely for the equivalent of DoubleToInt64Bits and vice versa.
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        private struct Int32SingleUnion
        {
            /// <summary>
            /// Int32 version of the value.
            /// </summary>
            [FieldOffset(0)]
            private readonly int _i;
            /// <summary>
            /// Single version of the value.
            /// </summary>
            [FieldOffset(0)]
            private readonly float _f;

            /// <summary>
            /// Creates an instance representing the given integer.
            /// </summary>
            /// <param name="i">The integer value of the new instance.</param>
            internal Int32SingleUnion(int i)
            {
                _f = 0; // Just to keep the compiler happy
                _i = i;
            }

            /// <summary>
            /// Creates an instance representing the given floating point number.
            /// </summary>
            /// <param name="f">The floating point value of the new instance.</param>
            internal Int32SingleUnion(float f)
            {
                _i = 0; // Just to keep the compiler happy
                _f = f;
            }

            /// <summary>
            /// Returns the value of the instance as an integer.
            /// </summary>
            internal int AsInt32 => _i;

            /// <summary>
            /// Returns the value of the instance as a floating point number.
            /// </summary>
            internal float AsSingle => _f;
        }
        #endregion
    }
}
