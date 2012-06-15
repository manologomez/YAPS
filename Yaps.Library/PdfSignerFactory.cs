namespace Yaps.Library {
	public class PdfSignerFactory {
		public static IPdfSigner GetSigner(YapsConfig config) {
			if (!config.UseSmartcard)
				return new PdfSigner(config);
			return new TokenSigner(config);
		}
	}
}