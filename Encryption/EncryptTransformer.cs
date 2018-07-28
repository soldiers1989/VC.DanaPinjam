using System;
using System.Security.Cryptography;

namespace Encryption
{
	public class EncryptTransformer
	{
	    private EncryptionAlgorithm algorithmID;
	    private byte[] encKey;
	    private byte[] initVec;

    	internal EncryptTransformer (EncryptionAlgorithm algId)
		{
			this.algorithmID = algId;
		}

    	internal ICryptoTransform GetCryptoServiceProvider (byte[] bytesKey)
		{
			DES des;
			switch (this.algorithmID) {
			case EncryptionAlgorithm.Des:
				des = new DESCryptoServiceProvider ();
				des.Mode = CipherMode.CBC;
				if (bytesKey != null) {
					des.Key = bytesKey;
					this.encKey = des.Key;
					break;
				}
				this.encKey = des.Key;
				break;
			case EncryptionAlgorithm.Rc2:
				{
					RC2 rc = new RC2CryptoServiceProvider ();
					rc.Mode = CipherMode.CBC;
					if (bytesKey != null) {
						rc.Key = bytesKey;
						this.encKey = rc.Key;
					} else {
						this.encKey = rc.Key;
					}
					if (this.initVec == null) {
						this.initVec = rc.IV;
					} else {
						rc.IV = this.initVec;
					}
					return rc.CreateEncryptor ();
				}
			case EncryptionAlgorithm.Rijndael:
				{
					Rijndael rijndael = new RijndaelManaged ();
					rijndael.Mode = CipherMode.CBC;
					if (bytesKey != null) {
						rijndael.Key = bytesKey;
						this.encKey = rijndael.Key;
					} else {
						this.encKey = rijndael.Key;
					}
					if (this.initVec == null) {
						this.initVec = rijndael.IV;
					} else {
						rijndael.IV = this.initVec;
					}
					return rijndael.CreateEncryptor ();
				}
			case EncryptionAlgorithm.TripleDes:
				{
					TripleDES edes = new TripleDESCryptoServiceProvider ();
					edes.Mode = CipherMode.CBC;
					if (bytesKey != null) {
						edes.Key = bytesKey;
						this.encKey = edes.Key;
					} else {
						this.encKey = edes.Key;
					}
					if (this.initVec == null) {
						this.initVec = edes.IV;
					} else {
						edes.IV = this.initVec;
					}
					return edes.CreateEncryptor ();
				}
			default:
				throw new CryptographicException ("算法 ID '" + this.algorithmID + "' 不受支持。");
			}
			if (this.initVec == null) {
				this.initVec = des.IV;
			} else {
				des.IV = this.initVec;
			}
			return des.CreateEncryptor ();
		}

    	internal byte[] InitVec {
			get {
				return this.initVec;
			}
			set {
				this.initVec = value;
			}
		}

    	internal byte[] EncKey {
			get {
				return this.encKey;
			}
		}
	}
}

