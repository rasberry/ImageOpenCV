using System;
using System.Text;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.XPhoto;

namespace ImageOpenCV.Dct
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
			XPhotoInvoke.DctDenoising(imgData,outData,O.Sigma,O.BlockSize);

			Log.Message("Saving "+O.Dst);
			outData.Save(O.Dst);
		}
	}
}