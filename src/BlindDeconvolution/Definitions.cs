using System.Collections.Generic;
using Emgu.CV;

namespace ImageOpenCV.BlindDeconvolution
{
	public struct ctf_params_t {
		public double lambdaMultiplier;
		public double maxLambda;
		public double finalLambda, kernelSizeMultiplier;
	};

	public struct uk_t {
		public Mat u;
		public Mat k;
	};

	public struct params_t {
		public double MK;
		public double NK;
		public double niters;
	};

	public struct input {
		public Mat f;
		public double MK;
		public double NK;
		public double lambda;
		public double lambdaMultiplier;
		public double scaleMultiplier;
		public double largestLambda;
	};

	public struct output{
		public List<Mat> fp;
		public List<double> Mp, Np, MKp, NKp, lambdas;
		public int scales;
	} ;

	public enum ConvolutionType {
		FULL,
		VALID
	};
}