using System;
using JiksLib.Extensions;

namespace JiksLib
{
    /// <summary>
    /// 进度报告器
    /// </summary>
    public static class Progress
    {
        /// <summary>
        /// 创建空的进度报告器
        /// </summary>
        /// <typeparam name="T">进度类型</typeparam>
        /// <returns>进度报告器</returns>
        public static IProgress<T> Null<T>() => NullProgressImpl<T>.Instance;

        /// <summary>
        /// 创建一个用于Unity的只用于主线程的进度报告器
        /// </summary>
        /// <typeparam name="T">进度类型</typeparam>
        /// <param name="onProgressReported">当进度被报告时</param>
        /// <returns>进度报告器</returns>
        public static IProgress<T> Create<T>(Action<T> onProgressReported) =>
            new ActionProgressImpl<T>(onProgressReported.ThrowIfNull());

        /// <summary>
        /// 创建子进度报告器
        /// 子进度是父进度的一部分，当子进度被报告时，父进度将报告对应的部分的进度
        /// </summary>
        /// <typeparam name="T">进度类型</typeparam>
        /// <param name="parentProgress">父进度报告器</param>
        /// <param name="startProgress">子进度开始部分</param>
        /// <param name="endProgress">子进度结束部分</param>
        /// <returns>子进度报告器</returns>
        public static IProgress<float> CreateSubProgress(
            this IProgress<float> parentProgress,
            float startProgress,
            float endProgress)
        {
            parentProgress.ThrowIfNull();

            if (float.IsNaN(startProgress) || float.IsInfinity(startProgress))
                throw new ArgumentOutOfRangeException(
                    nameof(startProgress),
                    "Value cannot be NaN or infinity.");

            if (float.IsNaN(endProgress) || float.IsInfinity(endProgress))
                throw new ArgumentOutOfRangeException(
                    nameof(endProgress),
                    "Value cannot be NaN or infinity.");

            if (parentProgress is NullProgressImpl<float>)
                return NullProgressImpl<float>.Instance;

            return new ActionProgressImpl<float>(x =>
                parentProgress.Report(
                    startProgress + x * (endProgress - startProgress)));
        }

        /// <summary>
        /// 转换进度类型
        /// </summary>
        /// <typeparam name="T">转换来源类型</typeparam>
        /// <typeparam name="U">转换目标类型</typeparam>
        /// <param name="source">转换来源进度</param>
        /// <param name="converter">进度转换器</param>
        /// <returns>转换后的进度</returns>
        public static IProgress<U> Convert<T, U>(
            this IProgress<T> source,
            Func<U, T> converter)
        {
            source.ThrowIfNull();
            converter.ThrowIfNull();

            if (source is NullProgressImpl<T>)
                return NullProgressImpl<U>.Instance;

            return new ActionProgressImpl<U>(
                x => source.Report(converter(x)));
        }

        private sealed class NullProgressImpl<T> : IProgress<T>
        {
            public readonly static NullProgressImpl<T> Instance = new();
            void IProgress<T>.Report(T value) { }
        }

        private sealed class ActionProgressImpl<T> : IProgress<T>
        {
            readonly Action<T> onProgressReported;

            public ActionProgressImpl(Action<T> onProgressReported)
            {
                this.onProgressReported = onProgressReported;
            }

            void IProgress<T>.Report(T value)
            {
                onProgressReported(value);
            }
        }
    }
}
