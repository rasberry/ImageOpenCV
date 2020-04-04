using System;
using System.Text;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.XPhoto;

namespace ImageOpenCV.Bm3d
{
	public class Start : IMain
	{
		Options O = new Options();

		public bool ParseArgs(string[] args)
		{
			return O.Parse(args);
		}

		public void Usage(StringBuilder sb)
		{
			O.Usage(sb);
		}

		public void Main()
		{
			Log.Message("Reading image "+O.Src);
			var imgData = CvInvoke.Imread(O.Src,ImreadModes.AnyColor);
			var outData = new Mat(imgData.Size,imgData.Depth,imgData.NumberOfChannels);

			Log.Message("Denoising using "+Options.Which);
			XPhotoInvoke.Bm3dDenoising(imgData,outData,
				O.FilterStregth,O.TWindow,O.SWindow,
				O.Block1,O.Block2,O.GroupSize,O.SlidingStep,
				O.Beta,MapNorm(O.PickNorm)
			);

			Log.Message("Saving "+O.Dst);
			outData.Save(O.Dst);
		}

		static NormType MapNorm(NormWrap n)
		{
			if (n == NormWrap.L1) {
				return NormType.L1;
			}
			if (n == NormWrap.L2) {
				return NormType.L2;
			}

			throw new ArgumentException("Invalid NormType");
		}
	}
}