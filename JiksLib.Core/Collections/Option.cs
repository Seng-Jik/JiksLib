using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace JiksLib.Collections
{
    /// <summary>
    /// 表示一个可能包含值也可能不包含值的容器类型。
    /// </summary>
    /// <typeparam name="T">值的类型。</typeparam>
    /// <remarks>
    /// <para>
    /// <see cref="Option{T}"/> 是一个只读结构体，用于表示一个可能包含值也可能不包含值的容器。
    /// 它类似于可为空的值类型，但适用于引用类型（其中 <typeparamref name="T"/> 必须是非空类型）。
    /// </para>
    /// <para>
    /// 当可选类型有值时，可以访问该值；当没有值时，可以执行备用操作。
    /// </para>
    /// </remarks>
    public readonly struct Option<T> : IReadOnlyCollection<T>, IEquatable<Option<T>>
        where T : notnull
    {
        /// <summary>
        /// 使用指定的值初始化 <see cref="Option{T}"/> 结构的新实例。
        /// </summary>
        /// <param name="value">要包装在可选类型中的值。如果 <paramref name="value"/> 为 <see langword="null"/>，
        /// 则可选类型将没有值。</param>
        /// <remarks>
        /// 如果 <paramref name="value"/> 不为 <see langword="null"/>，此构造函数将创建一个有值的可选类型；
        /// 否则，它将创建一个没有值的可选类型。
        /// </remarks>
        public Option(T value)
        {
            if (value is null)
            {
                HasValue = false;
                this.value = default;
            }
            else
            {
                HasValue = true;
                this.value = value;
            }
        }

        /// <summary>
        /// 获取一个值，指示此可选类型是否包含值。
        /// </summary>
        /// <value>
        /// 如果此可选类型有值，则为 <see langword="true"/>；否则为 <see langword="false"/>。
        /// </value>
        public readonly bool HasValue;


        /// <summary>
        /// 获取此可选类型的值。
        /// </summary>
        /// <value>此可选类型中包含的值。</value>
        /// <exception cref="InvalidOperationException">当此可选类型没有值时引发。</exception>
        public T Value
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (!HasValue)
                    throw new InvalidOperationException("Value is null.");

                return value!;
            }
        }

        /// <summary>
        /// 将可选类型中的值映射为新类型的可选类型。
        /// </summary>
        /// <typeparam name="U">新值的类型。</typeparam>
        /// <param name="mapper">用于转换值的函数。</param>
        /// <returns>
        /// 如果此可选类型有值，则返回包含映射结果的新可选类型；
        /// 否则返回没有值的可选类型。
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Option<U> Map<U>(Func<T, U> mapper)
            where U : notnull
        {
            if (!HasValue) return new();
            else return new(mapper(value!));
        }

        /// <summary>
        /// 将可选类型中的值映射为另一个可选类型。
        /// </summary>
        /// <typeparam name="U">新值的类型。</typeparam>
        /// <param name="mapper">返回可选类型的函数。</param>
        /// <returns>
        /// 如果此可选类型有值，则返回 <paramref name="mapper"/> 的结果；
        /// 否则返回没有值的可选类型。
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Option<U> FlatMap<U>(Func<T, Option<U>> mapper)
            where U : notnull
        {
            if (!HasValue) return new();
            else return mapper(value!);
        }

        /// <summary>
        /// 获取可选类型的值，如果没有值则返回默认值。
        /// </summary>
        /// <returns>
        /// 如果此可选类型有值，则返回该值；否则返回 <typeparamref name="T"/> 的默认值。
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T? GetOrDefault() =>
            HasValue ? value! : default;

        /// <summary>
        /// 获取可选类型的值，如果没有值则返回指定的默认值。
        /// </summary>
        /// <param name="defaultValue">没有值时要返回的默认值。</param>
        /// <returns>
        /// 如果此可选类型有值，则返回该值；否则返回 <paramref name="defaultValue"/>。
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetOrDefault(T defaultValue) =>
            HasValue ? value! : defaultValue;

        /// <summary>
        /// 获取可选类型的值，如果没有值则通过函数获取默认值。
        /// </summary>
        /// <param name="thunk">没有值时要调用的函数，用于生成默认值。</param>
        /// <returns>
        /// 如果此可选类型有值，则返回该值；否则返回 <paramref name="thunk"/> 的结果。
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetOrDefault(Func<T> thunk) =>
            HasValue ? value! : thunk();

        /// <summary>
        /// 返回此可选类型，如果此可选类型没有值，则返回指定的可选类型。
        /// </summary>
        /// <param name="option">此可选类型没有值时要返回的可选类型。</param>
        /// <returns>
        /// 如果此可选类型有值，则返回此可选类型；否则返回 <paramref name="option"/>。
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Option<T> OrElse(Option<T> option)
        {
            if (HasValue) return this;
            else return option;
        }

        /// <summary>
        /// 返回此可选类型，如果此可选类型没有值，则通过函数获取要返回的可选类型。
        /// </summary>
        /// <param name="thunk">此可选类型没有值时要调用的函数，用于生成要返回的可选类型。</param>
        /// <returns>
        /// 如果此可选类型有值，则返回此可选类型；否则返回 <paramref name="thunk"/> 的结果。
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Option<T> OrElse(Func<Option<T>> thunk)
        {
            if (HasValue) return this;
            else return thunk();
        }

        /// <summary>
        /// 将此可选类型与另一个可选类型组合为一个元组可选类型。
        /// </summary>
        /// <typeparam name="U">另一个可选类型的值的类型。</typeparam>
        /// <param name="option">要与此可选类型组合的另一个可选类型。</param>
        /// <returns>
        /// 如果两个可选类型都有值，则返回包含两个值的元组的可选类型；
        /// 否则返回没有值的可选类型。
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Option<(T, U)> Zip<U>(Option<U> option)
            where U : notnull
        {
            if (HasValue && option.HasValue)
                return new((value!, option.value!));
            else
                return new();
        }

        /// <summary>
        /// 将可选类型解构为值是否存在和值本身。
        /// </summary>
        /// <param name="hasValue">如果可选类型有值，则为 <see langword="true"/>；否则为 <see langword="false"/>。</param>
        /// <param name="value">可选类型中的值。如果 <paramref name="hasValue"/> 为 <see langword="false"/>，则为 <typeparamref name="T"/> 的默认值。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Deconstruct(out bool hasValue, out T? value)
        {
            hasValue = HasValue;
            value = hasValue ? this.value! : default;
        }

        /// <summary>
        /// 基于谓词过滤可选类型的值。
        /// </summary>
        /// <param name="predicate">用于测试值的函数。</param>
        /// <returns>
        /// 如果此可选类型有值且 <paramref name="predicate"/> 返回 <see langword="true"/>，则返回此可选类型；
        /// 否则返回没有值的可选类型。
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Option<T> Where(Func<T, bool> predicate)
        {
            if (!HasValue) return new();
            if (!predicate(value!)) return new();
            return this;
        }

        /// <summary>
        /// 确定此可选类型的值是否等于另一个可选类型的值。
        /// </summary>
        /// <param name="other">要与此实例比较的可选类型。</param>
        /// <returns>
        /// 如果当前可选类型等于 <paramref name="other"/> 参数，则为 <see langword="true"/>；
        /// 否则为 <see langword="false"/>。
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Option<T> other)
        {
            if (!HasValue && !other.HasValue) return true;
            if (HasValue && other.HasValue) return EqualityComparer<T>.Default.Equals(value!, other.value!);
            return false;
        }

        /// <summary>
        /// 确定此可选类型的值是否等于指定的对象。
        /// </summary>
        /// <param name="obj">要与此实例比较的对象。</param>
        /// <returns>
        /// 如果 <paramref name="obj"/> 是等于此实例的 <see cref="Option{T}"/>，则为 <see langword="true"/>；
        /// 否则为 <see langword="false"/>。
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object? obj) => obj is Option<T> other && Equals(other);

        /// <summary>
        /// 返回此可选类型的哈希代码。
        /// </summary>
        /// <returns>当前可选类型的哈希代码。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            if (!HasValue) return 0;
            return EqualityComparer<T>.Default.GetHashCode(value!);
        }

        /// <summary>
        /// 确定两个可选类型的值是否相等。
        /// </summary>
        /// <param name="left">要比较的第一个可选类型。</param>
        /// <param name="right">要比较的第二个可选类型。</param>
        /// <returns>
        /// 如果两个可选类型的值相等，则为 <see langword="true"/>；否则为 <see langword="false"/>。
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Option<T> left, Option<T> right) => left.Equals(right);

        /// <summary>
        /// 确定两个可选类型的值是否不相等。
        /// </summary>
        /// <param name="left">要比较的第一个可选类型。</param>
        /// <param name="right">要比较的第二个可选类型。</param>
        /// <returns>
        /// 如果两个可选类型的值不相等，则为 <see langword="true"/>；否则为 <see langword="false"/>。
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Option<T> left, Option<T> right) => !left.Equals(right);

        /// <summary>
        /// 返回表示当前可选类型的字符串。
        /// </summary>
        /// <returns>
        /// 如果可选类型有值，则返回该值的字符串表示；否则返回 "&lt;null&gt;"。
        /// </returns>
        public override string ToString()
        {
            if (!HasValue) return "<null option>";
            else return value!.ToString()!;
        }

        /// <summary>
        /// 返回一个遍历 <see cref="Option{T}"/> 的枚举器。
        /// </summary>
        /// <returns>可选类型的枚举器。</returns>
        public Enumerator GetEnumerator() => new(this);

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// 表示 <see cref="Option{T}"/> 的枚举器。
        /// </summary>
        public struct Enumerator : IEnumerator<T>
        {
            private readonly Option<T> __option;
            private bool _enumerated;

            /// <summary>
            /// 初始化 <see cref="Enumerator"/> 结构的新实例。
            /// </summary>
            /// <param name="option">要枚举的可选类型。</param>
            internal Enumerator(Option<T> option)
            {
                __option = option;
                _enumerated = false;
            }

            /// <inheritdoc/>
            public readonly T Current => __option.HasValue ? __option.value! : throw new InvalidOperationException("Enumeration has not started or has already finished.");

            readonly object? IEnumerator.Current => Current;

            /// <inheritdoc/>
            public bool MoveNext()
            {
                if (!_enumerated && __option.HasValue)
                {
                    _enumerated = true;
                    return true;
                }
                return false;
            }

            /// <inheritdoc/>
            public void Reset() => _enumerated = false;

            /// <inheritdoc/>
            public readonly void Dispose() { }
        }

        int IReadOnlyCollection<T>.Count => HasValue ? 1 : 0;
        readonly T? value;
    }
}
