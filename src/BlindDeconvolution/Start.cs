using System;
using System.Drawing;
using System.Text;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.XPhoto;

// https://github.com/tianyishan/Blind_Deconvolution

namespace ImageOpenCV.BlindDeconvolution
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

			O.Channel = imgData.NumberOfChannels;
			var (outImg,outKernel) = helper(imgData);

			Log.Message("Saving "+O.Dst);
			outImg.Save(O.Dst);
			Log.Message("Saving "+O.DstKernel);
			outKernel.Save(O.DstKernel);
		}

		(Mat,Mat) helper(Mat image)
		{
			var uk = new uk_t();
			var @params = new params_t();

			@params.MK = O.KernelSize; // row
			@params.NK = O.KernelSize; // col
			@params.niters = 1000;

			blind_deconv(image, O.Lambda, @params, uk);

			Mat tmpk = null, tmpu = null;
			uk.u.ConvertTo(tmpu, DepthType.Cv8U, 1.0*255.0);
			double ksml = 0, klag = 0;
			Point psml = Point.Empty, plag = Point.Empty;
			CvInvoke.MinMaxLoc(uk.k, ref ksml, ref klag, ref psml, ref plag);

			tmpk = uk.k / klag;
			uk.k.ConvertTo(tmpk, DepthType.Cv8U, 1.0*255.0);
			CvInvoke.ApplyColorMap(tmpk,tmpk,ColorMapType.Bone);

			return (tmpu,tmpk);
		}
	}
}
