using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using JiksLib.Collections;
using JiksLib.Control;
using JiksLib.Extensions;
using JiksLib.IO;
using JiksLib.Text;

namespace JiksLib.Test
{
    /// <summary>
    /// 本轮修复的回归用例：每个用例对应评审报告中的一个具体缺陷，
    /// 修复前应当失败，修复后应持续通过。
    /// </summary>
    [TestFixture]
    public class RegressionTests
    {
        #region Deque（P1-1 / P1-2 / P2-17）

        [Test]
        public void Deque_ToArray_AfterClear_ReturnsEmpty()
        {
            var deque = new Deque<int>();
            deque.Add(1);
            deque.Add(2);
            deque.Clear();

            Assert.That(deque.ToArray(), Is.Empty);
        }

        [Test]
        public void Deque_ToArray_AfterRemovingAllFromFront_ReturnsEmpty()
        {
            var deque = new Deque<int>();
            deque.Add(1);
            deque.TryRemoveFront(out _);

            Assert.That(deque.ToArray(), Is.Empty);
        }

        [Test]
        public void Deque_AddRange_EmptySequence_OnEmptyDeque_DoesNotThrow()
        {
            var deque = new Deque<int>();

            Assert.DoesNotThrow(() => deque.AddRange(Array.Empty<int>()));
            Assert.That(deque.Count, Is.EqualTo(0));
        }

        [Test]
        public void Deque_AddRange_SingleUseEnumerable_AddsAllElements()
        {
            var deque = new Deque<int>();
            deque.AddRange(new SingleUseEnumerable(new[] { 1, 2, 3 }));

            Assert.That(deque.ToArray(), Is.EqualTo(new[] { 1, 2, 3 }));
        }

        #endregion

        #region IntRange（P1-3 / P1-4 / P1-5）

        [Test]
        public void IntRange_SinglePointIncludingMax_CountsAndEnumeratesOnce()
        {
            var range = new IntRange(5, false, 5, true);

            Assert.That(range.Count, Is.EqualTo(1));
            Assert.That(range.Contains(5), Is.True);
            Assert.That(range, Is.EqualTo(new[] { 5 }));
        }

        [Test]
        public void IntRange_SinglePointIncludingNeither_IsEmpty()
        {
            var range = new IntRange(5, false, 5, false);

            Assert.That(range.Count, Is.EqualTo(0));
            Assert.That(range, Is.Empty);
        }

        [Test]
        public void IntRange_EnumeratorAtIntMaxValue_Terminates()
        {
            var range = new IntRange(int.MaxValue - 1, true, int.MaxValue, true);

            // 修复前这里会自增溢出并回绕到 int.MinValue，实际等同于无限枚举
            Assert.That(range, Is.EqualTo(new[] { int.MaxValue - 1, int.MaxValue }));
        }

        [Test]
        public void IntRange_EmptyRangeAtIntMaxValue_IsEmpty()
        {
            var range = new IntRange(int.MaxValue, false, int.MaxValue, false);

            Assert.That(range, Is.Empty);
        }

        [Test]
        public void IntRange_Count_Overflow_Throws()
        {
            var range = new IntRange(int.MinValue, true, int.MaxValue, true);

            Assert.That(() => _ = range.Count, Throws.TypeOf<OverflowException>());
        }

        [Test]
        public void IntRange_ReversedBounds_ThrowsWithParamName()
        {
            var ex = Assert.Throws<ArgumentException>(() => new IntRange(5, true, 1, true));
            Assert.That(ex!.ParamName, Is.EqualTo("min"));
        }

        #endregion

        #region Cache（P1-6 / P1-7）

        [Test]
        public void AsyncCache_DuplicateReceiveForSameKey_DoesNotCorruptEviction()
        {
            // 异步工厂对同一个 key 回调两次，触发两次 Put
            var cache = Cache.MakeAsync<int, string>(
                Cache.Algorithm.LRU,
                (key, onReceive, onError) =>
                {
                    onReceive("v" + key);
                    onReceive("v2" + key);
                },
                2);

            cache.Access(1, _ => { }, e => throw e);
            cache.Access(2, _ => { }, e => throw e);

            // 修复前重复 Put 会留下悬挂节点，后续淘汰时抛 KeyNotFoundException
            Assert.DoesNotThrow(() => cache.Access(3, _ => { }, e => throw e));
            Assert.That(cache.Count, Is.EqualTo(2));
        }

        #endregion

        #region IO（P1-17 / P2-28）

        [Test]
        public void EndianBinaryReader_ReadBytesExactly_HandlesPartialReads()
        {
            var stream = new TrickleStream(new byte[] { 1, 2, 3, 4 });
            using var reader = new EndianBinaryReader(
                EndianBitConverter.LittleEndian, stream);

            Assert.That(reader.ReadInt32(), Is.EqualTo(0x04030201));
        }

        [Test]
        public void EndianBinaryReader_Close_RespectsLeaveOpen()
        {
            var stream = new MemoryStream();
            var reader = new EndianBinaryReader(
                EndianBitConverter.LittleEndian, stream, leaveOpen: true);

            reader.Close();
            reader.Dispose();

            Assert.That(stream.CanRead, Is.True);

            var owned = new MemoryStream();
            var ownedReader = new EndianBinaryReader(
                EndianBitConverter.LittleEndian, owned, leaveOpen: false);
            ownedReader.Close();

            Assert.That(owned.CanRead, Is.False);
        }

        #endregion

        #region 扩展与集合（P1-12 / P2-9）

        [Test]
        public void StripPrefixSuffix_OverlappingAffixes_ReturnsFalse()
        {
            Assert.That("abc".StripPrefixSuffix("abc", "c", out var remain), Is.False);
            Assert.That(remain, Is.Empty);
        }

        [Test]
        public void MultiDictionary_AddNullValue_DoesNotLeaveEmptyKey()
        {
            var dict = new MultiDictionary<string, string>();

            Assert.That(() => dict.Add("key", null!), Throws.ArgumentNullException);
            Assert.That(dict.ContainsKey("key"), Is.False);
            Assert.That(dict.Count, Is.EqualTo(0));
        }

        #endregion

        #region 辅助类型

        /// <summary>
        /// 只能枚举一次的序列：第二次请求枚举器直接抛异常。
        /// </summary>
        private sealed class SingleUseEnumerable : IEnumerable<int>
        {
            readonly int[] items;
            bool consumed;

            public SingleUseEnumerable(int[] items)
            {
                this.items = items;
            }

            public IEnumerator<int> GetEnumerator()
            {
                if (consumed)
                    throw new InvalidOperationException("已枚举过，不能再次枚举。");

                consumed = true;
                return ((IEnumerable<int>)items).GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        /// <summary>
        /// 每次 Read 最多返回 1 个字节，用于验证 ReadBytesExactly 会循环读满。
        /// </summary>
        private sealed class TrickleStream : Stream
        {
            readonly byte[] data;
            int position;

            public TrickleStream(byte[] data)
            {
                this.data = data;
            }

            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => data.Length;

            public override long Position
            {
                get => position;
                set => throw new NotSupportedException();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                if (position >= data.Length)
                    return 0;

                buffer[offset] = data[position++];
                return 1;
            }

            public override void Flush() { }

            public override long Seek(long offset, SeekOrigin origin) =>
                throw new NotSupportedException();

            public override void SetLength(long value) =>
                throw new NotSupportedException();

            public override void Write(byte[] buffer, int offset, int count) =>
                throw new NotSupportedException();
        }

        #endregion
    }
}
