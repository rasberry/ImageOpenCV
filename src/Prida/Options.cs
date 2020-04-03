using System;
using System.IO;
using System.Text;

namespace ImageOpenCV.Prida
{
	public class Options
	{
		public const PickMethod Which = PickMethod.Prida;

		public void Usage(StringBuilder sb)
		{
			string name = Aids.MethodName(Which);

			sb
				.WL()
				.WL(0,$"{name} [options] (input image) [output image]")
				.WL(1,"Provably Robust Image Deconvolution Algorithm, a image deblurring algorithm that implements blind deconvolution")
				.WL(0,"Options:")
				.WL(1,"-l (double | 0.0006)" ,"Tunable regularization parameter.  Higher values of lambda will encourage more smoothness in the optimal sharp image")
				.WL(1,"-k (int | 19)"        ,"Kernel size in pixels")
				.WL(1,"-i (int | 1000)"      ,"Number of iterations");
			;
		}

		public string Src;
		public string Dst;
		public string DstKernel;
		public double Lambda;
		public int KernelSize;
		public int Iterations;

		public bool Parse(string[] args)
		{
			var p = new Params(args);

			if (p.Default("-l",out Lambda,6e-4).IsInvalid()) { return false; }
			if (p.Default("-k",out KernelSize,19).IsInvalid()) { return false; }
			if (p.Default("-i",out Iterations,1000).IsInvalid()) { return false; }
			if (p.Expect(out Src,"input image").IsBad()) { return false; }
			if (p.Has(out Dst).IsBad()) {
				Dst = Aids.GetOutputName(Which);
				var root = Path.Combine(
					Path.GetDirectoryName(Dst),
					Path.GetFileNameWithoutExtension(Dst)
				);
				DstKernel = root + "-kernel" + Path.GetExtension(Dst);
			}
			return true;
		}

	}
}
