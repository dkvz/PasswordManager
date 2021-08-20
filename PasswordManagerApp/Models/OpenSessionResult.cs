namespace PasswordManagerApp.Models
{
  public enum OpenSessionResult
  {
    InvalidSessionId = 0,
    DataFileError = 1,
    InvalidPasswordOrFSError = 2,
    Success = 3,
    IpAddressNotAllowed = 4
  }
}
