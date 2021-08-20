using System;
using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;

namespace PasswordManagerApp.Models
{
  internal class CleanUpHostedService : IHostedService, IDisposable
  {
    private Timer _timer;
    private ISessionManager _sessionManager;

    public CleanUpHostedService(ISessionManager sessionManager)
    {
      _sessionManager = sessionManager;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
      _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromMinutes(3));
      return Task.CompletedTask;
    }

    private void DoWork(object state)
    {
      // There should be a mechanism to prevent running the
      // task if it's still running.
      // This is supposed to be embedded in the cleanUp method
      // I call from here.
      //Console.WriteLine($"Session count: {_sessionManager.Count}");
      _sessionManager.CleanUpSessions();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
      _timer?.Change(Timeout.Infinite, 0);
      return Task.CompletedTask;
    }

    public void Dispose()
    {
      _timer?.Dispose();
    }
  }
}