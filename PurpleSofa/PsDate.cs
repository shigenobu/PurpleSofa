using System;

namespace PurpleSofa
{
    public class PsDate
    {
        public static string Now()
        {
            return DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        }
    }
}