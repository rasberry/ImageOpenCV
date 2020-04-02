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
			//Log.Debug($"helper f={Helpers.MatDebug(image)}");
			//var uk = new uk_t();
			var @params = new params_t();

			@params.MK = O.KernelSize; // row
			@params.NK = O.KernelSize; // col
			@params.niters = O.Iterations;

			var (u,k) = blind_deconv(image, O.Lambda, @params);

			Mat tmpk = new Mat(), tmpu = new Mat();
			u.ConvertTo(tmpu, DepthType.Cv8U, 1.0*255.0);
			double ksml = 0, klag = 0;
			Point psml = Point.Empty, plag = Point.Empty;
			CvInvoke.MinMaxLoc(k, ref ksml, ref klag, ref psml, ref plag);

			tmpk = k / klag;
			k.ConvertTo(tmpk, DepthType.Cv8U, 1.0*255.0);
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
		(Mat,Mat) blind_deconv(Mat f, double lambda, params_t @params)
		{
			//Log.Debug($"blind_deconv f={Helpers.MatDebug(f)} uk.k={Helpers.MatDebug(uk.k)} uk.u={Helpers.MatDebug(uk.u)}");
			f.ConvertTo(f,DepthType.Cv64F, 1.0/255.0);
			int rpad = 0;
			int cpad = 0;
			if (f.Rows % 2 == 0) { rpad = 1; }
			if (f.Cols % 2 == 0) { cpad = 1; }
			Mat f3 = new Mat(f,new Range(0,f.Rows-rpad),new Range(0,f.Cols-cpad));
			//Helpers.LogMat(f3,"f3");
			var ctf_params = new ctf_params_t {
				lambdaMultiplier = 1.9,
				maxLambda = 0.11,
				finalLambda = lambda,
				kernelSizeMultiplier = 1.0
			};
			var (u,k) = coarseToFine(f3,@params,ctf_params);
			return (u,k);
		}

		/***********************************************************************************************
		* @param f The input image. blind_params The kernel size and niters size. params Parameters
		*        uk The output image and kernel
		* Call buildPyrmaid
		* For each layer in the pyrmaid, call Prida
		**********************************************************************************************/
		(Mat,Mat) coarseToFine(Mat f, params_t blind_params, ctf_params_t @params)
		{
			//Log.Debug($"coarseToFine f={Helpers.MatDebug(f)} uk.k={Helpers.MatDebug(uk.k)} uk.u={Helpers.MatDebug(uk.u)}");
			double MK = blind_params.MK;
			double NK = blind_params.NK;

			Mat u = new Mat();
			int top = (int)Math.Floor(MK/2.0);
			int left = (int)Math.Floor(NK/2.0);
			CvInvoke.CopyMakeBorder(f,u,top,top,left,left,BorderType.Replicate);
			//Helpers.LogMat(f,"f");
			//Helpers.LogMat(u,"u");

			var k = Mat.Ones((int)NK,(int)MK,DepthType.Cv64F,1);
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
				double Ms = answer.Mp[i];
				double Ns = answer.Np[i];
				double MKs = answer.MKp[i];
				double NKs = answer.NKp[i];
				double lambda = answer.lambdas[i];
				Mat fs = answer.fp[i];

				//Log.Debug($"Ns={Ns} NKs={NKs} Ms={Ms} MKs={MKs} w={Ns + NKs - 1} h={Ms + MKs - 1}");
				CvInvoke.Resize(u,u,new Size((int)(Ns + NKs - 1), (int)(Ms + MKs - 1))); //,0,0,Inter.Linear);
				CvInvoke.Resize(k,k,new Size((int)NKs, (int)MKs)); //,0,0,Inter.Linear);

				k = k / CvInvoke.Sum(k).V0;
				blind_params.MK = MKs;
				blind_params.NK = NKs;

				Log.Message($"Working on Scale: {i+1} with lambda = {data.lambda} with pyramid_lambda = {lambda} and Kernel size {MKs}");
				(u,k) = prida(fs, u, k, lambda, blind_params);
			}

			return (u,k);
		}

		/*****************************************************************************************************************
		* @param data Including input image, kernel size, lambda, lambdaMultiplier, scaleMultiplier and largestLambda
		*        answer The dest struct stores all the result
		****************************************************************************************************************/
		output buildPyramid(input data)
		{
			//Log.Debug($"buildPyramid");

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
			//Helpers.LogMat(data.f,$"py - dst[0] = ");

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
				double factorM = answer.MKp[s-1] / answer.MKp[s];
				double factorN = answer.NKp[s-1] / answer.NKp[s];

				answer.Mp[s] = Math.Round(answer.Mp[s-1] / factorM);
				answer.Np[s] = Math.Round(answer.Np[s-1] / factorN);

				// Makes image dimension odd
				if (answer.Mp[s] % 2 == 0) { answer.Mp[s] -= 1; }
				if (answer.Np[s] % 2 == 0) { answer.Np[s] -= 1; }

				Mat dst = new Mat();
				CvInvoke.Resize(data.f,dst,new Size((int)answer.Np[s],(int)answer.Mp[s])); //,0,0,Inter.Linear);
				//Helpers.LogMat(dst,$"py - dst[{s}] = ");
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
		(Mat,Mat) prida(Mat f, Mat u, Mat k, double lambda, params_t @params)
		{
			//Log.Debug($"prida f={Helpers.MatDebug(f)} u={Helpers.MatDebug(u)} k={Helpers.MatDebug(k)}");

			for (int i = 0; i < @params.niters; i++) {
				var pGradu = new Mat[f.NumberOfChannels];
				VectorOfMat
					pf = new VectorOfMat(),
					pu = new VectorOfMat();

				CvInvoke.Split(f, pf);
				CvInvoke.Split(u, pu);

				int c = 0;
				while (c < f.NumberOfChannels) {
					//Helpers.LogMat(pu[c],$"pu[{c}]");
					Mat tmp = conv2(pu[c], k, ConvolutionType.VALID);
					//Helpers.LogMat(tmp,"tmp");
					//Helpers.LogMat(pf[c],$"pf[{c}]");
					tmp = tmp - pf[c];

					Mat rotk = Mat.Zeros(k.Height,k.Width,DepthType.Cv64F,1);
					CvInvoke.Rotate(k, rotk, RotateFlags.Rotate180);
					Mat gTmp = conv2(tmp, rotk, ConvolutionType.FULL);
					pGradu[c] = gTmp;
					c++;
				}
				Mat gradu = new Mat();
				CvInvoke.Merge(new VectorOfMat(pGradu),gradu);
				c = 0;

				var gradTV = gradTVcc(u);
				//Helpers.LogMat(u,"u");
				//Helpers.LogMat(gradTV,"gradTV");
				gradu = (gradu - lambda * gradTV);

				double minValu = 0;
				double maxValu = 0;
				Helpers.MinMaxLoc(u, ref minValu, ref maxValu);

				double minValgu = 0;
				double maxValgu = 0;
				Helpers.MinMaxLoc(Helpers.Abs(gradu), ref minValgu, ref maxValgu);

				double sf = 1e-3 * maxValu / Math.Max(1e-31, maxValgu);
				Mat u_new = u - sf * gradu;
				Mat gradk = Mat.Zeros(k.Height,k.Width,DepthType.Cv64F,1);

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
				Helpers.MinMaxLoc(k, ref minValk, ref maxValk);
				double minValgk = 0, maxValgk = 0;
				Helpers.MinMaxLoc(Helpers.Abs(gradk), ref minValgk, ref maxValgk);

				double sh = 1e-3 * maxValk / Math.Max(1e-31, maxValgk);
				double eps = double.Epsilon;
				Mat etai = sh / (k + eps);

				int bigM = 1000;
				Mat expTmp = new Mat();
				CvInvoke.Multiply(0 - etai, gradk, expTmp);
				CvInvoke.Exp(expTmp,expTmp);

				Mat tmp2 = new Mat();
				Mat tmpbigM = Helpers.InitFrom(expTmp,bigM);
				CvInvoke.Min(expTmp, tmpbigM, tmp2);
				Mat MDS = new Mat();
				CvInvoke.Multiply(k,tmp2,MDS);

				Mat k_new = MDS / CvInvoke.Sum(MDS).V0;

				u = u_new;
				k = k_new;

				//added to prevent memory from swinging wildly.
				//c# doesn't use c++'s destruct on leave context so there's a ton of
				// discarded Mats lying around - and figuring out usings is too much work
				GC.Collect();
			}
			return (u,k);
		}

		/**********************************************************************************************
		* @param img The input img, Kernel The input kernel, type FULL or VALID, dest The output result
		* Compute conv2 by calling filter2D.
		************************************************************************************************/
		Mat conv2(Mat img, Mat kernel, ConvolutionType type)
		{
			//Log.Debug($"conv2 img={Helpers.MatDebug(img)} kernel={Helpers.MatDebug(kernel)} type={type}");

			Mat flipped_kernel = new Mat();
			CvInvoke.Flip(kernel, flipped_kernel, FlipType.Horizontal | FlipType.Vertical);

			//Helpers.LogMat(img,"img");
			//Helpers.LogMat(kernel,"kernel");
			//Helpers.LogMat(flipped_kernel,"flipped_kernel");

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
				var paddTmp = new Mat(padded,new Rectangle(kernel.Rows - 1, kernel.Cols - 1, img.Cols, img.Rows));
				//Helpers.LogMat(img,"img");
				//Helpers.LogMat(paddTmp,"paddTmp");
				img.CopyTo(paddTmp);
				break;
			default:
				throw new NotSupportedException("Unsupported convolutional shape");
			}
			var region = new Rectangle( pad.X / 2, pad.Y / 2, padded.Cols - pad.X, padded.Rows - pad.Y);
			Mat dest = Helpers.InitFrom(padded);
			CvInvoke.Filter2D(padded, dest, flipped_kernel, new Point(-1, -1), 0, BorderType.Constant);
			//Helpers.LogMat(dest,"before");
			dest = new Mat(dest,region);
			//Helpers.LogMat(dest,"after");
			return dest;
		}

		/***********************************************************************************
		* @param f The scaled input image. dest The result image.
		* Calc total variation for image f
		**********************************************************************************/
		Mat gradTVcc(Mat f)
		{
			Mat fxforw = Helpers.InitFrom(f);
			new Mat(f,new Range(1,f.Rows), new Range(0,f.Cols)).CopyTo(fxforw);
			CvInvoke.CopyMakeBorder(fxforw,fxforw,0,1,0,0,BorderType.Replicate);
			fxforw = fxforw - f;

			Mat fyforw = Helpers.InitFrom(f);
			new Mat(f,new Range(0,f.Rows), new Range(1,f.Cols)).CopyTo(fyforw);
			CvInvoke.CopyMakeBorder(fyforw,fyforw,0,0,0,1,BorderType.Replicate);
			fyforw = fyforw - f;

			Mat fxback = Helpers.InitFrom(f);
			new Mat(f,new Range(0,f.Rows-1),new Range(0,f.Cols)).CopyTo(fxback);
			CvInvoke.CopyMakeBorder(fxback,fxback,1,0,0,0,BorderType.Replicate);

			Mat fyback = Helpers.InitFrom(f);
			new Mat(f,new Range(0,f.Rows),new Range(0,f.Cols-1)).CopyTo(fyback);
			CvInvoke.CopyMakeBorder(fyback,fyback,0,0,1,0,BorderType.Replicate);

			Mat fxmixd = Helpers.InitFrom(f);
			new Mat(f,new Range(1,f.Rows),new Range(0,f.Cols-1)).CopyTo(fxmixd);
			CvInvoke.CopyMakeBorder(fxmixd,fxmixd,0,1,1,0,BorderType.Replicate);
			fxmixd = fxmixd - fyback;

			Mat fymixd = Helpers.InitFrom(f);
			new Mat(f,new Range(0,f.Rows-1),new Range(1,f.Cols)).CopyTo(fymixd);
			CvInvoke.CopyMakeBorder(fymixd,fymixd,1,0,0,1,BorderType.Replicate);
			fymixd = fymixd - fxback;
			fyback = f - fyback;
			fxback = f - fxback;

			var pfxforw = fxforw.Split();
			var pfyforw = fyforw.Split();
			var pfxback = fxback.Split();
			var pfyback = fyback.Split();
			var pfxmixd = fxmixd.Split();
			var pfymixd = fymixd.Split();
			var pdest = new Mat[f.NumberOfChannels];

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
				Mat maxsqtforw = 1e-3 * Mat.Ones(sqtforw.Height,sqtforw.Width,sqtforw.Depth,sqtforw.NumberOfChannels);
				CvInvoke.Max(sqtforw, maxsqtforw, max1);

				Mat max2 = new Mat();
				Mat maxsqtmixed = 1e-3 * Mat.Ones(sqtmixed.Height,sqtmixed.Width,sqtmixed.Depth,sqtmixed.NumberOfChannels);
				CvInvoke.Max(sqtmixed, maxsqtmixed, max2);

				Mat max3 = new Mat();
				Mat maxsqtback = 1e-3 * Mat.Ones(sqtback.Height,sqtback.Width,sqtback.Depth,sqtback.NumberOfChannels);
				CvInvoke.Max(sqtback, maxsqtback, max3);

				Mat pmax1 = new Mat();
				CvInvoke.Divide(pfxforw[c] + pfyforw[c],max1,pmax1);
				pdest[c] = pmax1;

				Mat pmax2 = new Mat();
				CvInvoke.Divide(pfxback[c],max2,pmax2);
				pdest[c] = pdest[c] - pmax2;

				Mat pmax3 = new Mat();
				CvInvoke.Divide(pfyback[c],max3,pmax3);
				pdest[c] = pdest[c] - pmax3;

				c++;
			}

			Mat dest = Mat.Zeros(f.Height,f.Height,DepthType.Cv64F,f.NumberOfChannels);
			CvInvoke.Merge(new VectorOfMat(pdest),dest);
			return dest;
		}
	}
}
