
using System;

namespace PasswordManagerApp.Models.Requests
{
  [Serializable]
  public class LoginRequestBody : RequestBody
  {
    /// Encrypted master password
    public string Password { get; set; }
    /// Requested data file
    public int DataFile { get; set; }
    public LoginRequestBody(string sessionId, string password, int dataFile) 
      : base(sessionId)
    {
      Password = password;
      DataFile = dataFile;
    }
  }
}