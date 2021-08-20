using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PasswordManagerApp.Models;

namespace PasswordManagerApp.Pages
{
  public class IndexModel : PageModel
  {
    private readonly ISessionManager _sessionManager;
    public SecureSession Session { get; set; }
    public List<string> DataFiles { get; set; }
    public int GridWidth { get; set; }
    public int GridHeight { get; set; }
    public IndexModel(ISessionManager sessionManager)
    {
      _sessionManager = sessionManager;
      DataFiles = _sessionManager.GetAvailableDataFiles();
      GridHeight = _sessionManager.GridHeight;
      GridWidth = _sessionManager.GridWidth;
    }
    public void OnGet()
    {
      Session = _sessionManager.CreateSession(
        Request.HttpContext.Connection.RemoteIpAddress
      );
    }
  }
}
