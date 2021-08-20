using System.Net;

namespace PasswordManagerApp.Models
{
  public interface INotificationManager
  {
    void NotifyMostChannels(string cause, string msg, string extra);
    void NotifyMostChannels(string cause, string msg, string extra, IPAddress clientIp);
    void Log(string cause, string msg);
  }
}