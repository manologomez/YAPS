using System.Collections.Generic;
using System.IO;
using Yaps.Library;

namespace Yaps.Cmd {
	/// <summary>
	/// Loads the master configuration file
	/// </summary>
	public class ConfigManager {

		public string configFileName = "yaps_config.json";
		public IList<string> Errors;

		public ConfigManager() {
			Errors = new List<string>();
		}

		public void GenerateEmptyFile() {
			var template = new YapsConfigExtra();
			template.Appearance = new SignatureAppearance();
			template.Metadata = GetDefaultMetadata();
			template.Timestamp = new TsaConfig();
			var json = JsonSerializerEx.Serialize(template, true);
			File.WriteAllText(configFileName, json);
		}

		public YapsConfigExtra LoadAndValidate() {
			var json = File.ReadAllText(configFileName);
			var config = JsonSerializerEx.Deserialize<YapsConfigExtra>(json);
			Errors = config.CheckErrors();
			return config;
		}

		public static Dictionary<string, string> GetDefaultMetadata() {
			return new Dictionary<string, string>{
			                                     	{ "Author", "" },{ "Title", "" },
			                                     	{ "Subject", "" },{ "Keywords", "" },
			                                     	{ "Creator", "" },{ "Producer", "" }
			                                     };
		}

	}
}