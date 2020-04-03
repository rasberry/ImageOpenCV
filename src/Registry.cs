using System;

namespace ImageOpenCV
{
	public static class Registry
	{
		public static IMain Map(PickMethod method)
		{
			switch(method)
			{
			case PickMethod.NlMeans:
				return new NlMeans.Start();
			case PickMethod.NlMeansColored:
				return new NlMeansColored.Start();
			case PickMethod.Dct:
				return new Dct.Start();
			case PickMethod.TVL1:
				return new Tvl1.Start();
			case PickMethod.DFTForward:
				return null;
			case PickMethod.DFTInverse:
				return null;
			case PickMethod.Prida:
				return new Prida.Start();
			}
			return null;
		}
	}
}