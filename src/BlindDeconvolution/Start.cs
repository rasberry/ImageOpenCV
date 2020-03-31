using System;
using System.Drawing;
using System.Text;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
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

			if (imgData.NumberOfChannels == 1) {
				CvInvoke.CvtColor(imgData,imgData,ColorConversion.Gray2Rgb);
			}
			var (outImg,outKernel) = helper(imgData);

			Log.Message("Saving "+O.Dst);
			outImg.Save(O.Dst);
			Log.Message("Saving "+O.DstKernel);
			outKernel.Save(O.DstKernel);
		}


		/***********************************************
		* @param image The input image
		* Create structs uk and params
		* Set the niter number to 1000
		* Call blind_deconv
		* Write out the results to the folder
		***********************************************/
		(Mat,Mat) helper(Mat image)
		{
			var uk = new uk_t();
			var @params = new params_t();

			@params.MK = O.KernelSize; // row
			@params.NK = O.KernelSize; // col
			@params.niters = 1000;

			blind_deconv(image, O.Lambda, @params, ref uk);

			Mat tmpk = new Mat(), tmpu = new Mat();
			uk.u.ConvertTo(tmpu, DepthType.Cv8U, 1.0*255.0);
			double ksml = 0, klag = 0;
			Point psml = Point.Empty, plag = Point.Empty;
			CvInvoke.MinMaxLoc(uk.k, ref ksml, ref klag, ref psml, ref plag);

			tmpk = uk.k / klag;
			uk.k.ConvertTo(tmpk, DepthType.Cv8U, 1.0*255.0);
			CvInvoke.ApplyColorMap(tmpk,tmpk,ColorMapType.Bone);

			return (tmpu,tmpk);
		}

		/********************************************************************************************
		* @param f The input image. lambda The input lambda. params The kernel size and niter size
		*        uk The output image and kernel
		* Convert the image to double precision
		* Adjust the photo size
		* Initialize parameters
		* Call coarseToFine
		*********************************************************************************************/
		void blind_deconv(Mat f, double lambda, params_t @params, ref uk_t uk)
		{
			f.ConvertTo(f,DepthType.Cv64F, 1.0/255.0);
			int rpad = 0;
			int cpad = 0;
			if (f.Rows % 2 == 0) { rpad = 1; }
			if (f.Cols % 2 == 0) { cpad = 1; }
			Mat f3 = new Mat(f,new Rectangle(0,0,f.Rows - rpad,f.Cols - cpad));
			var ctf_params = new ctf_params_t {
				lambdaMultiplier = 1.9,
				maxLambda = 1.1e-1,
				finalLambda = lambda,
				kernelSizeMultiplier = 1.0
			};
			coarseToFine(f3,@params,ctf_params,ref uk);
		}

		/***********************************************************************************************
		* @param f The input image. blind_params The kernel size and niters size. params Parameters
		*        uk The output image and kernel
		* Call buildPyrmaid
		* For each layer in the pyrmaid, call Prida
		**********************************************************************************************/
		void coarseToFine(Mat f, params_t blind_params, ctf_params_t @params, ref uk_t uk)
		{
			double MK = blind_params.MK;
			double NK = blind_params.NK;

			Mat u = new Mat();
			int top = (int)Math.Floor(MK/2);
			int left = (int)Math.Floor(NK/2);
			CvInvoke.CopyMakeBorder(f,u,top,top,left,left,BorderType.Replicate);

			var k = Mat.Ones((int)MK,(int)NK,DepthType.Cv64F,1);
			k = k / MK / NK;

			var data = new input {
				MK = MK,
				NK = NK,
				lambda = @params.finalLambda,
				lambdaMultiplier = @params.lambdaMultiplier,
				scaleMultiplier = @params.kernelSizeMultiplier,
				largestLambda = @params.maxLambda,
				f = f
			};
			var answer = buildPyramid(data);

			for (int i = answer.scales-1; i >=0; i--) {
				double Ms, Ns, MKs, NKs, lambda;
				Mat fs;

				Ms = answer.Mp[i];
				Ns = answer.Np[i];

				MKs = answer.MKp[i];
				NKs = answer.NKp[i];
				fs = answer.fp[i];

				lambda = answer.lambdas[i];

				CvInvoke.Resize(u,u,new Size((int)(Ns - NKs - 1), (int)(Ms + MKs - 1))); //,0,0,Inter.Linear);
				CvInvoke.Resize(k,k,new Size((int)NKs, (int)MKs)); //,0,0,Inter.Linear);

				k = k / CvInvoke.Sum(k).V0;
				blind_params.MK = MKs;
				blind_params.NK = NKs;

				prida(fs, ref u, ref k, lambda, blind_params);
				Console.WriteLine($"Working on Scale: {i+1} with lambda = {data.lambda} with pyramid_lambda = {lambda} and Kernel size {MKs}");
			}

			uk.u = u;
			uk.k = k;
		}

		/*****************************************************************************************************************
		* @param data Including input image, kernel size, lambda, lambdaMultiplier, scaleMultiplier and largestLambda
		*        answer The dest struct stores all the result
		****************************************************************************************************************/
		output buildPyramid(input data)
		{
			double smallestScale = 3;
			int scales = 1;
			double mkpnext = data.MK;
			double nkpnext = data.NK;
			double lamnext = data.lambda;

			int M = data.f.Height;
			int N = data.f.Width;

			while (mkpnext > smallestScale
				&& nkpnext > smallestScale
				&& lamnext * data.lambdaMultiplier < data.largestLambda
			) {
				scales = scales + 1;
				double lamprev = lamnext;
				double mkpprev = mkpnext;
				double nkpprev = nkpnext;

				// Compute lambda value for the current scale
				lamnext = lamprev * data.lambdaMultiplier;
				mkpnext = Math.Round(mkpprev / data.scaleMultiplier);
				nkpnext = Math.Round(nkpprev / data.scaleMultiplier);

				// Makes kernel dimension odd
				if (mkpnext % 2 == 0) { mkpnext = mkpnext - 1; }
				if (nkpnext % 2 == 0) { nkpnext = nkpnext - 1; }
				if (nkpnext == nkpprev) { nkpnext = nkpnext - 2; }
				if (mkpnext == mkpprev) { mkpnext = mkpnext - 2; }
				if (nkpnext < smallestScale) { nkpnext = smallestScale; }
				if (mkpnext < smallestScale) { mkpnext = smallestScale; }
			}

			var answer = new output();
			answer.fp = new Mat[scales];
			answer.Mp = new double[scales];
			answer.Np = new double[scales];
			answer.MKp = new double[scales];
			answer.NKp = new double[scales];
			answer.lambdas = new double[scales];

			//set the first (finest level) of pyramid to original data
			answer.fp[0] = data.f;
			answer.Mp[0] = M;
			answer.Np[0] = N;
			answer.MKp[0] = data.MK;
			answer.NKp[0] = data.NK;
			answer.lambdas[0] = data.lambda;

			//loop and fill the rest of the pyramid
			for (int s = 1 ; s <scales; s++) {
				answer.lambdas[s] = answer.lambdas[s - 1] * data.lambdaMultiplier;

				answer.MKp[s] = Math.Round(answer.MKp[s - 1] / data.scaleMultiplier);
				answer.NKp[s] = Math.Round(answer.NKp[s - 1] / data.scaleMultiplier);

				// Makes kernel dimension odd
				if (answer.MKp[s] % 2 == 0) { answer.MKp[s] = answer.MKp[s] - 1; }
				if (answer.NKp[s] % 2 == 0) { answer.NKp[s] -= 1; }
				if (answer.NKp[s] == answer.NKp[s-1]) { answer.NKp[s] -= 2; }
				if (answer.MKp[s] == answer.MKp[s-1]) { answer.MKp[s] -= 2; }
				if (answer.NKp[s] < smallestScale) { answer.NKp[s] = smallestScale; }
				if (answer.MKp[s] < smallestScale) { answer.MKp[s] = smallestScale; }

				//Correct scaleFactor for kernel dimension correction
				double factorM = answer.MKp[s-1]/answer.MKp[s];
				double factorN = answer.NKp[s-1]/answer.NKp[s];

				answer.Mp[s] = Math.Round(answer.Mp[s-1] / factorM);
				answer.Np[s] = Math.Round(answer.Np[s-1] / factorN);

				// Makes image dimension odd
				if (answer.Mp[s] % 2 == 0) { answer.Mp[s] -= 1; }
				if (answer.Np[s] % 2 == 0) { answer.Np[s] -= 1; }

				Mat dst = new Mat();
				CvInvoke.Resize(data.f,dst,new Size((int)answer.Np[s],(int)answer.Mp[s])); //,0,0,Inter.Linear);
				answer.fp[s] = dst;
			}

			answer.scales = scales;
			return answer;
		}

		/***********************************************************************************
		* @param f The scaled input image. u The scaled result image.
		*        k The scaled result kernel. lambda The input lambda. params Parameters.
		*        uk The output image and kernel.
		* Initialize gradu and gradk
		* Loop for niters times, call conv2 and gradTVCC to get the result of u and k.
		**********************************************************************************/
		void prida(Mat f, ref Mat u, ref Mat k, double lambda, params_t @params)
		{
			for (int i = 0; i < @params.niters; i++) {
				Mat gradu = Mat.Zeros(
					f.Cols + (int)@params.NK - 1,
					f.Rows + (int)@params.MK - 1,
					DepthType.Cv64F, 3
				);
				int c = 0;
				VectorOfMat
					pGradu = new VectorOfMat(),
					pf = new VectorOfMat(),
					pu = new VectorOfMat();

				CvInvoke.Split(gradu,pGradu);
				CvInvoke.Split(f, pf);
				CvInvoke.Split(u, pu);
				while (c < f.NumberOfChannels) {
					Mat tmp = conv2(pu[c], k, ConvolutionType.VALID);
					tmp = tmp - pf[c];
					Mat rotk = Mat.Zeros(k.Width,k.Height,DepthType.Cv64F,1);
					CvInvoke.Rotate(k, rotk, RotateFlags.Rotate180);
					pGradu[c].SetTo(conv2(tmp, rotk, ConvolutionType.FULL));
					c++;
				}
				CvInvoke.Merge(pGradu,gradu);
				c = 0;

				Mat gradTV = Mat.Zeros(u.Width,u.Height,DepthType.Cv64F,1);
				gradTVcc(u,ref gradTV);
				gradu = (gradu - lambda * gradTV);

				double minValu = 0;
				double maxValu = 0;
				MinMaxLoc(u, ref minValu, ref maxValu);

				double minValgu = 0;
				double maxValgu = 0;
				MinMaxLoc(Abs(gradu), ref minValgu, ref maxValgu);

				double sf = 1e-3 * maxValu / Math.Max(1e-31, maxValgu);
				Mat u_new = u - sf * gradu;
				Mat gradk = Mat.Zeros(k.Width,k.Height,DepthType.Cv64F,1);

				VectorOfMat
					pff = new VectorOfMat(),
					puu = new VectorOfMat();
				CvInvoke.Split(f, pff);
				CvInvoke.Split(u, puu);

				while (c < f.NumberOfChannels) {
					Mat subconv2 = conv2(puu[c], k, ConvolutionType.VALID);
					subconv2 = subconv2 - pff[c];

					Mat rotu = new Mat();
					CvInvoke.Rotate(puu[c],rotu, RotateFlags.Rotate180);

					Mat majconv2 = conv2(rotu, subconv2, ConvolutionType.VALID);
					gradk = gradk + majconv2;
					c++;
				}

				double minValk = 0, maxValk = 0;
				MinMaxLoc(k, ref minValk, ref maxValk);
				double minValgk = 0, maxValgk = 0;
				MinMaxLoc(Abs(gradk), ref minValgk, ref maxValgk);

				double sh = 1e-3 * maxValk / Math.Max(1e-31, maxValgk);
				double eps = double.Epsilon;
				Mat etai = sh / (k + eps);

				int bigM = 1000;
				Mat expTmp = new Mat();
				CvInvoke.Multiply(0 - etai, gradk, expTmp);
				CvInvoke.Exp(expTmp,expTmp);

				Mat tmp2 = new Mat();
				Mat tmpbigM = InitFrom(expTmp,bigM);
				CvInvoke.Min(expTmp, tmpbigM, tmp2);
				Mat MDS = new Mat();
				CvInvoke.Multiply(k,tmp2,MDS);

				Mat k_new = MDS / CvInvoke.Sum(MDS).V0;

				u = u_new;
				k = k_new;
			}
		}

		static Mat Abs(Mat mat)
		{
			Mat copy = new Mat();
			var zeros = Mat.Zeros(mat.Width,mat.Height,mat.Depth,mat.NumberOfChannels);
			CvInvoke.AbsDiff(mat, zeros, copy);
			return copy;
		}

		static void MinMaxLoc(IInputArray arr, ref double min, ref double max)
		{
			Point minP = Point.Empty;
			Point maxP = Point.Empty;
			CvInvoke.MinMaxLoc(arr, ref min, ref max, ref minP, ref maxP);
		}

		static Mat InitFrom(Mat f, double? value = null)
		{
			var m = new Mat(f.Width,f.Height,f.Depth,f.NumberOfChannels);
			if (value.HasValue) {
				m.SetTo(new MCvScalar(value.Value));
			}
			return m;
		}

		/**********************************************************************************************
		* @param img The input img, Kernel The input kernel, type FULL or VALID, dest The output result
		* Compute conv2 by calling filter2D.
		************************************************************************************************/
		Mat conv2(Mat img, Mat kernel, ConvolutionType type)
		{
			Mat flipped_kernel = new Mat();
			CvInvoke.Flip(kernel, flipped_kernel, FlipType.Horizontal | FlipType.Vertical);

			Point pad;
			Mat padded;

			switch( type ) {
			case ConvolutionType.VALID:
				padded = img;
				pad = new Point(kernel.Cols - 1, kernel.Rows - 1);
				break;
			case ConvolutionType.FULL:
				pad = new Point(kernel.Cols - 1, kernel.Rows - 1);
				padded = new Mat(
					img.Rows + 2*(kernel.Rows - 1),
					img.Cols + 2*(kernel.Cols - 1),
					img.Depth, img.NumberOfChannels
				);
				padded.SetTo(new ScalarArray(0));
				img.CopyTo(new Mat(padded,new Rectangle(kernel.Rows - 1, kernel.Cols - 1, img.Cols, img.Rows)));
				break;
			default:
				throw new NotSupportedException("Unsupported convolutional shape");
			}
			var region = new Rectangle( pad.X / 2, pad.Y / 2, padded.Cols - pad.X, padded.Rows - pad.Y);
			Mat dest = new Mat();
			CvInvoke.Filter2D(padded, dest, flipped_kernel, new Point(-1, -1), 0, BorderType.Constant);
			dest = new Mat(dest,region);
			return dest;
		}

		/***********************************************************************************
		* @param f The scaled input image. dest The result image.
		* Calc total variation for image f
		**********************************************************************************/
		void gradTVcc(Mat f, ref Mat dest)
		{
			Mat fxforw = InitFrom(f);
			new Mat(f,new Range(1,f.Rows), new Range(0,f.Cols)).CopyTo(fxforw);
			CvInvoke.CopyMakeBorder(fxforw,fxforw,0,1,0,0,BorderType.Replicate);
			fxforw = fxforw - f;

			Mat fyforw = InitFrom(f);
			new Mat(f,new Range(0,f.Rows), new Range(1,f.Cols)).CopyTo(fyforw);
			CvInvoke.CopyMakeBorder(fyforw,fyforw,0,0,0,1,BorderType.Replicate);
			fyforw = fyforw - f;

			Mat fxback = InitFrom(f);
			new Mat(f,new Range(0,f.Rows-1),new Range(0,f.Cols)).CopyTo(fxback);
			CvInvoke.CopyMakeBorder(fxback,fxback,1,0,0,0,BorderType.Replicate);

			Mat fyback = InitFrom(f);
			new Mat(f,new Range(0,f.Rows),new Range(0,f.Cols-1)).CopyTo(fyback);
			CvInvoke.CopyMakeBorder(fyback,fyback,0,0,1,0,BorderType.Replicate);

			Mat fxmixd = InitFrom(f);
			new Mat(f,new Range(1,f.Rows),new Range(0,f.Cols-1)).CopyTo(fxmixd);
			CvInvoke.CopyMakeBorder(fxmixd,fxmixd,0,1,1,0,BorderType.Replicate);
			fxmixd = fxmixd - fyback;

			Mat fymixd = InitFrom(f);
			new Mat(f,new Range(0,f.Rows-1),new Range(1,f.Cols)).CopyTo(fymixd);
			CvInvoke.CopyMakeBorder(fymixd,fymixd,1,0,0,1,BorderType.Replicate);
			fymixd = fymixd - fxback;
			fyback = f - fyback;
			fxback = f - fxback;

			dest = Mat.Zeros(f.Width,f.Height,DepthType.Cv64F,f.NumberOfChannels);

			var pfxforw = fxforw.Split();
			var pfyforw = fyforw.Split();
			var pfxback = fxback.Split();
			var pfyback = fyback.Split();
			var pfxmixd = fxmixd.Split();
			var pfymixd = fymixd.Split();
			var pdest = dest.Split();

			int c = 0;
			while (c < f.NumberOfChannels) {

				Mat powfx = new Mat();
				CvInvoke.Pow(pfxforw[c],2,powfx);
				Mat powfy = new Mat();
				CvInvoke.Pow(pfyforw[c],2,powfy);

				Mat sqtforw = new Mat();
				CvInvoke.Sqrt(powfx + powfy, sqtforw);

				Mat powfxback = new Mat();
				CvInvoke.Pow(pfxback[c],2,powfxback);
				Mat powfymixd = new Mat();
				CvInvoke.Pow(pfymixd[c],2,powfymixd);

				Mat sqtmixed = new Mat();
				CvInvoke.Sqrt(powfymixd + powfxback, sqtmixed);

				Mat powfxmixd = new Mat();
				CvInvoke.Pow(pfxmixd[c],2,powfxmixd);
				Mat powfyback = new Mat();
				CvInvoke.Pow(pfyback[c],2,powfyback);

				Mat sqtback = new Mat();
				CvInvoke.Sqrt(powfxmixd + powfyback,sqtback);

				Mat max1 = new Mat();
				Mat maxsqtforw = 1e-3 * Mat.Ones(sqtforw.Width,sqtforw.Height,sqtforw.Depth,sqtforw.NumberOfChannels);
				CvInvoke.Max(sqtforw, maxsqtforw, max1);

				Mat max2 = new Mat();
				Mat maxsqtmixed = 1e-3 * Mat.Ones(sqtmixed.Width,sqtmixed.Height,sqtmixed.Depth,sqtmixed.NumberOfChannels);
				CvInvoke.Max(sqtmixed, maxsqtmixed, max2);

				Mat max3 = new Mat();
				Mat maxsqtback = 1e-3 * Mat.Ones(sqtback.Width,sqtback.Height,sqtback.Depth,sqtback.NumberOfChannels);
				CvInvoke.Max(sqtback, maxsqtback, max3);

				Mat pmax1 = new Mat();
				CvInvoke.Divide(pfxforw[c] + pfyforw[c],max1,pmax1);
				pdest[c].SetTo(pmax1);

				Mat pmax2 = new Mat();
				CvInvoke.Divide(pfxback[c],max2,pmax2);
				pdest[c].SetTo(pdest[c] - pmax2);

				Mat pmax3 = new Mat();
				CvInvoke.Divide(pfyback[c],max3,pmax3);
				pdest[c].SetTo(pdest[c] - pmax3);

				c++;
			}

			CvInvoke.Merge(new VectorOfMat(pdest),dest);
		}
	}
}
