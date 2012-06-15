using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Yaps.Library;

namespace Yaps.Cmd {
	public class Controller {
		public YapsConfig config;
		public IPdfSigner Signer { get; set; }
		public bool AllDirectories { get; set; }
		public event Action<string> OnLog;

		StringBuilder sb = new StringBuilder();

		public Controller(YapsConfig config) {
			this.config = config;
		}

		public IPdfSigner GetSigner() {
			return PdfSignerFactory.GetSigner(config);
		}

		public int Start() {
			sb = new StringBuilder();
			Signer = GetSigner();
			if (!Directory.Exists(config.InputFolder)) {
				Console.WriteLine("Source Directory does not exists!");
				return 0;
			}

			Signer.Initialize();

			if (!Directory.Exists(config.OutputFolder))
				Directory.CreateDirectory(config.OutputFolder);

			Console.WriteLine("Reading Files");

			var searchOption = AllDirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

			var files = Directory.GetFiles(config.InputFolder, "*.pdf", searchOption);
			if (files.Length == 0) {
				Console.WriteLine("No files no process!");
				return 0;
			}
			if (config.Async) {
				// HACK: fugly hack, will be better in following versions
				OnLog += x => {
					lock (this) {
						sb.AppendLine(x);
					}
				};
				var n = ProcessAsync(files);
				Console.WriteLine(sb.ToString());
				return n;
			}
			int num = 0;
			foreach (var file in files) {
				var start = DateTime.Now;
				try {
					var filename = Path.GetFileNameWithoutExtension(file);
					var extension = Path.GetExtension(file);
					Console.Write(string.Format("{0}{1}", filename, extension));
					filename = string.Format("{0}{1}{2}{3}", config.Prefix, filename, config.Suffix, extension);
					var path = Path.GetDirectoryName(file).Replace(config.InputFolder, config.OutputFolder);
					if (!Directory.Exists(path))
						Directory.CreateDirectory(path);
					var destPath = Path.Combine(path, filename);
					if (File.Exists(destPath))
						File.Delete(destPath);
					var info = new FileInfo(destPath);
					if (!IsFileValid(info)) // check for repetitions
						Signer.ProcessFile(file, destPath);
				} catch (Exception ex) {
					Console.WriteLine(ex);
				}
				var end = DateTime.Now - start;
				Console.WriteLine(" : {0} sec", end.TotalSeconds);
				num++;
			}
			return num;
		}

		protected void Log(string txt) {
			if (OnLog != null) {
				OnLog(txt);
			}
		}

		protected int ProcessAsync(string[] files) {
			int[] num = { 0 };
			Parallel.ForEach(files, file => {
				var start = DateTime.Now;
				var filename = Path.GetFileNameWithoutExtension(file);
				var extension = Path.GetExtension(file);
				filename = string.Format("{0}{1}{2}{3}", config.Prefix, filename, config.Suffix, extension);
				try {
					var path = Path.GetDirectoryName(file).Replace(config.InputFolder, config.OutputFolder);
					if (!Directory.Exists(path))
						Directory.CreateDirectory(path);
					var destPath = Path.Combine(path, filename);
					if (File.Exists(destPath))
						File.Delete(destPath);
					var info = new FileInfo(destPath);
					if (!IsFileValid(info)) // check for repetitions
						Signer.ProcessFile(file, destPath);
					var end = DateTime.Now - start;
					Log(string.Format("{0} : {1}", filename, end.TotalSeconds));
				} catch (Exception ex) {
					Log(string.Format("ERROR\t {0} : {1}", filename, ex.Message));
				}
				Interlocked.Increment(ref num[0]);
			});
			return num[0];
		}

		public bool IsFileValid(FileInfo info) {
			return info.Exists && info.Length > 0;
		}

	}

	public class HashUtil {

		static readonly MD5 Crypto = MD5.Create();
		private static CultureInfo ci = CultureInfo.InvariantCulture;

		public static string GetMd5(string text) {
			if (string.IsNullOrEmpty(text))
				return "";
			byte[] data = Encoding.ASCII.GetBytes(text);
			data = Crypto.ComputeHash(data);
			var sb = new StringBuilder();
			for (int i = 0; i < data.Length; i++)
				sb.Append(data[i].ToString("x2").ToUpperInvariant());
			return sb.ToString();
		}

		public static string GetMd5File(string path) {
			var f = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
			byte[] data = Crypto.ComputeHash(f);
			f.Close();
			var sb = new StringBuilder();
			for (int i = 0; i < data.Length; i++)
				sb.Append(data[i].ToString("x2").ToUpperInvariant());
			return sb.ToString();
		}

		public static string GetHashFileInfo(FileInfo info) {
			var sb = new StringBuilder();
			sb.AppendLine(info.CreationTimeUtc.ToString(ci));
			sb.AppendLine(info.FullName);
			sb.AppendLine(info.LastWriteTimeUtc.ToString(ci));
			sb.AppendLine(info.Length.ToString(ci));
			return GetMd5(sb.ToString());
		}

	}

	// objects for activity log

	public class SignerSession {
		public YapsConfig Config { get; set; }
		public DateTime Start { get; set; }
		public DateTime End { get; set; }
		public string Estado { get; set; }

		public IList<FileTask> Files { get; set; }
		public SignerSession() {
			Files = new List<FileTask>();
		}

	}

	public class FileTask {
		public string InputPath { get; set; }
		public string OutputPath { get; set; }
		public string InputHash { get; set; }
		public string OutputHash { get; set; }
		public decimal TotalTimeMs { get; set; }
		public string Estado { get; set; }
		public string Error { get; set; }

		public FileTask() {
			Estado = "pendiente";
		}
	}



}