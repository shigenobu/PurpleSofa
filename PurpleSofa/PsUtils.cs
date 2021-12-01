using System;
using System.Text;

namespace PurpleSofa
{
    public static class PsUtils
    {
        private const string RandomChars = "0123456789abcedfghijklmnopqrstuvwxyzABCEDFGHIJKLMNOPQRSTUVWXYZ";
        
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