
using System;

namespace PasswordManagerApp.Models.Requests
{
  public enum RequestOperation
  {
    Read = 0,
    Create = 1,
    Modify = 2,
    Delete = 3
  }

  [Serializable]
  public class EntryRequestBody : RequestBody
  {
    /// Encrypted new password to save:
    public string Password { get; set; }

    /// Requested data file
    public string Name { get; set; }

    /**
    Default value for int is 0
     */
    public int EntryId { get; set; }

    public RequestOperation Operation { get; set; }


    public EntryRequestBody(string sessionId, string password, 
      string name, int entryId, RequestOperation operation) 
      : base(sessionId)
    {
      Password = password != null ? password : "";
      Name = name != null ? name : "";
      EntryId = entryId >= 0 ? 
        entryId : Math.Abs(entryId);
      Operation = operation >= 0 ? 
        operation : (RequestOperation)Math.Abs((int)operation);
    }
  }
}