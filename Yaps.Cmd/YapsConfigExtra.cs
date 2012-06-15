using Yaps.Library;

namespace Yaps.Cmd{
	/// <summary>
	/// Extended config just fot this example
	/// </summary>
	public class YapsConfigExtra : YapsConfig {
		public bool BypassSSLCheck { get; set; }
		public bool AllDirectories { get; set; }
	}
}