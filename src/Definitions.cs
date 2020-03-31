using System;
using System.Text;

namespace ImageOpenCV
{
	public enum PickMethod {
		None = 0,
		NlMeans = 1,
		NlMeansColored = 2,
		Dct = 3,
		TVL1 = 4,
		DFTForward = 5,
		DFTInverse = 6,
		Prida = 7
	}

	public interface IMain
	{
		void Usage(StringBuilder sb);
		bool ParseArgs(string[] args);
		void Main();
	}
}