using System;
using System.Text;

namespace ImageOpenCV.NlMeansColored
{
	public class Options
	{
		public const PickMethod Which = PickMethod.NlMeansColored;

		public void Usage(StringBuilder sb)
		{
			string name = Aids.MethodName(Which);

			sb
				.WL()
				.WL(0,$"{name} [options] (input image) [output image]")
				.WL(1,"Perform image denoising using Non-local Means Denoising algorithm (modified for color image): http://www.ipol.im/pub/algo/bcm_non_local_means_denoising/ with several computational optimizations. Noise expected to be a gaussian white noise. The function converts image to CIELAB colorspace and then separately denoise L and AB components with given h parameters using fastNlMeansDenoising function.")
				.WL(0,"Options:")
				.WL(1,"-h (float | 3.0)","Parameter regulating filter strength. Big h value perfectly removes noise but also removes image details, smaller h value preserves details but also preserves some noise.")
				.WL(1,"-c (float | 3.0)","The same as -h but for color components. For most images a value of 10 will be enough to remove colored noise and not distort colors.")
				.WL(1,"-t (int | 7)"    ,"Size in pixels of the template patch that is used to compute weights. Should be odd.")
				.WL(1,"-s (int | 21)"   ,"Size in pixels of the window that is used to compute weighted average for given pixel. Should be odd. Affects performance linearly: greater searchWindowsSize -> greater denoising time.")
			;
		}

		public string Src;
		public string Dst;
		public double H = 3.0;
		public double HColor = 0.0;
		public int TemplateWindowSize = 7;
		public int SearchWindowSize = 21;

		public bool Parse(string[] args)
		{
			var p = new Params(args);

			if (!p.Default("-h",out H,3.0).IsInvalid()) { return false; }
			if (!p.Default("-c",out HColor,3.0).IsInvalid()) { return false; }
			if (!p.Default("-t",out TemplateWindowSize,7).IsInvalid()) { return false; }
			if (!p.Default("-s",out SearchWindowSize,21).IsInvalid()) { return false; }
			if (!p.Expect(out Src,"input image").IsBad()) { return false; }
			if (!p.Has(out Dst).IsBad()) {
				Dst = Aids.GetOutputName(Which);
			}
			return true;
		}

	}
}
