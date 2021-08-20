using System;
using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace PasswordManagerApp.Models
{
  public class NotificationManager : INotificationManager
  {

    private readonly ILogger _logger;
    public string SmtpHost { get; set; }
    public string EmailFrom { get; set; }
    public string EmailTo { get; set; }
    public int SmtpPort { get; set; }
    private bool _notifications;
    public const string CauseLoginFailure = "LOGIN FAIL";
    public const string CauseLoginSuccess = "LOGIN SUCCESS";
    public const string CauseUnauthorizedApiAccess = "UNAUTH API ACCESS";

    public NotificationManager(ILogger<NotificationManager> logger, IConfiguration config)
    {
      _logger = logger;
      SmtpHost = config.GetValue<string>("smtpHost") 
        ?? "localhost";
      EmailFrom = config.GetValue<string>("mailFrom") 
        ?? "pwdmanager@localhost";
      EmailTo = config.GetValue<string>("mailTo") 
        ?? "root";
      SmtpPort = config.GetValue<int>("smtpPort");
      if (SmtpPort <= 0) SmtpPort = 25;
      _notifications = config.GetValue<bool>("notifications");
      _logger.LogInformation($"Notifier configured with email host {SmtpHost} port {SmtpPort}");
      if (!_notifications) 
        _logger.LogWarning("Notification channels have been disabled by configuration");
    }

    public void NotifyMostChannels(string cause, string msg, string extra = null)
    {
      Log(cause, msg);

      if (extra != null)
        msg = msg + "\n---\n" + extra;

      if (_notifications) sendEmail(cause, msg);
    }

    public void NotifyMostChannels(string cause, string msg, string extra, IPAddress clientIp)
    {
      NotifyMostChannels($"{cause} - {clientIp.ToString()}", msg, extra);
    }

    public void Log(string cause, string msg)
    {
      _logger.LogWarning($"{cause} - {msg}");
    }

    private void sendEmail(string subject, string msg)
    {
      try
      {
        SmtpClient client = new SmtpClient(SmtpHost);
        client.Port = SmtpPort;
        //client.UseDefaultCredentials = false;
        MailMessage message = new MailMessage();
        message.From = new MailAddress(EmailFrom);
        message.To.Add(EmailTo);
        message.Body = msg;
        message.Subject = $"PasswordManagerApp - {subject}";
        client.SendAsync(message, null);
      }
      catch(Exception ex)
      {
        _logger.LogError(ex, "Error trying to send an email");
      }
    }

  }
}