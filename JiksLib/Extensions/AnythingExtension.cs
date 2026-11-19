using System;

namespace JiksLib.Extensions
{
    public static class AnythingExtension
    {
        /// <summary>
        /// 当值为 null 时抛出异常
        /// </summary>
        /// <param name="value">要检查的值</param>
        /// <param name="paramName">异常中携带的参数名；为 null 时不设置</param>
        /// <param name="message">异常消息；为 null 时使用运行时默认消息</param>
        /// <exception cref="ArgumentNullException">
        /// 当 <paramref name="value"/> 为 null 时抛出
        /// </exception>
        public static T ThrowIfNull<T>(
            this T? value,
            string? paramName = null,
            string? message = "Value must not be null.")
        {
            if (value == null)
                throw new ArgumentNullException(paramName, message);

            return value;
        }
    }
}
