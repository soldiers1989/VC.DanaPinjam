using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Encryption
{
	public class Decryptor
	{
	    private byte[] initVec;
	    private DecryptTransformer transformer;

    	public Decryptor (EncryptionAlgorithm algId)
		{
			transformer = new DecryptTransformer (algId);
		}

    	public byte[] DecryptNew(byte[] bytesData, byte[] bytesKey)
		{
			DESCryptoServiceProvider DES = new DESCryptoServiceProvider();
			DES.Mode = CipherMode.ECB;
			DES.Padding = PaddingMode.Zeros;
			ICryptoTransform DESDecrypt = DES.CreateDecryptor(bytesKey, initVec);
			return DESDecrypt.TransformFinalBlock(bytesData, 0, bytesData.Length);
		}

		public byte[] Decrypt(byte[] bytesData, byte[] bytesKey)
		{
			MemoryStream stream = new MemoryStream();
			transformer.InitVec = this.initVec;
			ICryptoTransform cryptoServiceProvider = this.transformer.GetCryptoServiceProvider(bytesKey);
			CryptoStream stream2 = new CryptoStream(stream, cryptoServiceProvider, CryptoStreamMode.Write);
			try
			{
				stream2.Write(bytesData, 0, bytesData.Length);
			}
			catch (Exception exception)
			{
				throw new Exception("����ʧ��" + exception.Message);
			}
			stream2.FlushFinalBlock();
			stream2.Close();
			return stream.ToArray();
		}

    	public byte[] InitVec {
			set {
				this.initVec = value;
			}
		}
	}
}