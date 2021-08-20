using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PasswordManagerApp.Models;

namespace PasswordManagerApp.Pages
{
  public class PrivacyModel : PageModel
  {

    private readonly INotificationManager _notificationManager;

    public PrivacyModel(INotificationManager notificationManager)
    {
      _notificationManager = notificationManager;
    }

    public void OnGet()
    {
      //_notificationManager.SendEmail("Does this work?", "Hello world");
    }
  }
}