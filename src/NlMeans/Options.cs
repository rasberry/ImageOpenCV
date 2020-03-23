using System;
using System.Text;

namespace ImageOpenCV.NlMeans
{
	public class Options
	{
		const PickMethod Which = PickMethod.NlMeans;

		public void Usage(StringBuilder sb)
		{
			string name = Aids.MethodName(Which);

			sb
				.WL()
				.WL(0,$"{name} [options] (input image) [output image]")
				.WL(1,"Perform image denoising using Non-local Means Denoising algorithm: http://www.ipol.im/pub/algo/bcm_non_local_means_denoising/ with several computational optimizations. Noise expected to be a gaussian white noise.")
				.WL(0,"Options:")
				.WL(1,"-h (number {3.0})","Parameter regulating filter strength. Big h value perfectly removes noise but also removes image details, smaller h value preserves details but also preserves some noise.")
				.WL(1,"-t (number {7})"  ,"Size in pixels of the template patch that is used to compute weights. Should be odd.")
				.WL(1,"-s (number {21})" ,"Size in pixels of the window that is used to compute weighted average for given pixel. Should be odd. Affect performance linearly: greater searchWindowsSize - greater denoising time.")
			;
		}

		public string Src;
		public string Dst;
		public double H = 3.0;
		public int TemplateWindowSize = 7;
		public int SearchWindowSize = 21;

		public bool Parse(string[] args)
		{
			int len = args.Length;
			for(int a=0; a<len; a++)
			{
				string curr = args[a];

				if (curr == "-h" && ++a < len) {
					if (!Aids.TryParse(args[a],out double h)) {
						return false;
					}
					H = h;
				}
				else if (curr == "-t" && ++a < len) {
					if (!Aids.TryParse(args[a],out int templateWindowSize)) {
						return false;
					}
					TemplateWindowSize = templateWindowSize;
				}
				else if (curr == "-s" && ++a < len) {
					if (!Aids.TryParse(args[a],out int searchWindowSize)) {
						return false;
					}
					SearchWindowSize = searchWindowSize;
				}
				else {
					if (Src == null) { Src = curr; }
					else if (Dst == null) { Dst = curr; }
				}

				if (String.IsNullOrWhiteSpace(Src)) {
					Tell.MustProvideInput("input image");
					return false;
				}
				if (String.IsNullOrWhiteSpace(Dst)) {
					Dst = Aids.GetOutputName(Which);
				}
			}
			return true;
		}

	}
}
