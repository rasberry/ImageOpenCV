using System;
using System.Collections.Generic;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.Structure;

namespace ImageOpenCV.BlindDeconvolution
{
	public static class Helpers
	{
		public static Mat Abs(Mat mat)
		{
			Mat copy = new Mat();
			var zeros = Mat.Zeros(mat.Height,mat.Width,mat.Depth,mat.NumberOfChannels);
			CvInvoke.AbsDiff(mat, zeros, copy);
			return copy;
		}

		public static void MinMaxLoc(IInputArray arr, ref double min, ref double max)
		{
			CvInvoke.MinMaxIdx(arr,out double minv,out double maxv,null,null);
			//CvInvoke.MinMaxLoc(arr, ref minv, ref maxv, ref minP, ref maxP); //crashes
			min = minv; max = maxv;
		}

		public static Mat InitFrom(Mat f, double? value = null)
		{
			var m = new Mat(f.Size,f.Depth,f.NumberOfChannels);
			if (value.HasValue) {
				m.SetTo(new MCvScalar(value.Value));
			}
			return m;
		}

		public static void LogMat(Mat m, string prefix = "")
		{
			Log.Debug($"{prefix} Mat {MatDebug(m)}");
		}

		public static string MatDebug(Mat m)
		{
			if (m == null) { return "null"; }
			return $"[{m.Width}x{m.Height},{m.Depth},{m.NumberOfChannels}]";
		}

	}
}