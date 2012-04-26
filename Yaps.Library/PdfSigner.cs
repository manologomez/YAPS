using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;
using iTextSharp.text.pdf;

namespace Yaps.Library {
	/// <summary>
	/// Simple logic to sign PDFs using iText. Based on the "official" code available at: http://itextpdf.sourceforge.net/howtosign.html
	/// TODO: smartcard support, further checks
	/// </summary>
	public class PdfSigner {
		public YapsConfig config;
		public CertificateReader CertReader { get; set; }

		public PdfSigner(YapsConfig configuration) {
			config = configuration;
			CertReader = new CertificateReader();
			CertReader.ProcessCert(config.CertificateFile, config.CertificatePassword);
		}

		public void ProcessFile(string infile, string outfile) {
			PdfReader reader = new PdfReader(infile);
			FileStream fs = new FileStream(outfile, FileMode.Create, FileAccess.Write);
			PdfStamper st = PdfStamper.CreateSignature(reader, fs, '\0');
			PdfSignatureAppearance sap = st.SignatureAppearance;

			var meta = config.Metadata;
			if (meta != null && meta.Count > 0) {
				//st.MoreInfo = new Hashtable(meta);
				st.MoreInfo = meta;
				st.XmpMetadata = config.GetStreamedMetaData();
			}

			//sap.SetCrypto(this.myCert.Akp, this.myCert.Chain, null, PdfSignatureAppearance.WINCER_SIGNED);
			var appearance = config.Appearance ?? new SignatureAppearance();
			sap.SetCrypto(null, CertReader.Chain, null, PdfSignatureAppearance.WINCER_SIGNED);
			sap.Reason = appearance.Reason;
			sap.Contact = appearance.Contact;
			sap.Location = appearance.Location;

			if (config.Visible && appearance.ValidateRect()) {
				//iTextSharp.text.Rectangle rect = st.Reader.GetPageSize(sigAP.Page);
				var xi = appearance.X + appearance.Width;
				var yi = appearance.Y + appearance.Height;
				var rect = new iTextSharp.text.Rectangle(appearance.X, appearance.Y, xi, yi);
				//sap.Image = sigAP.RawData == null ? null : iTextSharp.text.Image.GetInstance(sigAP.RawData);
				if (!string.IsNullOrEmpty(appearance.CustomText))
					sap.Layer2Text = appearance.CustomText;
				//sap.SetVisibleSignature(new iTextSharp.text.Rectangle(100, 100, 300, 200), 1, "Signature");
				sap.SetVisibleSignature(rect, appearance.Page, "Signature");
			}

			/////
			PdfSignature dic = new PdfSignature(PdfName.ADOBE_PPKLITE, new PdfName("adbe.pkcs7.detached"));
			dic.Reason = sap.Reason;
			dic.Location = sap.Location;
			dic.Contact = sap.Contact;
			dic.Date = new PdfDate(sap.SignDate);
			sap.CryptoDictionary = dic;

			int contentEstimated = 15000;
			// Preallocate excluded byte-range for the signature content (hex encoded)
			var exc = new Dictionary<PdfName, int>();
			//var exc = new Hashtable();
			exc[PdfName.CONTENTS] = contentEstimated * 2 + 2;
			sap.PreClose(exc);

			PdfPKCS7 sgn = new PdfPKCS7(CertReader.Akp, CertReader.Chain, null, "SHA1", false);
			IDigest messageDigest = DigestUtilities.GetDigest("SHA1");
			//Stream data = sap.RangeStream;
			Stream data = sap.GetRangeStream();

			byte[] buf = new byte[8192];
			int n;
			while ((n = data.Read(buf, 0, buf.Length)) > 0) {
				messageDigest.BlockUpdate(buf, 0, n);
			}
			byte[] hash = new byte[messageDigest.GetDigestSize()];
			messageDigest.DoFinal(hash, 0);
			DateTime cal = DateTime.Now;
			byte[] ocsp = null;
			if (CertReader.Chain.Length >= 2) {
				String url = PdfPKCS7.GetOCSPURL(CertReader.Chain[0]);
				if (!string.IsNullOrEmpty(url))
					ocsp = new OcspClientBouncyCastle().GetEncoded(CertReader.Chain[0], CertReader.Chain[1], url);
					//ocsp = new OcspClientBouncyCastle(CertReader.Chain[0], CertReader.Chain[1], url).GetEncoded();
			}
			byte[] sh = sgn.GetAuthenticatedAttributeBytes(hash, cal, ocsp);
			sgn.Update(sh, 0, sh.Length);

			byte[] paddedSig = new byte[contentEstimated];

			if (config.Stamp) {
				ITSAClient tsc = GetTsaClient();
				byte[] encodedSigTsa = sgn.GetEncodedPKCS7(hash, cal, tsc, ocsp);
				System.Array.Copy(encodedSigTsa, 0, paddedSig, 0, encodedSigTsa.Length);
				if (contentEstimated + 2 < encodedSigTsa.Length)
					throw new Exception("Not enough space for signature");
			} else {
				byte[] encodedSig = sgn.GetEncodedPKCS7(hash, cal);
				System.Array.Copy(encodedSig, 0, paddedSig, 0, encodedSig.Length);
				if (contentEstimated + 2 < encodedSig.Length)
					throw new Exception("Not enough space for signature");
			}

			PdfDictionary dic2 = new PdfDictionary();

			dic2.Put(PdfName.CONTENTS, new PdfString(paddedSig).SetHexWriting(true));
			sap.Close(dic2);
		}

		protected ITSAClient GetTsaClient() {
			var tsa = config.Timestamp;
			var tsc = new TSAClientEx(tsa.Url, tsa.Username, tsa.Password);
			if (!string.IsNullOrEmpty(tsa.ProxyAddress)) {
				tsc.ProxyAddress = tsa.ProxyAddress;
				tsc.ProxyUsername = tsa.ProxyUsername;
				tsc.ProxyPassword = tsa.ProxyPassword;
			}
			return tsc;
		}

	}


}