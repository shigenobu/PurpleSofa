using System;
using System.Text;

namespace PurpleSofa
{
    /// <summary>
    ///     Utils.
    /// </summary>
    public static class PsUtils
    {
        /// <summary>
        ///     Random chars.
        /// </summary>
        private const string RandomChars = "0123456789abcedfghijklmnopqrstuvwxyzABCEDFGHIJKLMNOPQRSTUVWXYZ";
        
        /// <summary>
        ///     Make Random string.
        /// </summary>
        /// <param name="length">length</param>
        /// <returns>random string</returns>
        public static string RandomString(int length)
        {
            if (length < 1)
                return string.Empty;
 
            var randomCharLen = RandomChars.Length;
            var builder = new StringBuilder(length);
            var random = new Random();
 
            for (var i = 0; i < length; i++)
            {
                var idx = random.Next(randomCharLen);
                builder.Append(RandomChars[idx]);
            }
 
            return builder.ToString();
        }
        
        /// <summary>
        ///     Or null.
        /// </summary>
        /// <param name="func">func</param>
        /// <typeparam name="T">type</typeparam>
        /// <returns>invoke result or null</returns>
        public static T? OrNull<T>(Func<T> func)
        {
            try
            {
                return func.Invoke();
            }
            catch (Exception e)
            {
                PsLogger.Error(e);
            }

            return default;
        }
    }
}