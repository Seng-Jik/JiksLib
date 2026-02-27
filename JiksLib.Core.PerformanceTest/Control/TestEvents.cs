using System;
using JiksLib.Control;

namespace JiksLib.PerformanceTest.Control
{
    /// <summary>
    /// 测试事件类（引用类型）
    /// </summary>
    public class TestEvent
    {
        public int Value { get; set; }
    }

    /// <summary>
    /// 测试事件派生类
    /// </summary>
    public class DerivedTestEvent : TestEvent
    {
        public string Message { get; set; } = string.Empty;
    }

    public interface IValueEvent { }

    /// <summary>
    /// 测试值类型事件
    /// </summary>
    public struct TestValueEvent : IValueEvent
    {
        public int Value { get; set; }
    }
}