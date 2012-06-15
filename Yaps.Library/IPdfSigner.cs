namespace Yaps.Library{
	public interface IPdfSigner{
		void Initialize();
		void ProcessFile(string infile, string outfile);
	}
}