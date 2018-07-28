using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Encryption
{
	public class Encryptor
	{
    	private byte[] encKey;
    	private byte[] initVec;
    	private EncryptTransformer transformer;

    	public Encryptor (EncryptionAlgorithm algId)
		{
			this.transformer = new EncryptTransformer (algId);
		}

    	public byte[] EncryptNew(byte[] bytesData, byte[] bytesKey)
		{

			DESCryptoServiceProvider DES = new DESCryptoServiceProvider();
			DES.Key = bytesKey;
			DES.Mode = CipherMode.ECB;
			DES.Padding = PaddingMode.Zeros;
			ICryptoTransform DESEncrypt = DES.CreateEncryptor();
			this.initVec = DES.IV;
			return DESEncrypt.TransformFinalBlock(bytesData, 0, bytesData.Length);
			//return Convert.ToBase64String(DESEncrypt.TransformFinalBlock(bytesData, 0, bytesData.Length));

			//MemoryStream stream = new MemoryStream ();
			//transformer.InitVec = initVec;
			//DESCryptoServiceProvider esc = new DESCryptoServiceProvider();
			//esc.Key = bytesKey;
			//esc.IV = bytesKey;
			////ICryptoTransform cryptoServiceProvider = this.transformer.GetCryptoServiceProvider (bytesKey);
			//CryptoStream stream2 = new CryptoStream (stream, esc.get, CryptoStreamMode.Write);
			//try {
			//    stream2.Write (bytesData, 0, bytesData.Length);
			//} catch (Exception exception) {
			//    throw new Exception ("将加密数据写入流时出错： \n" + exception.Message);
			//}
			//encKey = transformer.EncKey;
			//initVec = transformer.InitVec;
			//stream2.FlushFinalBlock ();
			//stream2.Close ();
			//return stream.ToArray ();
		}

		public byte[] Encrypt(byte[] bytesData, byte[] bytesKey)
		{
			MemoryStream stream = new MemoryStream();
			transformer.InitVec = initVec;
			ICryptoTransform cryptoServiceProvider = this.transformer.GetCryptoServiceProvider(bytesKey);
			CryptoStream stream2 = new CryptoStream(stream, cryptoServiceProvider, CryptoStreamMode.Write);
			try
			{
				stream2.Write(bytesData, 0, bytesData.Length);
			}
			catch (Exception exception)
			{
				throw new Exception("将加密数据写入流时出错" + exception.Message);
			}
			encKey = transformer.EncKey;
			initVec = transformer.InitVec;
			stream2.FlushFinalBlock();
			stream2.Close();
			return stream.ToArray();
		}

    	public byte[] InitVec {
			get {
				return this.initVec;
			}
			set {
				this.initVec = value;
			}
		}

    	public byte[] EncKey {	
			get {
				return this.encKey;
			}
		}
	}
}

