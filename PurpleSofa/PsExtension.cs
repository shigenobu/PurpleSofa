using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace PurpleSofa
{
    /// <summary>
    ///     Extenstion.
    /// </summary>
    public static class PsExtension
    {
        /// <summary>
        ///     Byte[] to string.
        /// </summary>
        /// <param name="self">byte array</param>
        /// <returns>utf8 string</returns>
        /// <exception cref="PsExtensionException">error</exception>
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
        
        /// <summary>
        ///     String to byte array.
        /// </summary>
        /// <param name="self">utf8 string</param>
        /// <returns>byte array</returns>
        /// <exception cref="PsExtensionException">error</exception>
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

        public static EndPoint? PxSocketLocalEndPoint(this Socket self)
        {
            return PsUtils.OrNull(() => self.LocalEndPoint);
        }
        
        public static EndPoint? PxSocketRemoteEndPoint(this Socket self)
        {
            return PsUtils.OrNull(() => self.RemoteEndPoint);
        }
    }
    
    /// <summary>
    ///     Extension exception.
    /// </summary>
    public class PsExtensionException : Exception
    {
        /// <summary>
        ///     Constructor.
        /// </summary>
        /// <param name="exception">error</param>
        public PsExtensionException(Exception exception) : base(exception.ToString())
        {
        }
    }
}