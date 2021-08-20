/**
  I called this "SecureSession" because Session doesn't exist in .NET Core.
  But in retrospect I could've just called it Session.
 */

using System;
using System.Net;
using System.Security.Cryptography;
using PasswordManager.Security;

namespace PasswordManagerApp.Models 
{
  public class SecureSession: IDisposable
  {
    public const int SESSION_ID_SIZE = 32;
    public IPAddress ClientIp { get; private set; }
    public byte[] SessionId { get; private set; }
    public DateTime Created { get; private set; }
    public PasswordManagerData Data { get; set; } = null;

    public SecureSession(IPAddress clientIp) 
    {
      ClientIp = clientIp;
      Created = DateTime.Now;
      var prov = new RNGCryptoServiceProvider();
      SessionId = new byte[SecureSession.SESSION_ID_SIZE];
      prov.GetBytes(SessionId);
    }

    public void Dispose() 
    {
      Array.Clear(SessionId, 0, SessionId.Length);
      Data = null;
    }

    public string GetSessionId()
    {
      return Convert.ToBase64String(SessionId);
    }

    public override string ToString() 
    {
      return GetSessionId();
    }
  }
}