using System;
using System.Text;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.XPhoto;

namespace ImageOpenCV.Tvl1
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
			var observations = new Mat[O.SrcArr.Length];
			for(int s = 0; s < O.SrcArr.Length; s++) {
				var img = O.SrcArr[s];
				Log.Message("Reading image "+img);
				var imgData = CvInvoke.Imread(img,ImreadModes.AnyColor);
				observations[s] = imgData;
			}

			var outData = new Mat();
			Log.Message("Denoising using "+Options.Which);
			CvInvoke.DenoiseTVL1(observations,outData,O.Lambda,O.Iterations);

			Log.Message("Saving "+O.Dst);
			outData.Save(O.Dst);
		}
	}
}