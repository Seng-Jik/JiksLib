using System;

namespace JiksLib.Collections
{
    /// <summary>
    /// 可以被设置多次的格子
    /// 可用于强类型装箱值类型或者提供修改时事件
    /// </summary>
    /// <remarks>
    /// <typeparamref name="T"/> 不受约束，允许传入 null（引用类型）或值类型；
    /// 需要"非空"语义时请由调用方自行校验。每次设置都会在写入后触发 <see cref="OnSet"/>。
    /// </remarks>
    public sealed class Cell<T>
    {
        /// <summary>
        /// 设置格子的值时触发该事件
        /// </summary>
        public event Action<T>? OnSet;

        /// <summary>
        /// 格子的值
        /// </summary>
        public T Value
        {
            get
            {
                return v;
            }

            set
            {
                v = value;
                OnSet?.Invoke(v);
            }
        }

        public Cell(T value)
        {
            v = value;
        }

        T v;
    }
}
