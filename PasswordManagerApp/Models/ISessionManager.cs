using System.Net;
using System.Collections.Generic;
using PasswordManagerApp.Models.Requests;

namespace PasswordManagerApp.Models 
{
  public interface ISessionManager
  {
    int Count { get; }
    int GridHeight { get; }
    int GridWidth { get; }
    SecureSession CreateSession(IPAddress clientIp);
    SecureSession GetSession(string sessionId, IPAddress clientIp);
    OpenSessionResult OpenSession(LoginRequestBody login, IPAddress clientIp);
    SaveSessionResults SaveSession(LoginRequestBody login, IPAddress clientIp);
    bool CloseSession(string sessionId, IPAddress clientIp);
    void CleanUpSessions();
    List<string> GetAvailableDataFiles();
  }
}