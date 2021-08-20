using NUnit.Framework;
using PasswordManagerApp.Security;

namespace PasswordManagerApp.Tests
{
  public class UtilityTests
  {
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void TestHashUtilsByteArrayConcat()
    {
      byte[] a1 = new byte[] { 0x10, 0x20, 0x30, 0x40 };
      byte[] a2 = new byte[] { 0x50, 0x60 };
      byte[] expected = new byte[] { 0x10, 0x20, 0x30, 0x40, 0x50, 0x60 };
      byte[] combined = HashUtils.ConcatByteArrays(a1, a2);
      // This makes a deep equal of the length then all of the
      // individual values:
      Assert.That(combined, Is.EqualTo(expected));
    }
  }
}