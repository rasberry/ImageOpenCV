using System;
using System.Text;
using Emgu.CV.CvEnum;
using Emgu.CV.XPhoto;

namespace ImageOpenCV.Bm3d
{
	public class Options
	{
		public const PickMethod Which = PickMethod.Bm3d;

		public void Usage(StringBuilder sb)
		{
			string name = Aids.MethodName(Which);

			sb
				.WL()
				.WL(0,$"{name} [options] (input image) [output image]")
				.WL(1,"Performs image denoising using the Block-Matching and 3D-filtering algorithm http://www.cs.tut.fi/~foi/GCF-BM3D/BM3D_TIP_2007.pdf with several computational optimizations. Noise expected to be a gaussian white noise.")
				.WL(0,"Options:")
				.WL(1,"-h (float | 1.0)"  ,"Filter strength. Big h value perfectly removes noise but also removes image details, smaller h value preserves details but also preserves some noise.")
				.WL(1,"-tw (int | 4)"     ,"Size in pixels of the template patch that is used for block-matching. Should be power of 2.")
				.WL(1,"-sw (int | 16)"    ,"Size in pixels of the window that is used to perform block-matching. Affect performance linearly: greater searchWindowsSize - greater denoising time. Must be larger than -tw. ")
				.WL(1,"-b1 (int | 2500)"  ,"Block matching threshold for the first step of BM3D (hard thresholding), i.e. maximum distance for which two blocks are considered similar. Value expressed in euclidean distance.")
				.WL(1,"-b2 (int | 400)"   ,"Block matching threshold for the second step of BM3D (Wiener filtering), i.e. maximum distance for which two blocks are considered similar. Value expressed in euclidean distance.")
				.WL(1,"-gs (int | 8)"     ,"Maximum size of the 3D group for collaborative filtering.")
				.WL(1,"-ss (int | 1)"     ,"Sliding step to process every next reference block.")
				.WL(1,"-k (float | 2.0)"  ,"Kaiser window parameter that affects the sidelobe attenuation of the transform of the window. Kaiser window is used in order to reduce border effects. To prevent usage of the window, set this to zero.")
				.WL(1,"-n (normType | L2)","Norm used to calculate distance between blocks. L2 is slower than L1 but yields more accurate results.")
				.WL(0,"NormTypes:")
				.PrintEnum<NormWrap>(1,false,GetNormDesc)
			;
		}

		static string GetNormDesc(NormWrap n)
		{
			if (n == NormWrap.L1) {
				return "Manhattan norm";
			}
			if (n == NormWrap.L2) {
				return "Euclidean norm";
			}
			return "";
		}

		public string Src;
		public string Dst;
		public float FilterStregth;
		public int TWindow;
		public int SWindow;
		public int Block1;
		public int Block2;
		public int GroupSize;
		public int SlidingStep;
		public float Beta;
		public NormWrap PickNorm;

		public bool Parse(string[] args)
		{
			var p = new Params(args);

			if (p.Default("-h",out FilterStregth, 1.0f).IsInvalid()) { return false; }
			if (p.Default("-tw",out TWindow, 4).IsInvalid()) { return false; }
			if (p.Default("-sw",out SWindow, 16).IsInvalid()) { return false; }
			if (p.Default("-b1",out SWindow, 2500).IsInvalid()) { return false; }
			if (p.Default("-b2",out SWindow, 400).IsInvalid()) { return false; }
			if (p.Default("-gs",out SWindow, 8).IsInvalid()) { return false; }
			if (p.Default("-ss",out SWindow, 1).IsInvalid()) { return false; }
			if (p.Default("-k",out Beta, 2.0f).IsInvalid()) { return false; }
			if (p.Default("-n",out PickNorm, NormWrap.L2).IsInvalid()) { return false; }

			if (p.Expect(out Src,"input image").IsBad()) { return false; }
			if (p.Has(out Dst).IsBad()) {
				Dst = Aids.GetOutputName(Which);
			}
			return true;
		}
	}
}
