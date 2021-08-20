using Microsoft.AspNetCore.Mvc;
using System;
using System.Net;
using System.Text;
using System.Linq;
using PasswordManagerApp.Models;
using PasswordManagerApp.Models.Requests;
using System.Security.Cryptography;

namespace PasswordManagerApp.Controllers
{

  /// API Controller used by the client side JavaScript
  ///
  /// I thought of not using object bindings for JSON POST 
  /// requests since it's copying more immutable sensible
  /// strings around in memory, but using it is not only
  /// much more convenient but I also learn to write better
  /// dotnet core code which was the side goal of this 
  /// project. So, now you know (?).
  ///
  [Produces("application/json")]
  [Route("api/v1/[action]")]
  public class ApiController : Controller
  {

    private ISessionManager _sessionManager;
    private INotificationManager _notificationManager;

    public ApiController(ISessionManager sessionManager, INotificationManager notificationManager)
    {
      _sessionManager = sessionManager;
      _notificationManager = notificationManager;
    }

    [HttpGet]
    public JsonResult Test()
    {
      return Json("Strange API Version 0.1");
    }

    private SecureSession getSession(RequestBody sess, IPAddress clientIp)
    {
      if (sess != null && sess.SessionId != null && sess.SessionId.Length > 0)
      {
        return _sessionManager.GetSession(
          sess.SessionId, 
          Request.HttpContext.Connection.RemoteIpAddress
        );
      }
      return null;
    }

    [HttpPost]
    public JsonResult Names([FromBody]RequestBody sess)
    {
      var session = getSession(
        sess, 
        Request.HttpContext.Connection.RemoteIpAddress
      );
      if (session != null)
      {
        return Json(session.Data.PasswordEntries.Select(e => e.Name));
      }

      return ApiController.nonAuthorized();
    }

    [HttpPost]
    public JsonResult Logout([FromBody]RequestBody sess)
    {
      if (sess != null && sess.SessionId != null && sess.SessionId.Length > 0)
      {
        _sessionManager.CloseSession(
          sess.SessionId,
          Request.HttpContext.Connection.RemoteIpAddress
        );
      }
      // I chose to always return success.
      // return Json(new {result = "OK"});
      return ApiController.success();
    }

    private static JsonResult success()
    {
      return new JsonResult(new {result = "OK"});
    }

    private static JsonResult serverError()
    {
      var sErr = new JsonResult(new { result = "Server error" });
      sErr.StatusCode = 500;
      return sErr;
    }

    private static JsonResult nonAuthorized()
    {
      var res = new JsonResult(new { result = "Non authorized" });
      res.StatusCode = 403;
      return res;
    }

    [HttpPost]
    public JsonResult Entry([FromBody]EntryRequestBody req)
    {
      /*
      Single endpoint to do CRUD on password entries.

      The list IDs (EntryId) sent from the client start at 1 and not 0!
      Value 0 for EntryId means something is wrong (default int value).

      By design we just send 403 errors if the entry requested does
      not exist.
       */
      
      var clientIp = Request.HttpContext.Connection.RemoteIpAddress;
      var session = getSession(
        req, 
        clientIp
      );
      if (session != null && session.Data != null && session.Data.IsEncryptedList)
      {
        if (req.Operation == RequestOperation.Create)
        {
          // We need a non empty name and password.
          // By the nature of EntryRequestBody we know these two can't 
          // be null.
          if (req.Name.Length > 0 && req.Password.Length > 0)
          {
            try 
            {
              // This endpoint doesn't actually save the changes to disk.
              session.Data.AddEntry(req.Name, req.Password);
              return ApiController.success();
            }
            catch
            {
              return ApiController.serverError();
            }
          }
        }
        else 
        {
          // All the other operations require a valid entryId.
          // Default operation is Read.
          // Reminder: EntryId received from the client start at 
          // index 1 and not 0.
          if (req.EntryId > 0 && req.EntryId <= session.Data.PasswordEntries.Count)
          {
            try 
            {
              switch(req.Operation)
              {
                case RequestOperation.Modify:
                  // We need the name and passwords to not be empty:
                  if (req.Name.Length > 0 && req.Password.Length > 0)
                  {
                    var entry = session.Data.GetEntry(req.EntryId - 1);
                    entry.Name = req.Name;
                    entry.Password = req.Password;
                    entry.Date = DateTime.Now;
                    return ApiController.success();
                  }
                  break;
                case RequestOperation.Delete:
                  session.Data.RemoveEntry(req.EntryId - 1);
                  return ApiController.success();
                default:
                  // We can just give the GenericPasswordEntry object
                  // to Json() and it should work fine.
                  return Json(session.Data.PasswordEntries[req.EntryId - 1]);
              }
            }
            catch
            {
              return ApiController.serverError();
            }
          }
        }
      } else {
        _notificationManager.NotifyMostChannels(
          NotificationManager.CauseUnauthorizedApiAccess,
          "Unauthorized Entry API endpoint call",
          $"name: {req.Name}; op: {req.Operation}; entryId: {req.EntryId}",
          clientIp
        );
      }
      return ApiController.nonAuthorized();
    }

    /* [HttpPost]
    public JsonResult Test([FromBody]EntryRequestBody req)
    {
      return Json(req);
    } */

    [HttpPost]
    public JsonResult Save([FromBody]LoginRequestBody login)
    {
      // Attempt to save the password data file opened for
      // the session to disk.
      // This is very similar to logging in as we got the 
      // master password in the request.

      // Check if the password is the original one, return
      // a 401 if not.

      // At some point things got crazy and most of this 
      // method got moved to SessionManager.SaveSession.

      if (login != null && login.Password != null && login.Password.Length > 0)
      {
        try
        {
          var result = _sessionManager.SaveSession(
            login,
            Request.HttpContext.Connection.RemoteIpAddress
          );
          switch (result)
          {
            case SaveSessionResults.OriginalPasswordDiffers:
              var res = new JsonResult(new {result = "Original password differs"});
              res.StatusCode = 401;
              return res;
            case SaveSessionResults.Success:
              return ApiController.success();
          }
        }
        catch (Exception ex)
        {
          Console.Error.WriteLine("Error when saving session");
          Console.Error.WriteLine(ex.StackTrace);
          Console.Error.WriteLine(ex.ToString());
          return ApiController.serverError();
        }
      }
      return ApiController.nonAuthorized();
    }

    [HttpPost]
    public JsonResult Login([FromBody]LoginRequestBody login)
    {
      // - Check that the session exists
      // - Check that it's valid for current IP address
      // -> We then need to call something that will decrypt the file and
      //    re-encrypt it in the session memory - Try catch that appropriately

      // See JS function postLogin in api.js as to what is going
      // to use this endpoint.

      var res = new JsonResult(null);

      try
      {
        // Check if the request body is valid:
        if (login != null)
        {
          var result = _sessionManager.OpenSession(
            login,
            Request.HttpContext.Connection.RemoteIpAddress
          );
          // We should consider invalid IP address and invalid session to be
          // the same thing as far as the result status goes.
          switch (result)
          {
            case OpenSessionResult.DataFileError:
            case OpenSessionResult.InvalidPasswordOrFSError:
              res.Value = new { result = "Invalid password or data file error" };
              res.StatusCode = 403;
              break;
            case OpenSessionResult.InvalidSessionId:
              res.Value = new { result = "Invalid session ID" };
              res.StatusCode = 401;
              break;
            case OpenSessionResult.Success:
              res.Value = new { result = "Success" };
              res.StatusCode = 200;
              break;
            default:
              res.Value = new { result = "Unknown error" };
              res.StatusCode = 403;
              break;
          }
        }
        else
        {
          res.Value = new { result = "Invalid arguments" };
          res.StatusCode = 400;
        }
      }
      catch (Exception ex)
      {
        Console.Error.WriteLine("Error when opening session");
        Console.Error.WriteLine(ex.StackTrace);
        Console.Error.WriteLine(ex.ToString());
        res.Value = new { result = "Server error" };
        res.StatusCode = 500;
      }

      return res;
    }

  }

}