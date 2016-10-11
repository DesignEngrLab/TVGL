// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Created          : 10-05-2016
//
// Last Modified By : Matt
// Last Modified On : 10-05-2016
// ***********************************************************************
// This file is taken and adopted from Microsoft's BitConverter
// ==++==

namespace TVGL
{
    internal static unsafe class TVGLBitConverter
    {
        #region convert to hex string
        /// <summary>
        /// Gets the hexadecimal value.
        /// </summary>
        /// <param name="i">The i.</param>
        /// <returns>System.Char.</returns>
        private static char GetHexValue(int i)
        {
            if (i < 10)
                return (char) (i + '0');

            return (char) (i - 10 + 'A');
        }
        // Converts an array of bytes into a String.  
        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="startIndex">The start index.</param>
        /// <param name="length">The length.</param>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        internal static string ToString(byte[] value, int startIndex, int length)
        {
            if (length == 0)
                return string.Empty;

            var chArrayLength = length*3;

            var chArray = new char[chArrayLength];
            var i = 0;
            var index = startIndex;
            for (i = 0; i < chArrayLength; i += 3)
            {
                var b = value[index++];
                chArray[i] = GetHexValue(b/16);
                chArray[i + 1] = GetHexValue(b%16);
                chArray[i + 2] = '-';
            }

            // We don't need the last '-' character
            return new string(chArray, 0, chArray.Length - 1);
        }

        // Converts an array of bytes into a String.  
        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        internal static string ToString(byte[] value)
        {
            return ToString(value, 0, value.Length);
        }

        // Converts an array of bytes into a String.  
        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="startIndex">The start index.</param>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        internal static string ToString(byte[] value, int startIndex)
        {
            return ToString(value, startIndex, value.Length - startIndex);
        }
        #endregion

        #region Convert to Bytes

        // Converts a byte into an array of bytes with length one.
        /// <summary>
        /// Gets the bytes.
        /// </summary>
        /// <param name="value">if set to <c>true</c> [value].</param>
        /// <returns>System.Byte[].</returns>
        internal static byte[] GetBytes(bool value)
        {
            var r = new byte[1];
            r[0] = value ? (byte) 255 : (byte) 0;
            return r;
        }

        // Converts a char into an array of bytes with length two.
        /// <summary>
        /// Gets the bytes.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>System.Byte[].</returns>
        internal static byte[] GetBytes(char value)
        {
            return GetBytes((short) value);
        }

        // Converts a short into an array of bytes with length
        // two.
        /// <summary>
        /// Gets the bytes.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>System.Byte[].</returns>
        internal static byte[] GetBytes(short value)
        {
            var bytes = new byte[2];
            fixed (byte* b = bytes)
            {
                *(short*) b = value;
            }
            return bytes;
        }

        // Converts an int into an array of bytes with length 
        // four.
        /// <summary>
        /// Gets the bytes.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>System.Byte[].</returns>
        internal static byte[] GetBytes(int value)
        {
            var bytes = new byte[4];
            fixed (byte* b = bytes)
            {
                *(int*) b = value;
            }
            return bytes;
        }

        // Converts a long into an array of bytes with length 
        // eight.
        /// <summary>
        /// Gets the bytes.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>System.Byte[].</returns>
        internal static byte[] GetBytes(long value)
        {
            var bytes = new byte[8];
            fixed (byte* b = bytes)
            {
                *(long*) b = value;
            }
            return bytes;
        }

        // Converts an ushort into an array of bytes with
        // length two.
        /// <summary>
        /// Gets the bytes.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>System.Byte[].</returns>
        internal static byte[] GetBytes(ushort value)
        {
            return GetBytes((short) value);
        }

        // Converts an uint into an array of bytes with
        // length four.
        /// <summary>
        /// Gets the bytes.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>System.Byte[].</returns>
        internal static byte[] GetBytes(uint value)
        {
            return GetBytes((int) value);
        }

        // Converts an unsigned long into an array of bytes with
        // length eight.
        /// <summary>
        /// Gets the bytes.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>System.Byte[].</returns>
        internal static byte[] GetBytes(ulong value)
        {
            return GetBytes((long) value);
        }

        // Converts a float into an array of bytes with length 
        // four.
        /// <summary>
        /// Gets the bytes.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>System.Byte[].</returns>
        internal static byte[] GetBytes(float value)
        {
            return GetBytes(*(int*) &value);
        }

        // Converts a double into an array of bytes with length 
        // eight.
        /// <summary>
        /// Gets the bytes.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>System.Byte[].</returns>
        internal static byte[] GetBytes(double value)
        {
            return GetBytes(*(long*) &value);
        }

        #endregion

        #region convert from bytes to common types
        /* ================Type===================================|===Size===
         * bool, byte (there is no 8-bit char in C#)              |  1 byte
         * int16, short, unsigned short, char                     |  2 bytes
         * float, int32, int, unsigned int, long, unsigned long   |  4 bytes
         * double, int64, long                                    |  8 bytes   */

        #region Covert from 1 byte (8-bit)

        /// <summary>
        /// To the boolean.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="startIndex">The start index.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        internal static bool ToBoolean(byte[] value, int startIndex)
        {
            return value[startIndex] != 0;
        }
        #endregion
        
        #region Covert from 2 bytes (16-bit)

        // Converts an array of bytes into a char.  
        /// <summary>
        /// To the character.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="startIndex">The start index.</param>
        /// <param name="bigEndian">if set to <c>true</c> [big endian].</param>
        /// <returns>System.Char.</returns>
        internal static char ToChar(byte[] value, int startIndex, bool bigEndian = false)
        {
            return (char) ToInt16(value, startIndex, bigEndian);
        }

        // Converts an array of bytes into an ushort.
        // 
        /// <summary>
        /// To the u int16.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="startIndex">The start index.</param>
        /// <param name="bigEndian">if set to <c>true</c> [big endian].</param>
        /// <returns>System.UInt16.</returns>
        internal static ushort ToUInt16(byte[] value, int startIndex, bool bigEndian = false)
        {
            return (ushort) ToInt16(value, startIndex, bigEndian);
        }

        // Converts an array of bytes into a short.  
        /// <summary>
        /// To the int16.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="startIndex">The start index.</param>
        /// <param name="bigEndian">if set to <c>true</c> [big endian].</param>
        /// <returns>System.Int16.</returns>
        internal static short ToInt16(byte[] value, int startIndex, bool bigEndian = false)
        {
            if (bigEndian)
                fixed (byte* pbyte = &value[startIndex])
                {
                    if (startIndex%2 == 0)
                        return *(short*) pbyte;
                    return (short) ((*pbyte << 8) | *(pbyte + 1));
                }
            fixed (byte* pbyte = &value[startIndex])
            {
                if (startIndex%2 == 0)
                    return *(short*) pbyte;
                return (short) (*pbyte | (*(pbyte + 1) << 8));
            }
        }

        #endregion

        #region Covert from 4 bytes (32-bit)

        // Converts an array of bytes into an uint.
        /// <summary>
        /// To the u int32.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="startIndex">The start index.</param>
        /// <param name="bigEndian">if set to <c>true</c> [big endian].</param>
        /// <returns>System.UInt32.</returns>
        internal static uint ToUInt32(byte[] value, int startIndex, bool bigEndian = false)
        {
            return (uint) ToInt32(value, startIndex, bigEndian);
        }

        // Converts an array of bytes into a float.  
        /// <summary>
        /// To the single.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="startIndex">The start index.</param>
        /// <param name="bigEndian">if set to <c>true</c> [big endian].</param>
        /// <returns>System.Single.</returns>
        internal static float ToSingle(byte[] value, int startIndex, bool bigEndian = false)
        {
            var val = ToInt32(value, startIndex, bigEndian);
            return *(float*) &val;
        }

        // Converts an array of bytes into an int.  
        /// <summary>
        /// To the int32.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="startIndex">The start index.</param>
        /// <param name="bigEndian">if set to <c>true</c> [big endian].</param>
        /// <returns>System.Int32.</returns>
        internal static int ToInt32(byte[] value, int startIndex, bool bigEndian = false)
        {
            if (bigEndian)
                fixed (byte* pbyte = &value[startIndex])
                {
                    if (startIndex%4 == 0)
                        return *(int*) pbyte;
                    return (*pbyte << 24) | (*(pbyte + 1) << 16) | (*(pbyte + 2) << 8) | *(pbyte + 3);
                }
            fixed (byte* pbyte = &value[startIndex])
            {
                if (startIndex%4 == 0)
                    return *(int*) pbyte;
                return *pbyte | (*(pbyte + 1) << 8) | (*(pbyte + 2) << 16) | (*(pbyte + 3) << 24);
            }
        }

        #endregion

        #region Convert from 8 bytes (64-bit)

        // Converts an array of bytes into an unsigned long.
        /// <summary>
        /// To the u int64.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="startIndex">The start index.</param>
        /// <param name="bigEndian">if set to <c>true</c> [big endian].</param>
        /// <returns>System.UInt64.</returns>
        internal static ulong ToUInt64(byte[] value, int startIndex, bool bigEndian = false)
        {
            return (ulong) ToInt64(value, startIndex, bigEndian);
        }

        // Converts an array of bytes into a double.  
        /// <summary>
        /// To the double.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="startIndex">The start index.</param>
        /// <param name="bigEndian">if set to <c>true</c> [big endian].</param>
        /// <returns>System.Double.</returns>
        internal static double ToDouble(byte[] value, int startIndex, bool bigEndian = false)
        {
            var val = ToInt64(value, startIndex, bigEndian);
            return *(double*) &val;
        }

        // Converts an array of bytes into a long.  
        /// <summary>
        /// To the int64.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="startIndex">The start index.</param>
        /// <param name="bigEndian">if set to <c>true</c> [big endian].</param>
        /// <returns>System.Int64.</returns>
        internal static long ToInt64(byte[] value, int startIndex, bool bigEndian = false)
        {
            if (bigEndian)
                fixed (byte* pbyte = &value[startIndex])
                {
                    if (startIndex%8 == 0)
                        return *(long*) pbyte;
                    var i1 = (*pbyte << 24) | (*(pbyte + 1) << 16) | (*(pbyte + 2) << 8) | *(pbyte + 3);
                    var i2 = (*(pbyte + 4) << 24) | (*(pbyte + 5) << 16) | (*(pbyte + 6) << 8) | *(pbyte + 7);
                    return (uint) i2 | ((long) i1 << 32);
                }
            fixed (byte* pbyte = &value[startIndex])
            {
                if (startIndex%8 == 0)
                    return *(long*) pbyte;
                var i1 = *pbyte | (*(pbyte + 1) << 8) | (*(pbyte + 2) << 16) | (*(pbyte + 3) << 24);
                var i2 = *(pbyte + 4) | (*(pbyte + 5) << 8) | (*(pbyte + 6) << 16) | (*(pbyte + 7) << 24);
                return (uint) i1 | ((long) i2 << 32);
            }
        }

        #endregion
        #endregion
    }
}