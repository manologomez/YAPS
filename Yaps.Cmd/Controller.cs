using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Yaps.Library;

namespace Yaps.Cmd {
	public class Controller {
		public YapsConfig config;
		public PdfSigner Signer { get; set; }

		public event Action<string> OnLog;

		StringBuilder sb = new StringBuilder();

		public Controller(YapsConfig config) {
			this.config = config;
		}

		public int Start() {
			sb = new StringBuilder();
			Signer = new PdfSigner(config);
			if (!Directory.Exists(config.InputFolder)) {
				Console.WriteLine("Source Directory does not exists!");
				return 0;
			}

			if (!Directory.Exists(config.OutputFolder))
				Directory.CreateDirectory(config.OutputFolder);

			Console.WriteLine("Reading Files");
			var files = Directory.GetFiles(config.InputFolder, "*.pdf");
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
				var filename = Path.GetFileName(file);
				Console.Write(filename);
				try {
					var outfilename = string.Format("{0}{1}", config.Prefix, filename);
					var outfile = Path.Combine(config.OutputFolder, outfilename);
					if (File.Exists(outfile))
						File.Delete(outfile);
					Signer.ProcessFile(file, outfile);
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
				var filename = Path.GetFileName(file);
				try {
					var start = DateTime.Now;
					var outfilename = string.Format("{0}{1}", config.Prefix, filename);
					var outfile = Path.Combine(config.OutputFolder, outfilename);
					if (File.Exists(outfile))
						File.Delete(outfile);
					Signer.ProcessFile(file, outfile);
					var end = DateTime.Now - start;
					Log(string.Format("{0} : {1}", filename, end.TotalSeconds));
				} catch (Exception ex) {
					Log(string.Format("ERROR\t {0} : {1}", filename, ex.Message));
				}
				Interlocked.Increment(ref num[0]);
			});
			return num[0];
		}

	}
}