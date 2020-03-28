using System;
using System.Text;

namespace ImageOpenCV.Dct
{
	public class Options
	{
		public const PickMethod Which = PickMethod.Dct;

		public void Usage(StringBuilder sb)
		{
			string name = Aids.MethodName(Which);

			sb
				.WL()
				.WL(0,$"{name} [options] (input image) [output image]")
				.WL(1,"The function implements simple DCT-based (Discrete Cosine Transform) denoising, link: http://www.ipol.im/pub/art/2011/ys-dct/.")
				.WL(0,"Options:")
				.WL(1,"-s (double)"   ,"Expected noise standard deviation.")
				.WL(1,"-p (int | 16)" ,"Size of block side where dct is computed.")
			;
		}

		public string Src;
		public string Dst;
		public double Sigma;
		public int BlockSize;

		public bool Parse(string[] args)
		{
			var p = new Params(args);

			if (p.Default("-p",out BlockSize,16).IsInvalid()) { return false; }
			if (p.Expect("-s",out Sigma).IsBad()) { return false; }
			if (p.Expect(out Src,"input image").IsBad()) { return false; }
			if (p.Has(out Dst).IsBad()) {
				Dst = Aids.GetOutputName(Which);
			}
			return true;
		}

	}
}
