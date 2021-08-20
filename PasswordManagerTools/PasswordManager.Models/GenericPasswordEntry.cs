using System;

namespace PasswordManager.Models
{
  [Serializable]
  public class GenericPasswordEntry
  {
    public string Name { get; set; }
    public string Password { get; set; }
    public DateTime Date { get; set; }
    public GenericPasswordEntry (string name, string password, DateTime date)
    {
      this.Name = name;
      this.Password = password;
      this.Date = date;
    }

    public GenericPasswordEntry (string name, string password) : 
      this(name, password, DateTime.Now)
    {
    }
  }
}