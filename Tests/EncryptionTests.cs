using NUnit.Framework;
using PasswordManager.Security;
using System.Text;
using System.Linq;
using System;

namespace PasswordManagerApp.Tests
{
  public class EncryptionTests
  {
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void EncryptAndDecryptCheckPadding()
    {
      string encrypted = AES256.Encrypt("test", "password");
      byte[] decrypted = AES256.DecryptToByteArray(
        encrypted,
        Encoding.UTF8.GetBytes("password")
      );
      Assert.That(
        decrypted,
        Is.EqualTo(
          new byte[] { 116, 101, 115, 116 }
        )
      );
    }

    [Test]
    public void EncryptAndDecryptString()
    {
      string encrypted = AES256.Encrypt("test", "password");
      string decrypted = AES256.Decrypt(encrypted, "password");
      Assert.AreEqual(decrypted, "test");
    }

    [Test]
    public void EncryptAndDecryptLargeString()
    {
      string encrypted = AES256.Encrypt(
        "test string that is longer than 16 characters",
        "password"
      );
      string decrypted = AES256.Decrypt(encrypted, "password");
      Assert.AreEqual(
        decrypted,
        "test string that is longer than 16 characters"
      );
    }

    [Test]
    public void EncryptAndDecryptExactly16chars()
    {
      string encrypted = AES256.Encrypt(
        "0123456789abcdef",
        "password"
      );
      string decrypted = AES256.Decrypt(encrypted, "password");
      Assert.AreEqual(
        decrypted,
        "0123456789abcdef"
      );
    }

  }
}