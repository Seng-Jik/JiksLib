using System;

namespace JiksLib.Extensions
{
    public static class AnythingExtension
    {
        /// <summary>
        /// 当值为null时抛出异常
        /// </summary>
        public static T ThrowIfNull<T>(
            this T? value,
            string message = "Value must not be null.")
        {
            if (value == null)
                throw new ArgumentNullException(message);

            return value;
        }
    }
}
