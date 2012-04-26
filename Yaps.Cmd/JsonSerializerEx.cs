using System.Web.Script.Serialization;

namespace Yaps.Cmd {
	/// <summary>
	/// Simple wrapper around default JavascriptSerializer with formatting (indenting) option
	/// </summary>
	public class JsonSerializerEx {
		public static JavaScriptSerializer Serializer = new JavaScriptSerializer();

		public static string Serialize(object obj, bool indent = false) {
			var json = Serializer.Serialize(obj);
			if (indent) {
				var formatter = new JsonFormatter(json);
				return formatter.Format();
			}
			return json;
		}

		public static T Deserialize<T>(string json) {
			return Serializer.Deserialize<T>(json);
		}
		public static object Deserialize(string json) {
			return Serializer.DeserializeObject(json);
		}
	}
}