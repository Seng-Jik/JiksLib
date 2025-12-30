using System;

namespace JiksLib.Collections
{
    /// <summary>
    /// 可以被设置多次的非空引用类型格子
    /// 可用于强类型装箱值类型或者提供修改时事件
    /// </summary>
    public sealed class Cell<T>
        where T : notnull
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
                return v!;
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
