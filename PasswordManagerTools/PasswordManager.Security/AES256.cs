using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace PasswordManager.Security
{
  /// <summary>
  /// A simple wrapper to the AesManaged class and the AES algorithm.
  /// Uses 256 bit key, 128 bit psuedo-random salt and a 16 bit
  /// psuedo-randomly generated Initialization Vector 
  ///
  /// Original code was from here: https://github.com/jonjomckay/dotnet-simpleaes
  /// With MIT license
  /// </summary>
  public class AES256
  {
    // Preconfigured Encryption Parameters
    private static readonly int BlockBitSize = 128;
    // To be sure we get the correct IV size, set the block size
    private static readonly int KeyBitSize = 256;
    // AES 256 bit key encryption
    // Preconfigured Password Key Derivation Parameters
    private static readonly int SaltBitSize = 128;

    private static readonly int Iterations = 10000;
    /// <summary>
    /// Encrypts the plainText input using the given Key.
    /// A 128 bit random salt will be generated and prepended to the ciphertext before it is base64 encoded.
    /// A 16 bit random Initialization Vector will also be generated prepended to the ciphertext before it is base64 encoded.
    /// </summary>
    /// <param name="plainText">The plain text to encrypt.</param>
    /// <param name="key">The plain text encryption key.</param>
    /// <returns>The salt, IV and the ciphertext, Base64 encoded.</returns>
    public static string Encrypt(string plainText, string key)
    {
      //User Error Checks
      if (string.IsNullOrEmpty(key))
      {
        throw new ArgumentNullException("key");
      }
      if (string.IsNullOrEmpty(plainText))
      {
        throw new ArgumentNullException("plainText");
      }

      // Derive a new Salt and IV from the Key, using a 128 bit salt and 10,000 iterations
      using (var keyDerivationFunction = new Rfc2898DeriveBytes(key, SaltBitSize / 8, Iterations))
      {
        return Encrypt(ref plainText, keyDerivationFunction);
      }
    }

    public static string Encrypt(string plainText, byte[] key)
    {
      if (key == null || key.Length == 0)
      {
        throw new ArgumentNullException("key");
      }
      if (string.IsNullOrEmpty(plainText))
      {
        throw new ArgumentNullException("plainText");
      }

      // Generate the salt:
      byte[] salt = new byte[SaltBitSize / 8];
      using (RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider())
      {
        // Fill the array with random values:
        rngCsp.GetBytes(salt);
      }

      using (var keyDerivationFunction = new Rfc2898DeriveBytes(key, salt, Iterations))
      {
        return Encrypt(ref plainText, keyDerivationFunction);
      }
    }

    private static string Encrypt(ref string plainText, Rfc2898DeriveBytes keyDerivationFunction)
    {
      using (var aesManaged = Aes.Create())
      {
        aesManaged.KeySize = KeyBitSize;
        aesManaged.BlockSize = BlockBitSize;

        // Generate random IV
        aesManaged.GenerateIV();

        // Retrieve the Salt, Key and IV
        byte[] saltBytes = keyDerivationFunction.Salt;
        byte[] keyBytes = keyDerivationFunction.GetBytes(KeyBitSize / 8);
        byte[] ivBytes = aesManaged.IV;

        // Create an encryptor to perform the stream transform.
        // Create the streams used for encryption.
        using (var encryptor = aesManaged.CreateEncryptor(keyBytes, ivBytes))
        {
          using (var memoryStream = new MemoryStream())
          {
            using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
            {
              using (var streamWriter = new StreamWriter(cryptoStream))
              {
                // Send the data through the StreamWriter, through the CryptoStream, to the underlying MemoryStream
                streamWriter.Write(plainText);
              }
            }

            // Return the encrypted bytes from the memory stream in Base64 form.
            byte[] cipherTextBytes = memoryStream.ToArray();

            // Resize saltBytes and append IV
            Array.Resize(ref saltBytes, saltBytes.Length + ivBytes.Length);
            Array.Copy(ivBytes, 0, saltBytes, SaltBitSize / 8, ivBytes.Length);

            // Resize saltBytes with IV and append cipherText
            Array.Resize(ref saltBytes, saltBytes.Length + cipherTextBytes.Length);
            Array.Copy(cipherTextBytes, 0, saltBytes, (SaltBitSize / 8) + ivBytes.Length, cipherTextBytes.Length);

            return Convert.ToBase64String(saltBytes);
          }
        }
      }
    }

    /// <summary>
    /// Decrypts the ciphertext using the Key.
    /// </summary>
    /// <param name="ciphertext">The ciphertext to decrypt.</param>
    /// <param name="key">The plain text encryption key.</param>
    /// <returns>The decrypted text.</returns>
    public static string Decrypt(string ciphertext, string key)
    {
      if (string.IsNullOrEmpty(ciphertext))
      {
        throw new ArgumentNullException("cipherText");
      }
      if (string.IsNullOrEmpty(key))
      {
        throw new ArgumentNullException("key");
      }

      return decrypt(ciphertext, key as Object);
    }

    public static string Decrypt(string ciphertext, byte[] key)
    {
      if (string.IsNullOrEmpty(ciphertext))
      {
        throw new ArgumentNullException("cipherText");
      }
      if (key == null || key.Length == 0)
      {
        throw new ArgumentNullException("key");
      }

      return decrypt(ciphertext, key);
    }

    public static byte[] DecryptToByteArray(string ciphertext, byte[] key)
    {
      return decryptToByteArray(ciphertext, key);
    }

    // I seriously hope all these KeyBitSize / 8 etc. are inlined by the
    // compiler.
    private static byte[] decryptToByteArray(string ciphertext, Object key)
    {
      // Prepare the Salt and IV arrays
      byte[] saltBytes = new byte[SaltBitSize / 8];
      byte[] ivBytes = new byte[BlockBitSize / 8];

      // Read all the bytes from the cipher text
      byte[] allTheBytes = Convert.FromBase64String(ciphertext);

      // Extract the Salt, IV from our ciphertext
      Array.Copy(allTheBytes, 0, saltBytes, 0, saltBytes.Length);
      Array.Copy(allTheBytes, saltBytes.Length, ivBytes, 0, ivBytes.Length);

      // Extract the Ciphered bytes
      byte[] ciphertextBytes = new byte[allTheBytes.Length - saltBytes.Length - ivBytes.Length];
      Array.Copy(allTheBytes, saltBytes.Length + ivBytes.Length, ciphertextBytes, 0, ciphertextBytes.Length);

      using (var keyDerivationFunction =
        key.GetType() == typeof(byte[]) ? new Rfc2898DeriveBytes((byte[])key, saltBytes, Iterations) :
        new Rfc2898DeriveBytes(key.ToString(), saltBytes, Iterations))
      {
        // Get the Key bytes
        byte[] keyBytes = keyDerivationFunction.GetBytes(KeyBitSize / 8);

        // Create a decrytor to perform the stream transform.
        // Create the streams used for decryption.
        // The default Cipher Mode is CBC and the Padding is PKCS7 which are both good
        using (var aesManaged = Aes.Create())
        {
          aesManaged.KeySize = KeyBitSize;
          aesManaged.BlockSize = BlockBitSize;

          using (var decryptor = aesManaged.CreateDecryptor(keyBytes, ivBytes))
          {
            using (var memoryStream = new MemoryStream(ciphertextBytes))
            {
              using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
              {
                //byte[] ret = new byte[ciphertextBytes.Length];
                var lastBlock = ciphertextBytes.Length - (BlockBitSize / 8);
                // We should stop when 0's are coming:
                // Ready to ret.length - (BlockBitSize / 8)
                //cryptoStream.Read(ret, 0, ret.Length);
                byte[] firstPart = null;
                if (lastBlock > 0)
                {
                  //firstPart = new byte[lastBlock];
                  //cryptoStream.Read(firstPart, 0, lastBlock);
                  firstPart = readStreamUpTo(cryptoStream, lastBlock);
                  // We got data up to index lastBlock - 1.
                  // Or so I think.
                }
                // Start reading from lastBlock, check if we 
                // got byte value 0.
                //byte[] lastPart = new byte[BlockBitSize / 8];
                int fromFirstPart = 0;
                if (firstPart != null) fromFirstPart = firstPart.Length;
                /*cryptoStream.Read(
                  lastPart,
                  0,
                  lastPart.Length
                );*/
                var lastPart = readStreamUpTo(cryptoStream, BlockBitSize / 8);
                int i;
                for (i = 0; i < lastPart.Length; i++)
                {
                  if (lastPart[i] == 0x00) break;
                }
                byte[] ret = new byte[fromFirstPart + i];
                if (fromFirstPart > 0)
                {
                  Array.Copy(firstPart, ret, firstPart.Length);
                }
                Array.Copy(lastPart, 0, ret, fromFirstPart, i);
                return ret;
              }
            }
          }
        }
      }
    }

    /* private static string Decrypt(string ciphertext, Object key) {
      // Prepare the Salt and IV arrays
      byte[] saltBytes = new byte[SaltBitSize / 8];
      byte[] ivBytes = new byte[BlockBitSize / 8];

      // Read all the bytes from the cipher text
      byte[] allTheBytes = Convert.FromBase64String(ciphertext);

      // Extract the Salt, IV from our ciphertext
      Array.Copy(allTheBytes, 0, saltBytes, 0, saltBytes.Length);
      Array.Copy(allTheBytes, saltBytes.Length, ivBytes, 0, ivBytes.Length);

      // Extract the Ciphered bytes
      byte[] ciphertextBytes = new byte[allTheBytes.Length - saltBytes.Length - ivBytes.Length];
      Array.Copy(allTheBytes, saltBytes.Length + ivBytes.Length, ciphertextBytes, 0, ciphertextBytes.Length);

      using (var keyDerivationFunction = 
        key.GetType() == typeof(byte[]) ? new Rfc2898DeriveBytes((byte[])key, saltBytes, Iterations) : 
        new Rfc2898DeriveBytes(key.ToString(), saltBytes, Iterations))
      {
        // Get the Key bytes
        byte[] keyBytes = keyDerivationFunction.GetBytes(KeyBitSize / 8);

        // Create a decrytor to perform the stream transform.
        // Create the streams used for decryption.
        // The default Cipher Mode is CBC and the Padding is PKCS7 which are both good
        using (var aesManaged = Aes.Create())
        {
          aesManaged.KeySize = KeyBitSize;
          aesManaged.BlockSize = BlockBitSize;

          using (var decryptor = aesManaged.CreateDecryptor(keyBytes, ivBytes))
          {
            using (var memoryStream = new MemoryStream(ciphertextBytes))
            {
              using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
              {
                using (var streamReader = new StreamReader(cryptoStream))
                {
                  // Return the decrypted bytes from the decrypting stream.
                  return streamReader.ReadToEnd();
                }
              }
            }
          }
        }
      }
    }
 */

    /**
    Not exactly perfect but should work.
    The previous solution using an extra sream (StreamReader)
    is still commented out above.
     */
    private static string decrypt(string ciphertext, Object key)
    {
      byte[] decrypted = decryptToByteArray(ciphertext, key);
      string ret = Encoding.UTF8.GetString(decrypted, 0, decrypted.Length);
      Array.Clear(decrypted, 0, decrypted.Length);
      return ret;
    }

    private static byte[] readStreamUpTo(CryptoStream stream, int length)
    {
      // At some point while upgrading to .NET 6, it came up
      // that the Read method on CryptoStream really doesn't
      // necessarily read up to where you want.
      // So I had to write my own dumb loop to make it so.
      var ret = new byte[length];
      int bytesRead = 0;
      while (bytesRead < length)
      {
        var buffer = new byte[16];
        var byteCount = stream.Read(buffer, 0, buffer.Length);
        if (byteCount == 0) break;
        Array.Copy(buffer, 0, ret, bytesRead, byteCount);
        bytesRead += byteCount;
      }
      return ret;
    }

  }
}