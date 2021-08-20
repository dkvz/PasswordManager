using System;
using System.IO;
using PasswordManager.Security;
using PasswordManager.Models;
using System.Security.Cryptography;
using McMaster.Extensions.CommandLineUtils;
using System.Text;

namespace PasswordManagerTools
{
  public class Program
  {
    public static int Main(string[] args)
        => CommandLineApplication.Execute<Program>(args);

    [Option(
      Description = "What to do; c - Create ; r - Reads ; m - Modifies ; a - Adds ; d - Removes",
      ShortName = "m")
    ]
    public string Mode { get; }

    [Option(Description = "Output or input filename", ShortName = "f")]
    public string Filename { get; }

    /* [Option(Description = "Amount of extraneous entries", ShortName = "n")]
    public int Count { get; } */

    [Option(Description = "Text file used to fill random placeholder data")]
    public string PlaceholderFilename { get; }

    public enum Modes {
      Create = 1,
      Read = 0,
      Modify = 3,
      Add = 2,
      Delete = 4
    }

    private void OnExecute()
    {
      Random random = new Random();
      var filename = Filename ?? "pwdfile";
      // Default mode is to read an existing file:
      var mode = Modes.Read;
      if (Mode != null)
      {
        if (Mode.Contains('c'))
        {
          // Create new
          mode = Modes.Create;
        }
        else if (Mode.Contains('a'))
        {
          // Add new password
          mode = Modes.Add;
        }
        else if (Mode.Contains('m'))
        {
          // Modify existing
          mode = Modes.Modify;
        }
        else if (Mode.Contains('d'))
        {
          mode = Modes.Delete;
        }
      }
      //var count = Count > 0 ? Count : random.Next(random.Next(50));
      var masterPwd = Prompt.GetPassword("Enter master password: ");
      if (masterPwd != null && masterPwd.Length > 0)
      {
        switch (mode)
        {
          case Modes.Delete:
          case Modes.Add:
          case Modes.Modify:
          case Modes.Create:
            alter(filename, ref masterPwd, mode);
            break;
          default:
            // Decrypt file and output to standard output
            if (Prompt.GetYesNo(
              "You are about to display plain text passwords on your terminal. Continue?",
              defaultAnswer: false
            ))
            {
              read(filename, ref masterPwd);
            }
            break;
        }
      }
      else
      {
        Console.WriteLine("Invalid master password, please try again.");
      }
    }

    private void alter(string filename, ref string masterPwd, Modes mode)
    {
      var fakePwd = Prompt.GetPassword("Enter placeholder password key (careful with special chars): ");
      IPlaceholderTextGenerator gen = null;
      if (PlaceholderFilename != null && !PlaceholderFilename.Equals(String.Empty))
      {
        var filePGen = new FilePlaceholderTextGenerator(PlaceholderFilename);
        filePGen.Initialize();
        if (filePGen.Valid) gen = filePGen;
      }
      if (gen == null) gen = new RandomPlaceholderTextGenerator();
      try
      {
        var data = new PasswordManagerData(filename);
        if (mode != Modes.Create)
        {
          // We need to read the current data first:
          data.ReadFromFile(ref masterPwd);
        }
        bool dontSave = false;
        // Most of the prompt work here could probably be refactored.
        switch (mode) {
          case Modes.Add:
            var name = Prompt.GetString("Name of the entry: ").Trim();
            var password = Prompt.GetPassword("Password: ");
            if (name != null && name.Length > 0) 
            {
              data.PasswordEntries.Add(new GenericPasswordEntry(name, password));
            }
            break;
          case Modes.Delete:
            var toDelete = Prompt.GetString("Name of the entry: ").Trim();
            int removed = data.PasswordEntries.RemoveAll(p => p.Name.Equals(toDelete));
            Console.WriteLine($"Removed {removed} entries from the list");
            break;
          case Modes.Modify:
            var toModify = Prompt.GetString("Name of the entry to edit: ").Trim();
            var entry = data.PasswordEntries.Find(e => e.Name.Equals(toModify));
            if (entry != null)
            {
              entry.Password = Prompt.GetPassword("New password: ");
              entry.Date = DateTime.Now;
            }
            else
            {
              Console.WriteLine($"Entry {toModify} does not exist");
              dontSave = true;
            }
            break;
          default:
            data.PasswordEntries.Add(new GenericPasswordEntry("test", "testPassword"));
            break;
        }
        // Don't forget to mention the filename that was written at the end.
        if (!dontSave) 
        {
          data.SaveToFile(ref masterPwd, ref fakePwd, gen);
          Console.WriteLine($"Password file {filename} has been created or modified");
        }
      }
      catch (FileNotFoundException)
      {
        Console.Error.WriteLine($"File {filename} not found");
      }
      catch (CryptographicException ex)
      {
        Console.Error.WriteLine("Could not decrypt file, most likely caused by a wrong password");
        Console.Error.WriteLine($"Error description: {ex.ToString()}");
      }
      catch (Exception ex)
      {
        Console.Error.WriteLine($"Error: Could not write the password file {filename}");
        Console.Error.WriteLine(ex.StackTrace);
      }
    }

    private void create(string filename, ref string masterPwd)
    {
      alter(filename, ref masterPwd, Modes.Create);
    }

    private void read(string filename, ref string masterPwd)
    {
      try
      {
        var data = new PasswordManagerData(filename);
        data.ReadFromFile(ref masterPwd);
        data.PasswordEntries.ForEach(p =>
        {
          Console.WriteLine($"{p.Name} - Modified {p.Date.ToString()} : {p.Password}");
        });
      }
      catch (FileNotFoundException)
      {
        Console.Error.WriteLine($"File {filename} not found");
      }
      catch (CryptographicException ex)
      {
        Console.Error.WriteLine("Could not decrypt file, most likely caused by a wrong password");
        Console.Error.WriteLine($"Error description: {ex.ToString()}");
      }
      catch (Exception ex)
      {
        Console.Error.WriteLine($"Could not read data file {filename}");
        Console.Error.WriteLine($"Exception: {ex.GetType().ToString()}");
        Console.Error.WriteLine(ex.StackTrace);
      }
    }

  }
}
