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
			case PickMethod.Bm3d:
				//NOTE: This algorithm is patented and is excluded in this configuration;
				// Set OPENCV_ENABLE_NONFREE CMake option and rebuild the library
				return null; // new Bm3d.Start();
			}
			return null;
		}
	}
}