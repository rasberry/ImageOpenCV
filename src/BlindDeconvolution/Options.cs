using System;
using System.IO;
using System.Text;

namespace ImageOpenCV.BlindDeconvolution
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
				.WL(1,"-l (double)"   ,"Lambda parameter")
				.WL(1,"-k (int)"      ,"Kernel size parameter")
			;
		}

		public string Src;
		public string Dst;
		public string DstKernel;
		public double Lambda;
		public int KernelSize;
		// public string PATH;

		public bool Parse(string[] args)
		{
			var p = new Params(args);

			if (p.Expect("-l",out Lambda).IsBad()) { return false; }
			if (p.Expect("-k",out KernelSize).IsBad()) { return false; }
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
