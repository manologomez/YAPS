using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Org.BouncyCastle.Cms;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.X509;

namespace Yaps.Library {
	/// <summary>
	/// Signs using the configuration and a token
	/// As always, code from http://itextpdf.sourceforge.net/howtosign.html#signextitextsharp2
	/// </summary>
	public class TokenSigner : IPdfSigner {
		public YapsConfig Config { get; set; }
		public Org.BouncyCastle.X509.X509Certificate[] Chain { get; set; }
		private X509Certificate2 Card;
		private byte[] otrosbytes;
		public bool Detached { get; set; }

		public TokenSigner(YapsConfig config) {
			Config = config;
		}

		public void Initialize() {
			// TODO lidiar cuando el usuario cancela el caudro de dialogo o se equivoca en la clave del token
			var tempcard = GetCertificate();
			otrosbytes = tempcard.Export(X509ContentType.SerializedCert);
			Card = new X509Certificate2(otrosbytes);
			Org.BouncyCastle.X509.X509CertificateParser cp = new Org.BouncyCastle.X509.X509CertificateParser();
			Chain = new[] { cp.ReadCertificate(Card.RawData) };
		}

		public void ProcessFile(string infile, string outfile) {
			if (Detached)
				SignDetached(infile, outfile);
			else SignHashed(infile, outfile);
		}

		public void SignDetached(string infile, string outfile) {
			if (Chain == null)
				throw new ApplicationException("Certificate chain has not been initialized");

			PdfReader reader = new PdfReader(infile);
			PdfStamper stp = PdfStamper.CreateSignature(reader, new FileStream(outfile, FileMode.Create), '\0');
			PdfSignatureAppearance sap = stp.SignatureAppearance;

			SignatureDataHandler.SetMetadata(Config, stp);
			sap.SetCrypto(null, Chain, null, PdfSignatureAppearance.WINCER_SIGNED);
			SignatureDataHandler.SetAppearance(Config, sap);

			//sap.Render = PdfSignatureAppearance.SignatureRender.NameAndDescription;
			PdfSignature dic = new PdfSignature(PdfName.ADOBE_PPKLITE, PdfName.ADBE_PKCS7_DETACHED);
			dic.Name = PdfPKCS7.GetSubjectFields(Chain[0]).GetField("CN");
			dic.Reason = sap.Reason;
			dic.Location = sap.Location;
			dic.Contact = sap.Contact;
			dic.Date = new PdfDate(sap.SignDate);
			sap.CryptoDictionary = dic;

			int csize = 15000; // was 10000
			Dictionary<PdfName, int> exc = new Dictionary<PdfName, int>();
			exc[PdfName.CONTENTS] = csize * 2 + 2;
			sap.PreClose(exc);

			Stream s = sap.GetRangeStream();
			MemoryStream ss = new MemoryStream();
			int read = 0;
			byte[] buff = new byte[8192];
			while ((read = s.Read(buff, 0, 8192)) > 0) {
				ss.Write(buff, 0, read);
			}

			var card = new X509Certificate2(otrosbytes);
			byte[] pk = SignMsg(ss.ToArray(), card, true);

			byte[] outc = new byte[csize];

			PdfDictionary dic2 = new PdfDictionary();

			Array.Copy(pk, 0, outc, 0, pk.Length);

			dic2.Put(PdfName.CONTENTS, new PdfString(outc).SetHexWriting(true));
			sap.Close(dic2);
		}

		public void SignHashed(string infile, string outfile) {
			if (Chain == null)
				throw new ApplicationException("Certificate chain has not been initialized");

			PdfReader reader = new PdfReader(infile);
			PdfStamper stp = PdfStamper.CreateSignature(reader, new FileStream(outfile, FileMode.Create), '\0');
			PdfSignatureAppearance sap = stp.SignatureAppearance;

			SignatureDataHandler.SetMetadata(Config, stp);
			sap.SetCrypto(null, Chain, null, PdfSignatureAppearance.WINCER_SIGNED);
			SignatureDataHandler.SetAppearance(Config, sap);

			//sap.SetVisibleSignature(new Rectangle(100, 100, 300, 200), 1, null);
			//sap.SignDate = DateTime.Now;
			//sap.SetCrypto(null, Chain, null, null);
			//sap.Reason = "I like to sign";
			//sap.Location = "Universe";
			//sap.Acro6Layers = true;

			//sap.Render = PdfSignatureAppearance.SignatureRender.NameAndDescription;
			PdfSignature dic = new PdfSignature(PdfName.ADOBE_PPKMS, PdfName.ADBE_PKCS7_SHA1);
			/*dic.Date = new PdfDate(sap.SignDate);
			dic.Name = PdfPKCS7.GetSubjectFields(Chain[0]).GetField("CN");
			if (sap.Reason != null)
				dic.Reason = sap.Reason;
			if (sap.Location != null)
				dic.Location = sap.Location;
			sap.CryptoDictionary = dic;*/
			dic.Name = PdfPKCS7.GetSubjectFields(Chain[0]).GetField("CN");
			dic.Reason = sap.Reason;
			dic.Location = sap.Location;
			dic.Contact = sap.Contact;
			dic.Date = new PdfDate(sap.SignDate);
			sap.CryptoDictionary = dic;


			int csize = 15000; // was 4000
			Dictionary<PdfName, int> exc = new Dictionary<PdfName, int>();
			exc[PdfName.CONTENTS] = csize * 2 + 2;
			sap.PreClose(exc);

			HashAlgorithm sha = new SHA1CryptoServiceProvider();

			Stream s = sap.GetRangeStream();
			int read = 0;
			byte[] buff = new byte[8192];
			while ((read = s.Read(buff, 0, 8192)) > 0) {
				sha.TransformBlock(buff, 0, read, buff, 0);
			}
			sha.TransformFinalBlock(buff, 0, 0);

			var card = new X509Certificate2(otrosbytes);
			byte[] pk = SignMsg(sha.Hash, card, false);

			byte[] outc = new byte[csize];

			PdfDictionary dic2 = new PdfDictionary();

			Array.Copy(pk, 0, outc, 0, pk.Length);

			dic2.Put(PdfName.CONTENTS, new PdfString(outc).SetHexWriting(true));
			sap.Close(dic2);
		}


		//  Sign the message with the private key of the signer.
		public byte[] SignMsg(Byte[] msg, X509Certificate2 signerCert, bool detached) {
			//  Place message in a ContentInfo object.
			//  This is required to build a SignedCms object.
			ContentInfo contentInfo = new ContentInfo(msg);

			//  Instantiate SignedCms object with the ContentInfo above.
			//  Has default SubjectIdentifierType IssuerAndSerialNumber.
			SignedCms signedCms = new SignedCms(contentInfo, detached);

			//  Formulate a CmsSigner object for the signer.
			CmsSigner cmsSigner = new CmsSigner(signerCert);

			// Include the following line if the top certificate in the
			// smartcard is not in the trusted list.
			cmsSigner.IncludeOption = X509IncludeOption.EndCertOnly;

			//  Sign the CMS/PKCS #7 message. The second argument is
			//  needed to ask for the pin.

			signedCms.ComputeSignature(cmsSigner, false);

			// TODO: Here the user can fail the password or cancel...what to do?

			//  Encode the CMS/PKCS #7 message.
			byte[] bb = signedCms.Encode();
			//return bb here if no timestamp is to be applied
			if (!Config.Stamp)
				return bb;

			CmsSignedData sd = new CmsSignedData(bb);
			SignerInformationStore signers = sd.GetSignerInfos();
			byte[] signature = null;
			SignerInformation signer = null;
			foreach (SignerInformation signer_ in signers.GetSigners()) {
				signer = signer_;
				break;
			}
			signature = signer.GetSignature();
			Org.BouncyCastle.Asn1.Cms.AttributeTable at = new Org.BouncyCastle.Asn1.Cms.AttributeTable(GetTimestamp(signature));
			signer = SignerInformation.ReplaceUnsignedAttributes(signer, at);
			IList signerInfos = new ArrayList();
			signerInfos.Add(signer);
			sd = CmsSignedData.ReplaceSigners(sd, new SignerInformationStore(signerInfos));
			bb = sd.GetEncoded();
			return bb;
		}

		public Asn1EncodableVector GetTimestamp(byte[] signature) {
			var conf = Config.Timestamp;
			var tsc = new TSAClientEx(conf.Url, conf.Username, conf.Password);
			tsc.ProxyAddress = conf.ProxyAddress;
			tsc.ProxyPassword = conf.ProxyPassword;
			tsc.ProxyUsername = conf.ProxyUsername;

			byte[] tsImprint = PdfEncryption.DigestComputeHash("SHA1", signature);

			//ITSAClient tsc = new TSAClientBouncyCastle("http://tsa.net", null, null);
			//return tsc.GetTimeStampToken(null, tsImprint);
			String ID_TIME_STAMP_TOKEN = "1.2.840.113549.1.9.16.2.14"; // RFC 3161 id-aa-timeStampToken

			Asn1InputStream tempstream = new Asn1InputStream(new MemoryStream(tsc.GetTimeStampToken(tsImprint)));

			Asn1EncodableVector unauthAttributes = new Asn1EncodableVector();

			Asn1EncodableVector v = new Asn1EncodableVector();
			v.Add(new DerObjectIdentifier(ID_TIME_STAMP_TOKEN)); // id-aa-timeStampToken
			Asn1Sequence seq = (Asn1Sequence)tempstream.ReadObject();
			v.Add(new DerSet(seq));

			unauthAttributes.Add(new DerSequence(v));
			return unauthAttributes;
		}

		public X509Certificate2 GetCertificate() {
			X509Store st = new X509Store(StoreName.My, StoreLocation.CurrentUser);
			st.Open(OpenFlags.ReadOnly);
			X509Certificate2Collection col = st.Certificates;
			X509Certificate2 card = null;
			X509Certificate2Collection sel = X509Certificate2UI.SelectFromCollection(col, "Certificates", "Select one to sign", X509SelectionFlag.SingleSelection);
			if (sel.Count > 0) {
				X509Certificate2Enumerator en = sel.GetEnumerator();
				en.MoveNext();
				card = en.Current;
			}
			st.Close();
			return card;
		}

	}
}
