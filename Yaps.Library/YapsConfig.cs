using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using iTextSharp.text.xml.xmp;

namespace Yaps.Library {
	/// <summary>
	/// Timestamp server specific options
	/// </summary>
	public class TsaConfig {
		public string Url { get; set; }
		public string Username { get; set; }
		public string Password { get; set; }

		public string ProxyAddress { get; set; }
		public string ProxyUsername { get; set; }
		public string ProxyPassword { get; set; }
	}

	/// <summary>
	/// Data about the signature appearance and position in the signed documents
	/// </summary>
	public class SignatureAppearance {
		public string CustomText { get; set; }
		public string Reason { get; set; }
		public string Contact { get; set; }
		public string Location { get; set; }
		public float X { get; set; }
		public float Y { get; set; }
		public float Width { get; set; }
		public float Height { get; set; }
		public int Page { get; set; }

		public SignatureAppearance() {
			Page = 1;
		}

		public bool ValidateRect() {
			return Page > 0 && X > 0 && Y > 0 && Width > 0 && Height > 0;
		}
	}

	/// <summary>
	/// General configuration options
	/// </summary>
	public class YapsConfig {
		public string InputFolder { get; set; }
		public string OutputFolder { get; set; }
		public string Prefix { get; set; }
		public string Suffix { get; set; }

		public string LogFile { get; set; }
		public string CertificateFile { get; set; }
		public string CertificatePassword { get; set; }
		public TsaConfig Timestamp { get; set; }

		public bool Visible { get; set; }
		public bool Stamp { get; set; }

		public Dictionary<string, string> Metadata { get; set; }
		public SignatureAppearance Appearance { get; set; }

		public bool Async { get; set; }
		public bool UseSmartcard { get; set; }

		public YapsConfig() {
			Metadata = new Dictionary<string, string>();
		}

		public byte[] GetStreamedMetaData() {
			var os = new MemoryStream();
			//var xmp = new XmpWriter(os, new Hashtable(Metadata));
			var xmp = new XmpWriter(os, Metadata);
			xmp.Close();
			return os.ToArray();
		}

		public IList<string> CheckErrors() {
			var errors = new List<string>();
			if (Val(InputFolder, x => !string.IsNullOrEmpty(x), "Source folder not specified", errors))
				Val(InputFolder, Directory.Exists, "Selected source folder does not exist", errors);
			Val(CertificateFile, x => !string.IsNullOrEmpty(x), "Select a .pfx certificate file", errors);
			//Val(CertificatePassword, x => !string.IsNullOrEmpty(x), "Especifique el password del certificado digital .pfx");
			if (Stamp && Timestamp != null) {
				Val(Timestamp.Url, x => !string.IsNullOrEmpty(x), "RFC 3161 Timestamp server URL not specified", errors);
			}
			return errors;
		}

		public bool Val<T>(T value, Func<T, bool> validator, string error, List<string> errors) {
			var bien = validator(value);
			if (!bien)
				errors.Add(error);
			return bien;
		}

	}
}