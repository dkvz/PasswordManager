using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Configuration;
using PasswordManagerApp.Security;
using PasswordManager.Security;
using PasswordManagerApp.Models.Requests;
using System.Security.Cryptography;

namespace PasswordManagerApp.Models
{
  /**
    Not thread safe. But I don't really care.
   */
  public class SessionManager : ISessionManager, IDisposable
  {
    // Max Age in minutes. I don't think it can go over 60. 
    // Maybe. I don't know.
    public const int MAX_SESSION_AGE = 15;
    /**
    I'm not using a dictionnary to store sessions because I like
    to think using a list makes it harder to find the session IDs.
    Yeah I don't know.
    */
    public IDictionary<string, SecureSession> Sessions { get; set; }
    public int Count => Sessions.Count;
    public int GridWidth { get; private set; }
    public int GridHeight { get; private set; }
    private bool _cleaningUp = false;
    private IConfiguration _config;
    private INotificationManager _notificationManager;
    private TimeSpan _max_age = TimeSpan.FromMinutes(SessionManager.MAX_SESSION_AGE);
    private List<string> _dataFiles;
    private string _dataPath;
    private byte[] _sequence;

    public SessionManager(IConfiguration config, INotificationManager notificationManager)
    {
      Sessions = new ConcurrentDictionary<string, SecureSession>();
      _config = config;
      _notificationManager = notificationManager;
      var files = _config.GetSection("dataFiles").Get<List<string>>();
      _dataPath = _config.GetValue<string>("dataPath") ?? "var/";
      // We don't need to keep the sequence in clear text, hash it:
      string sequence = _config.GetValue<string>("sequence") ?? "1,2;99,99";
      _sequence = HashUtils.HashStringToBytes(sequence);
      if (files != null) _dataFiles = files;
      else _dataFiles = new List<string>();
      // Get the sequence grid dimensions from config:
      string[] dims = (_config.GetValue<string>("sequenceGridSize") ?? "3x3").Split('x');
      try
      {
        if (dims.Length == 2)
        {
          GridWidth = int.Parse(dims[0]);
          GridHeight = int.Parse(dims[1]);
        }
        else throw new FormatException("Invalid sequence grid dimensions in config file");
      }
      catch (FormatException)
      {
        GridHeight = 3;
        GridWidth = 3;
      }
    }
    private byte[] generateSessionKey(SecureSession session)
    {
      return HashUtils.ConcatByteArrays(_sequence, session.SessionId);
    }
    public void saveSessionData(SecureSession session, byte[] masterPassword, byte[] sessionKey)
    {
      var textGenerator = new RandomPlaceholderTextGenerator();
      var fakePwd = textGenerator.RandomString(16); 
      session.Data.SaveToFile(
        masterPassword,
        ref fakePwd,
        textGenerator,
        sessionKey
      );
    }
    public SaveSessionResults SaveSession(LoginRequestBody login, IPAddress clientIp)
    {
      if (login != null && login.SessionId != null && login.SessionId.Length > 0)
      {
        var session = GetSession(login.SessionId, clientIp);
        if (session != null && session.Data != null)
        {
          byte[] key = null;
          byte[] mPwd = null;
          try 
          {
            key = generateSessionKey(session);
            try
            {
              mPwd = AES256.DecryptToByteArray(login.Password, key);
            }
            catch(CryptographicException)
            {
              return SaveSessionResults.InvalidPassword;
            }
            if (session.Data.IsOriginalPassword(mPwd))
            {
              saveSessionData(session, mPwd, key);
              return SaveSessionResults.Success;
            }
            else
            {
              return SaveSessionResults.OriginalPasswordDiffers;
            }
          }
          finally
          {
            // The byte array might already be cleared but it
            // doesn't hurt to do it more than one time.
            if (mPwd != null) Array.Clear(mPwd, 0, mPwd.Length);
            if (key != null) Array.Clear(key, 0, key.Length);
          }
        }
      }
      // Could also mean invalid IP address in this case.
      return SaveSessionResults.InvalidSession;
    }
    public SecureSession CreateSession(IPAddress clientIp)
    {
      var sess = new SecureSession(clientIp);

      // At some point Sessions was a List
      //Sessions.Add(sess);

      // We could, in theory, have a collision of sessions.
      // I'm going to take that risk.
      // We need to hash(hashed_sequence + session_id)
      // Both of these are byte arrays, so we need to build a bigger byte array.
      // Then clear that byte array once we no longer need it.
      byte[] hashedId = HashUtils.HashBytes(HashUtils.ConcatByteArrays(_sequence, sess.SessionId));
      Sessions.TryAdd(HashUtils.ByteArrayToHexString(hashedId), sess);
      HashUtils.ClearByteArray(hashedId);
      return sess;
    }
    public void CleanUpSessions()
    {
      if (!_cleaningUp)
      {
        _cleaningUp = true;
        //Sessions.RemoveAll(s => DateTime.Now - s.Created > _max_age);
        foreach (var kp in Sessions.Where(s => DateTime.Now - s.Value.Created > _max_age))
        {
          Sessions.Remove(kp.Key);
        }
        _cleaningUp = false;
      }
    }
    public List<string> GetAvailableDataFiles()
    {
      return _dataFiles;
    }
    public OpenSessionResult OpenSession(LoginRequestBody login, IPAddress clientIp)
    {
      // Check if we got that session.
      // Trying to get something that doesn't exist from
      // a dictionnary throws exceptions. We should actually
      // do that to be completely thread safe.
      if (Sessions.ContainsKey(login.SessionId))
      {
        var sess = Sessions[login.SessionId];
        // Check if the IP address is correct:
        if (sess.ClientIp.Equals(clientIp))
        {
          // Now try to load the file into the session with
          // the decrypted password from it:
          if (login.DataFile >= 0 && _dataFiles.Count >= login.DataFile)
          {
            sess.Data = new PasswordManagerData(getFullDataPath(_dataFiles[login.DataFile]));
            byte[] mPwd = null;
            byte[] dKey = null;
            try
            {
              dKey = generateSessionKey(sess);
              mPwd = AES256.DecryptToByteArray(login.Password, dKey);
              sess.Data.ReadFromFile(mPwd, dKey);
              _notificationManager.NotifyMostChannels(
                NotificationManager.CauseLoginSuccess,
                "Successful login",
                null,
                clientIp
              );
              return OpenSessionResult.Success;
            }
            catch (Exception ex)
            {
              Console.Error.WriteLine($"Password Data File processing error: {ex.ToString()}");
              sess.Data = null;
              _notificationManager.NotifyMostChannels(
                NotificationManager.CauseLoginFailure,
                "Failed login attempt",
                null,
                clientIp
              );
              return OpenSessionResult.InvalidPasswordOrFSError;
            }
            finally
            {
              // This is a little redundant.
              if (mPwd != null) HashUtils.ClearByteArray(mPwd);
              if (dKey != null) HashUtils.ClearByteArray(dKey);
            }
          }
          else return OpenSessionResult.DataFileError;
        }
        else 
        {
          _notificationManager.NotifyMostChannels(
            NotificationManager.CauseLoginFailure,
            "Login attempt with IP address different from session",
            null,
            clientIp
          );
          return OpenSessionResult.IpAddressNotAllowed;
        }
      }
      else 
      {
        _notificationManager.NotifyMostChannels(
          NotificationManager.CauseLoginFailure,
          "Login attempt with wrong session ID or sequence",
          login.SessionId,
          clientIp
        );
        return OpenSessionResult.InvalidSessionId;
      }
    }

    public bool CloseSession(string sessionId, IPAddress clientIp)
    {
      if (Sessions.ContainsKey(sessionId))
      {
        var sess = Sessions[sessionId];
        if (sess.ClientIp.Equals(clientIp))
        {
          Sessions.Remove(sessionId);
          sess.Dispose();
          return true;
        }
      }
      return false;
    }

    public SecureSession GetSession(string sessionId, IPAddress clientIp)
    {
      // Check if the session exists.
      // Check if the IP address in the session matches.
      // If something doesn't verify, return null.
      if (Sessions.ContainsKey(sessionId))
      {
        var sess = Sessions[sessionId];
        if (sess.ClientIp.Equals(clientIp))
          return sess;
      }
      return null;
    }

    private string getFullDataPath(string filename)
    {
      return _dataPath + filename;
    }
    public void Dispose()
    {
      Sessions.ToList().ForEach(kp => kp.Value.Dispose());
      Sessions.Clear();
      // We could clear the sequence but since it's in cleartext on
      // the filesystem, I won't bother.
      // OK we only live once, I'll do it (???)
      HashUtils.ClearByteArray(_sequence);
    }
  }
}