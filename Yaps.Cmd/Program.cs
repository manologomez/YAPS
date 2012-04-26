using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Yaps.Cmd {
	class Program {
		static void Main(string[] args) {
			var cosa = new Program();
			try {
				cosa.Start();
			} catch (Exception ex) {
				Console.WriteLine(">>> ERROR:");
				//throw ex;
				Console.WriteLine(ex);
			} finally {
				Console.Write("Press any key");
				Console.Read();
			}
		}

		public void EndBang(string error) {
			Console.WriteLine(error);
			Console.WriteLine("<< GAME OVER >>");
			//Console.Read();
		}

		public void Start() {
			Console.WriteLine("YET ANOTHER PDF SIGNER for .NET");
			Console.WriteLine("<< INSERT COIN >>");
			Console.WriteLine();
			Console.WriteLine("Reading config");
			var configManager = new ConfigManager();
			if (!File.Exists(configManager.configFileName)) {
				EndBang("Configuration File not found, creating an empty one and closing");
				configManager.GenerateEmptyFile();
				return;
			}

			var config = configManager.LoadAndValidate();
			if (configManager.Errors.Count > 0) {
				Console.WriteLine("There are errors in the configuration file:");
				foreach (var error in configManager.Errors) {
					Console.WriteLine("- " + error);
				}
				EndBang("");
				return;
			}

			if (config.BypassSSLCheck) {
				ServicePointManager.ServerCertificateValidationCallback += ValidateRemoteCertificate;
			}

			Console.WriteLine("Starting process");
			var watch = new Stopwatch();
			watch.Start();
			var proc = new Controller(config);
			proc.Start();
			watch.Stop();
			Console.WriteLine("Done in " + watch.Elapsed.TotalSeconds + " seconds, press any key to exit");
			Console.Read();
		}

		private static bool ValidateRemoteCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors policyErrors) {
			return true;
		}
	}


}
