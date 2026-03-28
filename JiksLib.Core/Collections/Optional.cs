using System;
using System.Runtime.CompilerServices;

namespace JiksLib.Collections
{
    public readonly struct Optional<T>
        // todo: 实现 IReadOnlyCollection<T> 接口
        // todo: 实现 Equal 约束
        where T : notnull
    {
        public Optional(T value)
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

        public readonly bool HasValue;

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Optional<U> Map<U>(Func<T, U> mapper)
            where U : notnull
        {
            if (!HasValue) return new();
            else return new(mapper(value!));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Optional<U> FlatMap<U>(Func<T, Optional<U>> mapper)
            where U : notnull
        {
            if (!HasValue) return new();
            else return mapper(value!);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T? GetOrDefault() =>
            HasValue ? value! : default;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetOrDefault(T defaultValue) =>
            HasValue ? value! : defaultValue;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetOrDefault(Func<T> thunk) =>
            HasValue ? value! : thunk();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Optional<T> OrElse(Optional<T> optional)
        {
            if (HasValue) return this;
            else return optional;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Optional<T> OrElse(Func<Optional<T>> thunk)
        {
            if (HasValue) return this;
            else return thunk();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Optional<(T, U)> Join<U>(Optional<U> optional)
            where U : notnull
        {
            if (HasValue && optional.HasValue)
                return new((value!, optional.value!));
            else
                return new();
        }

        public override string ToString()
        {
            if (!HasValue) return "<null>";
            else return value!.ToString();
        }

        readonly T? value;
    }
}
