# Image OpenCV #
A collection of various image processing functions with a dependency on OpenCV

## Commands ##
* build project
  * ./go.sh build
* run project
  * ./go.sh run

## Notes ##
* For windows
  * Using the normal ```dotnet``` commands may work.
* For linux
  * ImageOpenCV must be executed from the publish folder (normally ./src/bin/Debug/netcoreapp2.2/publish) Otherwise it will fail to find the opencv native library
  * There does not seem to be a published nuget Emgu.CV.runtime.ubuntu package. Therefore, I had to compile my own. This compiled package is in the ./nuget-repo folder

## Usage ##
```
Usage ImageOpenCV (method) [options]
Options:
 -h / --help                  Show full help
 (method) -h                  Method specific help
 --methods                    List possible methods

1. NlMeans [options] (input image) [output image]
 Perform image denoising using Non-local Means Denoising algorithm: http://www.ipol.im/pub/algo/bcm_non_local_means_denoising/ with several computational optimizations. Noise expected to be a gaussian white noise.
Options:
 -h (float | 3.0)             Parameter regulating filter strength. Big h value perfectly removes noise but also removes image details, smaller h value preserves details but also preserves some noise.
 -t (int | 7)                 Size in pixels of the template patch that is used to compute weights. Should be odd.
 -s (int | 21)                Size in pixels of the window that is used to compute weighted average for given pixel. Should be odd. Affects performance linearly: greater searchWindowsSize -> greater denoising time.

2. NlMeansColored [options] (input image) [output image]
 Perform image denoising using Non-local Means Denoising algorithm (modified for color image): http://www.ipol.im/pub/algo/bcm_non_local_means_denoising/ with several computational optimizations. Noise expected to be a gaussian white noise. The function converts image to CIELAB colorspace and then separately denoise L and AB components with given h parameters using fastNlMeansDenoising function.
Options:
 -h (float | 3.0)             Parameter regulating filter strength. Big h value perfectly removes noise but also removes image details, smaller h value preserves details but also preserves some noise.
 -c (float | 3.0)             The same as -h but for color components. For most images a value of 10 will be enough to remove colored noise and not distort colors.
 -t (int | 7)                 Size in pixels of the template patch that is used to compute weights. Should be odd.
 -s (int | 21)                Size in pixels of the window that is used to compute weighted average for given pixel. Should be odd. Affects performance linearly: greater searchWindowsSize -> greater denoising time.

3. Dct [options] (input image) [output image]
 The function implements simple DCT-based (Discrete Cosine Transform) denoising, link: http://www.ipol.im/pub/art/2011/ys-dct/.
Options:
 -s (double)                  Expected noise standard deviation.
 -p (int | 16)                Size of block side where dct is computed.

4. TVL1 [options] (input image) [input image] ...
 Denoise images using the Primal-dual algorithm. This algorithm is used for solving special types of variational problems (that is, finding a function to minimize some functional).
Options:
 -o (string | *)              Output image. If not provided a default name will be generated.
 -l (double | 1.0)            Lambda scaling parameter. As it is enlarged, the smooth (blurred) images are treated more favorably than detailed (but maybe more noised) ones. Roughly speaking, as it becomes smaller, the result will be more blur but more sever outliers will be removed.
 -n (int | 30)                Number of iterations that the algorithm will run. Of course, as more iterations as better, but it is hard to quantitatively refine this statement, so just use the default and increase it if the results are poor.

7. Prida [options] (input image) [output image]
 Provably Robust Image Deconvolution Algorithm, a image deblurring algorithm that implements blind deconvolution
Options:
 -l (double | 0.0006)         Tunable regularization parameter.  Higher values of lambda will encourage more smoothness in the optimal sharp image
 -k (int | 19)                Kernel size in pixels
 -i (int | 1000)              Number of iterations
```