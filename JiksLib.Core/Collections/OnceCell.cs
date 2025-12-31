#nullable enable

using System;

namespace JiksLib.Collections
{
    /// <summary>
    /// 可以被设置一次的格子
    /// </summary>
    public sealed class OnceCell<T>
    {
        /// <summary>
        /// 该格子是否已经被设置
        /// </summary>
        public bool HasSet { get; private set; }

        /// <summary>
        /// 设置格子的值时触发该事件
        /// </summary>
        public event Action<T>? OnSet;

        /// <summary>
        /// 格子的值，可以在设置一次后被任意次读取
        /// </summary>
        public T Value
        {
            get
            {
                if (HasSet) return v!;
                throw new InvalidOperationException("Value not set.");
            }

            set
            {
                if (HasSet)
                    throw new InvalidOperationException("Value already set.");

                HasSet = true;
                v = value;
                OnSet?.Invoke(v);
            }
        }

        T? v;
    }
}
