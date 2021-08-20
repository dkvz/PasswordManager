using System;
using System.IO;
using System.Collections.Generic;
using PasswordManager.Models;
using Newtonsoft.Json;
using System.Linq;
using System.Text;
using System.Security.Cryptography;

namespace PasswordManager.Security
{
  /**
    This code got REALLY ugly when I started to want to accept both string and byte
    array keys.
    There should be a way to make it look much better.
   */
  public class PasswordManagerData
  {
    public const string FAKE_PASSWORD_KEY = "fakePasswordThatIsTotallyFakeIPromise";
    public int PlaceholderMinCount { get; set; } = 10;
    public int PlaceholderMaxCount { get; set; } = 60;
    public string Filename { get; set; }
    public bool IsEncryptedList { get; set; } = false;
    private byte[] originalPasswordHash;
    private byte[] salt;

    public List<GenericPasswordEntry> PasswordEntries { get; private set; }

    public PasswordManagerData(string filename)
    {
      Filename = filename;
      PasswordEntries = new List<GenericPasswordEntry>();
      // I think it was already null but anyway:
      originalPasswordHash = null;
      salt = null;
    }

    private bool clearPlaceholderData()
    {
      var fakePwd = PasswordEntries.Find(e => e.Name.Equals(PasswordManagerData.FAKE_PASSWORD_KEY));
      if (fakePwd != null)
      {
        PasswordEntries.RemoveAll(e => e.Name.Contains(fakePwd.Password));
        // Also remove the fakePwd entry itself:
        PasswordEntries.Remove(fakePwd);
        return true;
      }
      return false;
    }

    public GenericPasswordEntry GetEntry(int i)
    {
      if (i >= 0 && i < PasswordEntries.Count)
      {
        return PasswordEntries.ElementAt(i);
      }
      return null;
    }

    public void RemoveEntry(int i)
    {
      if (i >= 0 && i < PasswordEntries.Count)
      {
        PasswordEntries.RemoveAt(i);
      }
    }

    /**
    Calls to this method should always be in a try/catch block
     */
    public void SaveToFile(ref string masterPassword, ref string fakePassword,
      IPlaceholderTextGenerator placeholderGenerator)
    {
      if (IsEncryptedList)
        throw new ApplicationException("The password list is encrypted in memory");
     
      saveToFile(
        null,
        ref masterPassword,
        ref fakePassword,
        placeholderGenerator
      );
    }

    public void SaveToFile(byte[] masterPassword, ref string fakePassword,
      IPlaceholderTextGenerator placeholderGenerator, byte[] encryptArrayKey)
    {
      if (!IsEncryptedList)
        throw new ApplicationException("The password list is not encrypted");

      var dummy = PasswordManagerData.veryDummyRandomString();

      saveToFile(
        masterPassword, 
        ref dummy, 
        ref fakePassword, 
        placeholderGenerator, 
        encryptArrayKey
      );
    }

    private void saveToFile(byte[] arrayPassword, ref string stringPassword,
      ref string fakePassword, IPlaceholderTextGenerator placeholderGenerator,
      byte[] encryptArrayKey = null)
    {
      // Add the placeholder data:
      var random = new Random();
      clearPlaceholderData();
      // Decrypt the whole array if needed:
      List<GenericPasswordEntry> toSave;
      if (encryptArrayKey != null)
      {
        // I thought of using Linq but you can't use Ref inside
        // of lambdas so... Yeah.
        toSave = new List<GenericPasswordEntry>();
        foreach(var e in PasswordEntries) {
          toSave.Add(
            new GenericPasswordEntry(
              e.Name,
              AES256.Decrypt(e.Password, encryptArrayKey),
              e.Date
            )
          );
        }
      }
      else toSave = PasswordEntries;
      // Add the fakePasswordEntry:
      toSave.Insert(
        0,
        new GenericPasswordEntry(PasswordManagerData.FAKE_PASSWORD_KEY, fakePassword)
      );
      var pholders = random.Next(this.PlaceholderMinCount, this.PlaceholderMaxCount);
      for (int i = 0; i < pholders; i++)
      {
        toSave.Insert(
          random.Next(0, toSave.Count),
          new GenericPasswordEntry(fakePassword + i, placeholderGenerator.Generate())
        );
      }
      using (var file = new StreamWriter(Filename))
      {
        file.WriteLine(
          arrayPassword != null ? 
            AES256.Encrypt(JsonConvert.SerializeObject(toSave), arrayPassword) :
            AES256.Encrypt(JsonConvert.SerializeObject(toSave), stringPassword)
        );
      }
    }

    // Not using async here because if I do I can't pass the masterPassword
    // by reference and I really want to for some reason. 
    // Also, who cares, I'll be the only user of this thing.
    public void ReadFromFile(ref string masterPassword)
    {
      // Convert to byte array and save the original 
      // password hash, then clean up the byte array:
      byte[] mPwd = Encoding.UTF8.GetBytes(masterPassword);
      //mPwd.ToList().ForEach(e => Console.WriteLine(e));
      readFromFile(mPwd);
      saveOriginalPasswordHash(mPwd);
      Array.Clear(mPwd, 0, mPwd.Length);
    }

    private void saveOriginalPasswordHash(byte[] masterPassword)
    {
      salt = new byte[32];
      using (RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider())
      {
        // Fill the array with random values:
        rngCsp.GetBytes(salt);
      }
      originalPasswordHash = getPasswordHash(masterPassword);
    }

    private byte[] getPasswordHash(byte[] password)
    {
      byte[] concat = new byte[salt.Length + password.Length];
      Array.Copy(salt, concat, salt.Length);
      Array.Copy(password, 0, concat, salt.Length, password.Length);
      byte[] result;
      using (SHA256 sha256Hash = SHA256.Create())  
      {
        // Compute the hash:
        result = sha256Hash.ComputeHash(concat);
      }
      Array.Clear(concat, 0, concat.Length);
      return result;
    }

    public bool IsOriginalPassword(byte[] password)
    {
      if (originalPasswordHash != null)
      {
        byte[] hashed = getPasswordHash(password);
        return hashed.SequenceEqual(originalPasswordHash);
      }
      return false;
    }

    private static string veryDummyRandomString()
    {
      var random = new Random();
      return random.NextDouble().ToString();
    }

    public void ReadFromFile(byte[] masterPassword, byte[] encryptArrayKey = null)
    {
      // I don't know what I'm doing with these ref string things.
      // They're not nullable or defaultable.
      // The caller is supposed to clear the masterPassword byte array afterwards.
      readFromFile(masterPassword, encryptArrayKey);
      saveOriginalPasswordHash(masterPassword);
    }

    private void readFromFile(byte[] arrayPassword, byte[] encryptArrayKey = null)
    {
      if (encryptArrayKey != null)
        IsEncryptedList = true;
      else
        IsEncryptedList = false;

      string source = File.ReadAllText(Filename);
      if (source != null && source.Length > 0)
      {
        // Decrypt:
        string decrypted =AES256.Decrypt(source, arrayPassword);
        
        JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(
          decrypted
        ).ForEach(e =>
        {
          DateTime d = DateTime.Now;
          if (e.ContainsKey("Date"))
          {
            try
            {
              d = DateTime.Parse(e["Date"]);
            }
            catch { }
          }
          // Make sure to not encrypt the fake password entry.
          PasswordEntries.Add(
            new GenericPasswordEntry(
              e["Name"],
              (encryptArrayKey == null || 
                e["Name"].Equals(PasswordManagerData.FAKE_PASSWORD_KEY)) ?
                  e["Password"] : AES256.Encrypt(e["Password"], encryptArrayKey),
              d
            )
          );
        });
        clearPlaceholderData();
      }
      else throw new Exception("File was empty or does not exist");
    }

    public void AddEntry(string name, string password)
    {
      PasswordEntries.Add(
        new GenericPasswordEntry(name, password)
      );
    }

    public void AddEntryAndEncrypt(string name, string password, byte[] encryptArrayKey)
    {
      if (!IsEncryptedList)
        throw new ApplicationException("The list is not currently encrypted in memory");
      PasswordEntries.Add(
        new GenericPasswordEntry(
          name,
          AES256.Encrypt(password, encryptArrayKey)
        )
      );
    }

  }
}