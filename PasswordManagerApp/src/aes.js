import aesjs from 'aes-js';
import pbkdf2 from 'pbkdf2';
import { Uint8ArrayToBase64, base64ToUint8Array } from './b64Uint8ArrayConversions';
import { byteArrayToString } from './textConversions';
const Buffer = require('buffer/').Buffer;

export default {
  crypto: (self.crypto || self.msCrypto),
  quota: 65536,
  saltBytes: 16,
  ivBytes: 16,
  keyBytes: 32,
  iterations: 10000,
  // Stolen and adapted from here: https://gist.github.com/alexdiliberto/39a4ad0453310d0a69ce
  randomBytes: function (n) {
    const a = new Uint8Array(n);
    for (let i = 0; i < n; i += this.quota) {
      this.crypto.getRandomValues(a.subarray(i, i + Math.min(n - i, this.quota)));
    }
    return a;
  },
  encrypt: function (payload, password) {
    return new Promise((resolve, reject) => {
      // We want to output a base64 string in the end.
      // I should probably document the specificities of the encryption 
      // somewhere though it can all be found here, basically.
      const iv = this.randomBytes(this.ivBytes);
      const salt = new Buffer(this.randomBytes(this.saltBytes));
      const pwd = new Buffer(password);
      pbkdf2.pbkdf2(pwd, salt, this.iterations, this.keyBytes, 'sha1', (err, dKey) => {
        if (err) reject(err);
        else {
          const aesCbc = new aesjs.ModeOfOperation.cbc(dKey, iv);
          const encryptedBytes = aesCbc.encrypt(
            aesjs.padding.pkcs7.pad(aesjs.utils.utf8.toBytes(payload))
          );
          const fullBytes = new Uint8Array(encryptedBytes.length + salt.length + iv.length);
          fullBytes.set(salt);
          fullBytes.set(iv, salt.length);
          fullBytes.set(encryptedBytes, salt.length + iv.length);
          resolve(Uint8ArrayToBase64(fullBytes));
        }
      });
    });
  },
  decrypt: function (payload, password) {
    return new Promise((resolve, reject) => {
      // We expect payload to be base64.
      // We need to:
      /*
      - Convert the base64 to byte array
      - Extract the salt and iv 
        -> They have to be "Buffer" objects
        -> They appear in that order
      - Derive the key
      - Create the aesjs decryptor
      - Decrypt to byte array
      - Convert to string BUT IGNORE THE PADDING BYTES
      */
      const payloadBytes = base64ToUint8Array(payload);
      // We can use subarray:
      const salt = new Buffer(payloadBytes.subarray(0, this.saltBytes));
      const iv = payloadBytes.subarray(this.saltBytes, this.ivBytes + this.saltBytes);
      const pwd = new Buffer(password);
      pbkdf2.pbkdf2(pwd, salt, this.iterations, this.keyBytes, 'sha1', (err, dKey) => {
        if (err) reject(err);
        else {
          const aesCbc = new aesjs.ModeOfOperation.cbc(dKey, iv);
          const decryptedBytes = aesCbc.decrypt(
            payloadBytes.subarray(this.saltBytes + this.ivBytes, payloadBytes.length)
          );
          
          // Convert to string.
          // We need to find out if there are padding bytes and how many
          // in order to ignore them.
          /*
          The idea is to look at the last byte:
            - If it's 1 -> 16, it's a padding byte 
              -> Unicode 0 to 16 are not real characters.
              -> We could probably go 1 -> 15 and ignore the empty string.
            - The decimal number value of the byte is the amount of padding bytes
          */

          /* 
          OK so it looks like there are two ways to pad, another one just puts
          bytes with value 0 at the end. So in that case we have to remove the 
          bytes that are "0".
          */

          /*
          OK so I think the server uses a different padding method. I might have to
          implement to zero-padding here.

          -> In the end I also remove the padding on server so I'm not sur if that's
             ever going to be useful.
          */

          const lastB = decryptedBytes[decryptedBytes.length - 1];

          if (lastB > 0 && lastB <= 16) {
            resolve(
              byteArrayToString(
                decryptedBytes.subarray(
                  0,
                  decryptedBytes.length - lastB
                )
              )
            )
          } else if (lastB === 0) {
            // Remove all the trailing bytes that are 0.
            // Find the last one that is 0. We could start
            // searching at the last multiple of block size
            // but I'm not bothering with that.
            resolve(byteArrayToString(
              decryptedBytes.subarray(
                0,
                decryptedBytes.length - decryptedBytes.indexOf(0)
              )
            ))
          } else {
            resolve(byteArrayToString(decryptedBytes));
          }
        }
      });
    });
  },
  /**
   * Stole this from here: https://developer.mozilla.org/en-US/docs/Web/API/SubtleCrypto/digest
   */
  hexString: function (byteArray) {
    //const byteArray = new Uint8Array(buffer);

    const hexCodes = [...byteArray].map(value => {
      const hexCode = value.toString(16);
      const paddedHexCode = hexCode.padStart(2, '0');
      return paddedHexCode;
    });

    return hexCodes.join('');
  },
  hash: function (source) {
    // Source has to be converted ty a byte array.
    // We can use aesjs.utils.utf8.toBytes.

    /* return this.crypto.subtle.digest(
      'SHA-1',
      aesjs.utils.utf8.toBytes(source)
    )
    .then(value => this.hexString(value))
    .catch(() => this.hexString(this.randomBytes(20))); */
    return this.hashBytesToBytes(aesjs.utils.utf8.toBytes(source))
      .then(value => this.hexString(value))
      .catch(value => this.hexString(value));
  },
  hashBytesToBytes: function (bytes) {
    return this.crypto.subtle.digest(
      'SHA-1',
      bytes
    )
      .then(value => new Uint8Array(value))
      .catch(() => this.randomBytes(20));
  },
  hashBytesToString: function (bytes) {
    return this.hashBytesToBytes(bytes)
      .then(value => this.hexString(value))
      .catch(value => this.hexString(value));
  },
  hashStringToBytes: function (source) {
    return this.hashBytesToBytes(aesjs.utils.utf8.toBytes(source))
      .then(value => value)
      .catch(value => value);
  }

};