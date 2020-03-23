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
				return null;
			case PickMethod.Dct:
				return null;
			case PickMethod.TVL1:
				return null;
			case PickMethod.DFTForward:
				return null;
			case PickMethod.DFTInverse:
				return null;
			}
			return null;
		}
	}
}