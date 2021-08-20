using System;
using System.Linq;

namespace PasswordManager.Security
{
  public class RandomPlaceholderTextGenerator : IPlaceholderTextGenerator
  {
    private Random _random;
    public const int MIN_LENGTH = 200;
    public const int MAX_LENGTH = 5000;

    public RandomPlaceholderTextGenerator()
    {
      _random = new Random();
    }

    /**
    Stole this from there: 
    https://stackoverflow.com/questions/1344221/how-can-i-generate-random-alphanumeric-strings
    */
    public string RandomString(int length)
    {
      const string chars = 
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_";
      return new string(Enumerable.Repeat(chars, length)
        .Select(s => s[_random.Next(s.Length)]).ToArray());
    }

    public string Generate()
    {
      return this.RandomString(
        _random.Next(
          RandomPlaceholderTextGenerator.MIN_LENGTH,
          RandomPlaceholderTextGenerator.MAX_LENGTH
        )
      );
    }

  }
}