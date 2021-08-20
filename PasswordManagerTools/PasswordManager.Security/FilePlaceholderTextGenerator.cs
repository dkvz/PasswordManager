using System;
using System.IO;

namespace PasswordManager.Security
{
  /**
  The class stores the entire text file in memory. Don't use
  a super massive file.
   */
  public class FilePlaceholderTextGenerator : IPlaceholderTextGenerator
  {

    public string Filename { get; private set; }
    public bool Valid { get; private set; }
    private string _source;
    private Random _random;
    // We're going to divide the full length of the source text
    // by a divider that's inside these bounds:
    public const int DIVIDER_LBOUND = 3;
    public const int DIVIDER_HBOUND = 8;

    public FilePlaceholderTextGenerator(string filename)
    {
      this.Filename = filename;
      this._random = new Random();
      // I'm relying on the fact that booleans are 
      // implicitely initialized to false for Valid.
    }

    public void Initialize()
    {
      try
      {
        _source = File.ReadAllText(Filename);
        Valid = (_source != null && _source.Length > 0);
      }
      catch 
      {
        // Just set Valid to false:
        Valid = false;
      }
    }

    public string Generate()
    {
      if (!Valid) throw new Exception("Generator not initialized");
      int chunkS = (int)Math.Ceiling(
        _source.Length / (decimal)_random.Next(
          FilePlaceholderTextGenerator.DIVIDER_LBOUND, FilePlaceholderTextGenerator.DIVIDER_HBOUND
          )
        );
      int start = _random.Next(0, _source.Length);
      Console.WriteLine($"Chunk size: {chunkS}; Start: {start}");
      if (start + chunkS >= _source.Length)
      {
        return _source.Substring(start, _source.Length - start) +
          _source.Substring(0, chunkS - (_source.Length - start));
      }
      else
      {
        return _source.Substring(start, chunkS);
      }
    }

  }
}