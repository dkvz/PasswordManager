
using System;

namespace PasswordManagerApp.Models.Requests
{
  [Serializable]
  public class RequestBody
  {
    /// SessionId is a hash(hash(sequence) + Original Session ID)
    public string SessionId { get; set; }
    
    public RequestBody(string sessionId) 
    {
      SessionId = sessionId;
    }
  }
}