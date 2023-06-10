using Stride.Core.Serialization;
using System;
using System.IO;
using System.Text;
using Xunit;

namespace StrideSource
{
    public class BinaryWriterTests
    {
        #region About String
        [Fact]
        public void WritesString()
        {
            using (var ms = new MemoryStream())
            {
                BinarySerializationWriter writer = new BinarySerializationWriter(ms);
                var temp = "Hello, world!&§\"%!?)(€@";
                writer.Serialize(ref temp);
                string result = Encoding.UTF8.GetString(ms.ToArray());
                Assert.Equal(result, temp);
            }
        }
        [Fact]
        public void WritesEmptyString()
        {
            using (var ms = new MemoryStream())
            {
                BinarySerializationWriter writer = new BinarySerializationWriter(ms);
                string temp = "";
                writer.Serialize(ref temp);
                string result = Encoding.UTF8.GetString(ms.ToArray());
                Assert.Equal(result, temp);
            }
        }
        #endregion
        #region About Byte
        [Fact]
        public void WritesByte()
        {
            using (var ms = new MemoryStream())
            {
                BinarySerializationWriter writer = new BinarySerializationWriter(ms);
                byte temp = 7;
                writer.Serialize(ref temp);
                byte result = ms.ToArray()[0];
                Assert.Equal(ms.ToArray().Length, 1);
                Assert.Equal(temp, result);
            }
        }
        [Fact]
        public void WritesSByte()
        {
            // positive sbyte
            using (var ms = new MemoryStream())
            {
                BinarySerializationWriter writer = new BinarySerializationWriter(ms);
                sbyte temp = 7;
                writer.Serialize(ref temp);
                ms.Seek(0, SeekOrigin.Begin);
                sbyte result = (sbyte)ms.ReadByte();
                Assert.Equal(temp, result);
            }
            // negative sbyte
            using (var ms = new MemoryStream())
            {
                BinarySerializationWriter writer = new BinarySerializationWriter(ms);
                sbyte temp = -7;
                writer.Serialize(ref temp);
                ms.Seek(0, SeekOrigin.Begin);
                sbyte result = (sbyte)ms.ReadByte();
                Assert.Equal(temp, result);
            }
        }
        #endregion
        #region About ints
        [Fact]
        public void WritesInt()
        {
            using (var ms = new MemoryStream())
            {
                BinarySerializationWriter writer = new BinarySerializationWriter(ms);
                int temp = 123912;
                writer.Serialize(ref temp);
                int result = BitConverter.ToInt32(ms.ToArray());
                Assert.Equal(result, temp);
            }
        }
        [Fact]
        public void WritesUint()
        {
            using (var ms = new MemoryStream())
            {
                BinarySerializationWriter writer = new BinarySerializationWriter(ms);
                uint temp = 75649848;
                writer.Serialize(ref temp);
                ms.Seek(0, SeekOrigin.Begin);
                uint result = BitConverter.ToUInt32(ms.ToArray());
                Assert.Equal(temp, result);
            }
        }
        #endregion
        #region About short
        [Fact]
        public void WritesShort()
        {
            // positive short
            using (var ms = new MemoryStream())
            {
                BinarySerializationWriter writer = new BinarySerializationWriter(ms);
                short temp = 1235;
                writer.Serialize(ref temp);
                short result = BitConverter.ToInt16(ms.ToArray());
                Assert.Equal(result, temp);
            }
            // negative short
            using (var ms = new MemoryStream())
            {
                BinarySerializationWriter writer = new BinarySerializationWriter(ms);
                short temp = -1235;
                writer.Serialize(ref temp);
                short result = BitConverter.ToInt16(ms.ToArray());
                Assert.Equal(result, temp);
            }
        }
        [Fact]
        public void WritesUShort()
        {
            using (var ms = new MemoryStream())
            {
                BinarySerializationWriter writer = new BinarySerializationWriter(ms);
                ushort temp = 64687;
                writer.Serialize(ref temp);
                ms.Seek(0, SeekOrigin.Begin);
                ushort result = BitConverter.ToUInt16(ms.ToArray());
                Assert.Equal(temp, result);
            }
        }

        #endregion
        #region About Long
        [Fact]
        public void WritesLong()
        {
            // positive long
            using (var ms = new MemoryStream())
            {
                BinarySerializationWriter writer = new BinarySerializationWriter(ms);
                long temp = 5618568456;
                writer.Serialize(ref temp);
                long result = BitConverter.ToInt64(ms.ToArray());
                Assert.Equal(result, temp);
            }
            // negative long
            using (var ms = new MemoryStream())
            {
                BinarySerializationWriter writer = new BinarySerializationWriter(ms);
                long temp = -5618568456;
                writer.Serialize(ref temp);
                long result = BitConverter.ToInt64(ms.ToArray());
                Assert.Equal(result, temp);
            }
        }
        [Fact]
        public void WritesULong()
        {
            using (var ms = new MemoryStream())
            {
                BinarySerializationWriter writer = new BinarySerializationWriter(ms);
                ulong temp = 6468754634534568;
                writer.Serialize(ref temp);
                ms.Seek(0, SeekOrigin.Begin);
                ulong result = BitConverter.ToUInt64(ms.ToArray());
                Assert.Equal(temp, result);
            }
        }
        #endregion
        #region About float
        [Fact]
        public void WritesFloat()
        {
            // positive long
            using (var ms = new MemoryStream())
            {
                BinarySerializationWriter writer = new BinarySerializationWriter(ms);
                float temp = 3574664.3151f;
                writer.Serialize(ref temp);
                float result = BitConverter.ToSingle(ms.ToArray());
                Assert.Equal(result, temp);
            }
            // negative long
            using (var ms = new MemoryStream())
            {
                BinarySerializationWriter writer = new BinarySerializationWriter(ms);
                float temp = -5862435.1235f;
                writer.Serialize(ref temp);
                float result = BitConverter.ToSingle(ms.ToArray());
                Assert.Equal(result, temp);
            }
        }
        #endregion
        #region About Double
        [Fact]
        public void WritesDouble()
        {
            // positive long
            using (var ms = new MemoryStream())
            {
                BinarySerializationWriter writer = new BinarySerializationWriter(ms);
                double temp = 3574675374364.3151f;
                writer.Serialize(ref temp);
                double result = BitConverter.ToDouble(ms.ToArray());
                Assert.Equal(result, temp);
            }
            // negative long
            using (var ms = new MemoryStream())
            {
                BinarySerializationWriter writer = new BinarySerializationWriter(ms);
                double temp = -5867373882435.1235f;
                writer.Serialize(ref temp);
                double result = BitConverter.ToDouble(ms.ToArray());
                Assert.Equal(result, temp);
            }
        }
        #endregion
    }
}
