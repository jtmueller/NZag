using NZag.Utilities;
using Xunit;

namespace NZag.Core.Tests
{
    public class BitSetTests
    {
        private IBitSet CreateBitSet(int length)
        {
            var bs = BitSet.Create(length);

            Assert.NotNull(bs);

            if (bs.Length <= 32)
                Assert.Equal("NZag.Utilities.BitSet+BitSet32", bs.GetType().FullName);
            else if (bs.Length <= 64)
                Assert.Equal("NZag.Utilities.BitSet+BitSet64", bs.GetType().FullName);
            else
                Assert.Equal("NZag.Utilities.BitSet+BitSetN", bs.GetType().FullName);

            return bs;
        }

        private void SimpleTests(IBitSet bitSet)
        {
            // Verify that IBitSet is initially cleared
            for (int i = 0; i < bitSet.Length; i++)
                Assert.False(bitSet[i]);

            // Add each bit
            for (int i = 0; i < bitSet.Length; i++)
                bitSet.Add(i);
            for (int i = 0; i < bitSet.Length; i++)
            {
                Assert.True(bitSet.Contains(i));
                Assert.True(bitSet[i]);
            }

            // Remove each bit
            for (int i = 0; i < bitSet.Length; i++)
                bitSet.Remove(i);
            for (int m = 0; m < bitSet.Length; m++)
            {
                Assert.False(bitSet.Contains(m));
                Assert.False(bitSet[m]);
            }

            // Set each bit
            for (int i = 0; i < bitSet.Length; i++)
                bitSet[i] = true;
            for (int i = 0; i < bitSet.Length; i++)
            {
                Assert.True(bitSet.Contains(i));
                Assert.True(bitSet[i]);
            }

            // Clear each bit
            for (int i = 0; i < bitSet.Length; i++)
                bitSet[i] = false;
            for (int i = 0; i < bitSet.Length; i++)
            {
                Assert.False(bitSet.Contains(i));
                Assert.False(bitSet[i]);
            }

            // Add every other bit
            for (int i = 0; i < bitSet.Length; i += 2)
                bitSet.Add(i);
            for (int i = 0; i < bitSet.Length; i += 2)
            {
                Assert.True(bitSet.Contains(i));
                Assert.True(bitSet[i]);
                Assert.False(bitSet.Contains(i + 1));
                Assert.False(bitSet[i + 1]);
            }

            // clear
            bitSet.Clear();

            for (int i = 0; i < bitSet.Length; i++)
            {
                Assert.False(bitSet.Contains(i));
                Assert.False(bitSet[i]);
            }
        }

        private void UnionWithTests(IBitSet bitSet1, IBitSet bitSet2)
        {
            int len = bitSet1.Length;
            int mid = len / 2;

            bitSet1.Clear();
            bitSet2.Clear();

            for (int i = 0; i < mid; i++)
                bitSet1[i] = true;
            for (int i = mid; i < len; i++)
                bitSet2[i] = true;

            bitSet1.UnionWith(bitSet2);

            for (int i = 0; i < len; i++)
            {
                Assert.True(bitSet1.Contains(i));
                Assert.True(bitSet1[i]);
            }

            bitSet1.Clear();
            bitSet2.Clear();

            for (int i = 0; i < len; i += 4)
                bitSet1[i] = true;
            for (int i = 0; i < len; i += 2)
                bitSet2[i] = true;

            bitSet1.UnionWith(bitSet2);

            for (int i = 0; i < len; i += 2)
            {
                Assert.True(bitSet1.Contains(i));
                Assert.True(bitSet1[i]);
                Assert.False(bitSet1.Contains(i + 1));
                Assert.False(bitSet1[i + 1]);
            }
        }

        private void RemoveWhereTests(IBitSet bitSet)
        {
            bitSet.Clear();

            for (int i = 0; i < bitSet.Length; i++)
                bitSet[i] = true;

            bitSet.RemoveWhere(i => i % 2 == 0);

            for (int i = 0; i < bitSet.Length; i++)
            {
                if (i % 2 == 0)
                    Assert.False(bitSet[i]);
                else
                    Assert.True(bitSet[i]);
            }
        }

        private void EqualsTests(IBitSet bitSet1, IBitSet bitSet2)
        {
            int len = bitSet1.Length;
            int mid = len / 2;

            bitSet1.Clear();
            bitSet2.Clear();

            Assert.True(bitSet1.Equals(bitSet2));
            Assert.True(bitSet2.Equals(bitSet1));

            for (int i = 0; i < mid; i++)
                bitSet1[i] = true;
            for (int i = mid; i < len; i++)
                bitSet2[i] = true;

            Assert.False(bitSet1.Equals(bitSet2));
            Assert.False(bitSet2.Equals(bitSet1));

            bitSet1.Clear();
            bitSet2.Clear();

            for (int i = 0; i < len; i++)
            {
                bitSet1[i] = true;
                bitSet2[i] = true;
            }
            Assert.True(bitSet1.Equals(bitSet2));
            Assert.True(bitSet2.Equals(bitSet1));
        }

        [Fact]
        public void Test32Bits() => SimpleTests(CreateBitSet(32));

        [Fact]
        public void Test64Bits() => SimpleTests(CreateBitSet(64));

        [Fact]
        public void Test256Bits() => SimpleTests(CreateBitSet(256));

        [Fact]
        public void TestUnionWith32() => UnionWithTests(CreateBitSet(32), CreateBitSet(32));

        [Fact]
        public void TestUnionWith64() => UnionWithTests(CreateBitSet(64), CreateBitSet(64));

        [Fact]
        public void TestUnionWith256() => UnionWithTests(CreateBitSet(256), CreateBitSet(256));

        [Fact]
        public void TestRemoveWhere32() => RemoveWhereTests(CreateBitSet(32));

        [Fact]
        public void TestRemoveWhere64() => RemoveWhereTests(CreateBitSet(64));

        [Fact]
        public void TestRemoveWhere256() => RemoveWhereTests(CreateBitSet(256));

        [Fact]
        public void TestEquals32() => EqualsTests(CreateBitSet(32), CreateBitSet(32));

        [Fact]
        public void TestEquals64() => EqualsTests(CreateBitSet(64), CreateBitSet(64));

        [Fact]
        public void TestEquals256() => EqualsTests(CreateBitSet(256), CreateBitSet(256));
    }
}
