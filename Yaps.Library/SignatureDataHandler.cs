using System;
using iTextSharp.text.pdf;

namespace Yaps.Library {
	/// <summary>
	/// Utility functions to set metadata and visual information on the signature
	/// </summary>
	public class SignatureDataHandler {

		public static void SetMetadata(YapsConfig config, PdfStamper stamper) {
			var meta = config.Metadata;
			if (meta == null || meta.Count <= 0) return;
			//st.MoreInfo = new Hashtable(meta);
			stamper.MoreInfo = meta;
			stamper.XmpMetadata = config.GetStreamedMetaData();
		}

		public static void SetAppearance(YapsConfig config, PdfSignatureAppearance sap) {
			var appearance = config.Appearance ?? new SignatureAppearance();
			sap.Reason = appearance.Reason;
			sap.Contact = appearance.Contact;
			sap.Location = appearance.Location;
			sap.SignDate = DateTime.Now;
			sap.Acro6Layers = true;
			if (!config.Visible || !appearance.ValidateRect())
				return;

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

	}
}