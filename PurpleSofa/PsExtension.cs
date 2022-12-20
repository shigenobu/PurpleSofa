using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace PurpleSofa;

/// <summary>
///     Extenstion.
/// </summary>
internal static class PsExtension
{
    /// <summary>
    ///     Byte[] to utf8 string.
    /// </summary>
    /// <param name="self">byte array</param>
    /// <returns>utf8 string</returns>
    /// <exception cref="PsExtensionException">error</exception>
    internal static string PxToString(this byte[] self)
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
    ///     Utf8 string to byte array.
    /// </summary>
    /// <param name="self">utf8 string</param>
    /// <returns>byte array</returns>
    /// <exception cref="PsExtensionException">error</exception>
    internal static byte[] PxToBytes(this string self)
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

    /// <summary>
    ///     Get socket locale endpoint.
    /// </summary>
    /// <param name="self">socket</param>
    /// <returns>locale endpoint or null</returns>
    internal static EndPoint? PxSocketLocalEndPoint(this Socket self)
    {
        return PsUtils.OrNull(() => self.LocalEndPoint);
    }

    /// <summary>
    ///     Get socket remote endpoint.
    /// </summary>
    /// <param name="self">socket</param>
    /// <returns>remote endpoint or null</returns>
    internal static EndPoint? PxSocketRemoteEndPoint(this Socket self)
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
    internal PsExtensionException(Exception exception) : base(exception.ToString())
    {
    }
}