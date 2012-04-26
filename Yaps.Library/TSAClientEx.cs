using System;
using System.IO;
using System.Net;
using System.Text;
using System.util;
using iTextSharp.text.pdf;
using iTextSharp.text.error_messages;

namespace Yaps.Library {
	/// <summary>
	/// Extended TSA Client based on the regular bouncycastle client that includes native proxy server support
	/// </summary>
	public class TSAClientEx : TSAClientBouncyCastle {
		public TSAClientEx(string url) : base(url) { }
		public TSAClientEx(string url, string username, string password) : base(url, username, password) { }
		public TSAClientEx(string url, string username, string password, int tokSzEstimate, string digestAlgorithm) : base(url, username, password, tokSzEstimate, digestAlgorithm) { }

		public string ProxyAddress { get; set; }
		public string ProxyUsername { get; set; }
		public string ProxyPassword { get; set; }

		protected override byte[] GetTSAResponse(byte[] requestBytes) {
			var con = (HttpWebRequest)WebRequest.Create(tsaURL);
			con.ContentLength = requestBytes.Length;
			con.ContentType = "application/timestamp-query";
			con.Method = "POST";
			if (!string.IsNullOrEmpty(tsaUsername)) {
				var authFormat = string.Format("{0}:{1}", tsaUsername, tsaPassword);
				var authInfo = Convert.ToBase64String(Encoding.Default.GetBytes(authFormat));
				con.Headers["Authorization"] = "Basic " + authInfo;
				var cred = new NetworkCredential(tsaUsername, tsaPassword);
				con.Credentials = cred;
			}

			if (!string.IsNullOrEmpty(ProxyAddress)) {
				var proxy = new WebProxy(ProxyAddress);
				if (!string.IsNullOrEmpty(ProxyUsername)) {
					var proxyCred = new NetworkCredential(ProxyUsername, ProxyPassword);
					proxy.Credentials = proxyCred;
				}
				con.Proxy = proxy;
			}

			Stream outp = con.GetRequestStream();
			outp.Write(requestBytes, 0, requestBytes.Length);
			outp.Close();
			var response = (HttpWebResponse)con.GetResponse();
			if (response.StatusCode != HttpStatusCode.OK)
				throw new IOException(MessageLocalization.GetComposedMessage("invalid.http.response.1", (int)response.StatusCode));
				//throw new IOException("Invalid Http response: Code " + response.StatusCode);
			Stream inp = response.GetResponseStream();
			var baos = new MemoryStream();
			byte[] buffer = new byte[1024];
			int bytesRead = 0;
			while ((bytesRead = inp.Read(buffer, 0, buffer.Length)) > 0) {
				baos.Write(buffer, 0, bytesRead);
			}
			inp.Close();
			response.Close();
			var respBytes = baos.ToArray();
			var encoding = response.ContentEncoding;
			if (Util.EqualsIgnoreCase(encoding, "base64")) {
				respBytes = Convert.FromBase64String(Encoding.ASCII.GetString(respBytes));
			}
			return respBytes;

		}
	}
}