using System.IO;
using System.Linq;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.X509;

namespace Yaps.Library {
	/// <summary>
	/// Extracts a certificate chain and AsymmetricKeyParameter from a Pkcs12 file
	/// </summary>
	public class CertificateReader {

		public AsymmetricKeyParameter Akp { get; set; }
		public X509Certificate[] Chain { get; set; }

		public void ProcessCert(string certificado, string password) {
			//First we'll read the certificate file
			Stream fs = new FileStream(certificado, FileMode.Open, FileAccess.Read);
			Pkcs12Store pk12 = new Pkcs12Store(fs, (password ?? "").ToCharArray());
			
			//then Iterate throught certificate entries to find the private key entry
			/*foreach (string al in pk12.Aliases) {
				if (pk12.IsKeyEntry(al) && pk12.GetKey(al).Key.IsPrivate) {
					alias = al;
					break;
				}
			}*/

			string alias = pk12.Aliases.Cast<string>().FirstOrDefault(al => pk12.IsKeyEntry(al) && pk12.GetKey(al).Key.IsPrivate);

			//IEnumerator i = pk12.Aliases.GetEnumerator();
			//while (i.MoveNext())
			//{
			//    alias = ((string)i.Current);
			//    if (pk12.IsKeyEntry(alias))
			//        break;
			//}
			fs.Close();

			Akp = pk12.GetKey(alias).Key;
			X509CertificateEntry[] ce = pk12.GetCertificateChain(alias);
			//X509Certificate[] chain = new Org.BouncyCastle.X509.X509Certificate[ce.Length];
			Chain = new Org.BouncyCastle.X509.X509Certificate[ce.Length];
			for (int k = 0; k < ce.Length; ++k)
				Chain[k] = ce[k].Certificate;
		}

	}
}