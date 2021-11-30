using System;
using System.Text;

namespace PurpleSofa
{
    public static class PsExtension
    {
        public static string PxToString(this byte[] self)
        {
            try
            {
                return Encoding.UTF8.GetString(self);
            }
            catch (Exception e)
            {
                PsLogger.Error(e);
                throw new PsExtensionException(e);
            }
        }
        
        public static byte[] PxToBytes(this string self)
        {
            try
            {
                return Encoding.UTF8.GetBytes(self);
            }
            catch (Exception e)
            {
                PsLogger.Error(e);
                throw new PsExtensionException(e);
            }
        }
    }
    
    public class PsExtensionException : Exception
    {
        public PsExtensionException(Exception exception) : base(exception.ToString())
        {
        }
    }
}