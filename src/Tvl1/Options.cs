using System;
using System.Text;

namespace ImageOpenCV.Tvl1
{
	public class Options
	{
		public const PickMethod Which = PickMethod.TVL1;

		public void Usage(StringBuilder sb)
		{
			string name = Aids.MethodName(Which);

			sb
				.WL()
				.WL(0,$"{name} [options] (input image) [input image] ...")
				.WL(1,"Denoise images using the Primal-dual algorithm. This algorithm is used for solving special types of variational problems (that is, finding a function to minimize some functional).")
				.WL(0,"Options:")
				.WL(1,"-o (string | *)"   ,"Output image. If not provided a default name will be generated.")
				.WL(1,"-l (double | 1.0)" ,"Lambda scaling parameter. As it is enlarged, the smooth (blurred) images are treated more favorably than detailed (but maybe more noised) ones. Roughly speaking, as it becomes smaller, the result will be more blur but more sever outliers will be removed.")
				.WL(1,"-n (int | 30)"     ,"Number of iterations that the algorithm will run. Of course, as more iterations as better, but it is hard to quantitatively refine this statement, so just use the default and increase it if the results are poor.")
			;
		}

		public string[] SrcArr;
		public string Dst;
		public double Lambda;
		public int Iterations;

		public bool Parse(string[] args)
		{
			var p = new Params(args);

			if (p.Default("-n",out Iterations,30).IsInvalid()) { return false; }
			if (p.Default("-l",out Lambda,1.0).IsInvalid()) { return false; }
			if (p.Default("-o",out Dst).IsBad()) {
				Dst = Aids.GetOutputName(Which);
			}
			SrcArr = p.Remaining();
			if (SrcArr.Length < 1) {
				Tell.MustProvideInput("one or more input images");
				return false;
			}
			return true;
		}

	}
}
