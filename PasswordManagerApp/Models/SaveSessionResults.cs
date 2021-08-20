namespace PasswordManagerApp.Models
{
  public enum SaveSessionResults
  {
    InvalidSession = 0,
    InvalidPassword = 2,
    Success = 3,
    OriginalPasswordDiffers = 5
  }
}
