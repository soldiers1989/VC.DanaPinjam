using System;
using System.Security.Cryptography;

namespace Encryption
{
	public class DecryptTransformer
	{
	    private EncryptionAlgorithm algorithmID;
	    private byte[] initVec;

    	internal DecryptTransformer (EncryptionAlgorithm deCryptId)
		{
			this.algorithmID = deCryptId;
		}

    	internal ICryptoTransform GetCryptoServiceProvider (byte[] bytesKey)
		{
			switch (this.algorithmID) {
			case EncryptionAlgorithm.Des:
				{
					DES des = new DESCryptoServiceProvider ();
					des.Mode = CipherMode.CBC;
					des.Key = bytesKey;
					des.IV = this.initVec;
					return des.CreateDecryptor ();
				}
			case EncryptionAlgorithm.Rc2:
				{
					RC2 rc = new RC2CryptoServiceProvider ();
					rc.Mode = CipherMode.CBC;
					return rc.CreateDecryptor (bytesKey, this.initVec);
				}
			case EncryptionAlgorithm.Rijndael:
				{
					Rijndael rijndael = new RijndaelManaged ();
					rijndael.Mode = CipherMode.CBC;
					return rijndael.CreateDecryptor (bytesKey, this.initVec);
				}
			case EncryptionAlgorithm.TripleDes:
				{
					TripleDES edes = new TripleDESCryptoServiceProvider ();
					edes.Mode = CipherMode.CBC;
					return edes.CreateDecryptor (bytesKey, this.initVec);
				}
			}
			throw new CryptographicException ("算法 ID '" + this.algorithmID + "' 不支持。");
		}

	    internal byte[] InitVec {
			set {
				this.initVec = value;
			}
		}
	}
}