using System;
using System.Runtime.CompilerServices;
using ImageProcessing;

namespace DotTraceExamplesTaskSolve;

public static class ImageProcessingAlgorithmsV2
{
	#region EdgePreservingSmoothing

	// Multiplication factor for convolution mask in edge preserving smoothing
	private const int MultiplicationFactor = 10;

	public static RGBImage EdgePreservingSmoothingV2(this RGBImage image)
	{
		var imageData = image.ImageData;
		var height = image.Height;
		var bytesPerLine = image.BytesPerLine;
		var bitesPerPixel = RGBImage.BytesPerPixel;
		var filteringData = new byte[imageData.Length];

		for (var y = 0; y < height; y++)
		{
			for (var x = 0; x < bytesPerLine; x += bitesPerPixel)
			{
				var i = y * bytesPerLine + x;

				if (x == 0 || y == 0 || x == bytesPerLine - bitesPerPixel || y == height - 1)
				{
					filteringData[i] = imageData[i];
					filteringData[i + 1] = imageData[i + 1];
					filteringData[i + 2] = imageData[i + 2];

					continue;
				}

				//Center pixel of convolution mask
				var centerRed = imageData[i + 2];
				var centerGreen = imageData[i + 1];
				var centerBlue = imageData[i];

				// Indexes of neighbor pixels of convolution mask
				var id1 = (y - 1) * bytesPerLine + (x - bitesPerPixel);
				var id2 = (y - 1) * bytesPerLine + x;
				var id3 = (y - 1) * bytesPerLine + x + bitesPerPixel;
				var id4 = y * bytesPerLine + (x - bitesPerPixel);
				var id5 = y * bytesPerLine + x + bitesPerPixel;
				var id6 = (y + 1) * bytesPerLine + (x - bitesPerPixel);
				var id7 = (y + 1) * bytesPerLine + x;
				var id8 = (y + 1) * bytesPerLine + x + bitesPerPixel;

				var c1 = GetCoefficient(centerRed, centerGreen, centerBlue, imageData[id1 + 2], imageData[id1 + 1],
					imageData[id1]);
				var c2 = GetCoefficient(centerRed, centerGreen, centerBlue, imageData[id2 + 2], imageData[id2 + 1],
					imageData[id2]);
				var c3 = GetCoefficient(centerRed, centerGreen, centerBlue, imageData[id3 + 2], imageData[id3 + 1],
					imageData[id3]);
				var c4 = GetCoefficient(centerRed, centerGreen, centerBlue, imageData[id4 + 2], imageData[id4 + 1],
					imageData[id4]);
				var c5 = GetCoefficient(centerRed, centerGreen, centerBlue, imageData[id5 + 2], imageData[id5 + 1],
					imageData[id5]);
				var c6 = GetCoefficient(centerRed, centerGreen, centerBlue, imageData[id6 + 2], imageData[id6 + 1],
					imageData[id6]);
				var c7 = GetCoefficient(centerRed, centerGreen, centerBlue, imageData[id7 + 2], imageData[id7 + 1],
					imageData[id7]);
				var c8 = GetCoefficient(centerRed, centerGreen, centerBlue, imageData[id8 + 2], imageData[id8 + 1],
					imageData[id8]);

				var csum = c1 + c2 + c3 + c4 + c5 + c6 + c7 + c8;

				var resultRed = (imageData[id1 + 2] * c8 +
				                 imageData[id2 + 2] * c7 +
				                 imageData[id3 + 2] * c6 +
				                 imageData[id4 + 2] * c5 +
				                 imageData[id5 + 2] * c4 +
				                 imageData[id6 + 2] * c3 +
				                 imageData[id7 + 2] * c2 +
				                 imageData[id8 + 2] * c1) / csum;

				var resultGreen = (imageData[id1 + 1] * c8 +
				                   imageData[id2 + 1] * c7 +
				                   imageData[id3 + 1] * c6 +
				                   imageData[id4 + 1] * c5 +
				                   imageData[id5 + 1] * c4 +
				                   imageData[id6 + 1] * c3 +
				                   imageData[id7 + 1] * c2 +
				                   imageData[id8 + 1] * c1) / csum;

				var resultBlue = (imageData[id1] * c8 +
				                  imageData[id2] * c7 +
				                  imageData[id3] * c6 +
				                  imageData[id4] * c5 +
				                  imageData[id5] * c4 +
				                  imageData[id6] * c3 +
				                  imageData[id7] * c2 +
				                  imageData[id8] * c1) / csum;


				filteringData[i + 2] = (byte)resultRed;
				filteringData[i + 1] = (byte)resultGreen;
				filteringData[i] = (byte)resultBlue;
			}
		}

		return new RGBImage(image.Width, height, bytesPerLine, filteringData);
	}

	/// <summary>
	/// Calculate coefficient for the convolution mask of edge preserving smoothing
	/// </summary>
	/// <param name="centerRed"></param>
	/// <param name="centerGreen"></param>
	/// <param name="centerBlue"></param>
	/// <param name="neighborRed"></param>
	/// <param name="neighborGreen"></param>
	/// <param name="neighborBlue"></param>
	/// <returns></returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static double GetCoefficient(byte centerRed, byte centerGreen, byte centerBlue, byte neighborRed,
		byte neighborGreen, byte neighborBlue)
	{
		var d = Math.Abs(centerRed - neighborRed) + Math.Abs(centerGreen - neighborGreen) +
		        Math.Abs(centerBlue - neighborBlue);

		return Pow(1 - d, MultiplicationFactor);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static double Pow(double powerBase, int exponent)
	{
		if (exponent < 0)
		{
			throw new ArgumentException("Exponent should be greater or equal to zero");
		}

		var result = 1.0;

		while (exponent > 0)
		{
			if ((exponent & 1) == 0)
			{
				powerBase *= powerBase;
				exponent >>= 1;
			}
			else
			{
				result *= powerBase;
				--exponent;
			}
		}

		return result;
	}

	#endregion

	#region MeanShift

	private const int Rad = 2;
	private const int RadCol = 20;
	private const int RadCol2 = RadCol * RadCol;
	public static RGBImage MeanShiftV2(this RGBImage image)
	{
		var width = image.Width;
		var height = image.Height;
		var imageData = image.ImageData;
		var bytesPerLine = image.BytesPerLine;
		var bytesPerPixel = RGBImage.BytesPerPixel;
		var luvImage = new double[width, height][];
		var filteringData = new byte[imageData.Length];

		var rgbBuffer = new byte[3];
		var xyzBuffer = new double[3];
		var luvBuffer = new double[3];

		for (var y = 0; y < height; y++)
		{
			for (var x = 0; x < width; x++)
			{
				var curElem = y * bytesPerLine + x * bytesPerPixel;
				var r = image.ImageData[curElem + 2];
				var g = image.ImageData[curElem + 1];
				var b = image.ImageData[curElem];
				rgbBuffer[0] = image.ImageData[curElem + 2]; //r
				rgbBuffer[1] = image.ImageData[curElem + 1]; //g
				rgbBuffer[2] = image.ImageData[curElem]; //b
				RgbToXyz(rgbBuffer, xyzBuffer);
				XyzToLuv(xyzBuffer, luvBuffer);

				luvImage[x, y] = new [] {luvBuffer[0], luvBuffer[1], luvBuffer[2]};
			}
		}

		var numOfIterations = 0;

		for (var y = 0; y < height; y++)
		{
			for (int x = 0, resX = 0; x < width; x++, resX += bytesPerPixel)
			{
				var i = y * bytesPerLine + resX;

				var xCenter = x;
				var yCenter = y;
				var lCenter = luvImage[x, y][0];
				var uCenter = luvImage[x, y][1];
				var vCenter = luvImage[x, y][2];
				double shift;

				do
				{
					var xCenterOld = xCenter;
					var yCenterOld = yCenter;
					var lCenterOld = lCenter;
					var uCenterOld = uCenter;
					var vCenterOld = vCenter;

					float mx = 0;
					float my = 0;
					float mY = 0;
					float mI = 0;
					float mQ = 0;
					var num = 0;

					for (var ry = -Rad; ry <= Rad; ry++)
					{
						var y2 = yCenter + ry;

						if (y2 >= 0 && y2 < height)
						{
							for (var rx = -Rad; rx <= Rad; rx++)
							{
								var x2 = xCenter + rx;
								if (x2 >= 0 && x2 < width)
								{
									var l2 = luvImage[x2, y2][0];
									var u2 = luvImage[x2, y2][1];
									var v2 = luvImage[x2, y2][2];

									var dYinner = lCenter - l2;
									var dIinner = uCenter - u2;
									var dQinner = vCenter - v2;

									if (dYinner * dYinner + dIinner * dIinner + dQinner * dQinner <= RadCol2)
									{
										mx += x2;
										my += y2;
										mY += (float) l2;
										mI += (float) u2;
										mQ += (float) v2;
										num++;
									}
								}
							}
						}
					}

					var numResult = 1.0 / num;
					lCenter = mY * numResult;
					uCenter = mI * numResult;
					vCenter = mQ * numResult;
					xCenter = (int) (mx * numResult + 0.5);
					yCenter = (int) (my * numResult + 0.5);
					var dx = xCenter - xCenterOld;
					var dy = yCenter - yCenterOld;
					var dY = lCenter - lCenterOld;
					var dI = uCenter - uCenterOld;
					var dQ = vCenter - vCenterOld;

					shift = dx * dx + dy * dy + dY * dY + dI * dI + dQ * dQ;
					numOfIterations++;
				} while (shift > 1 && numOfIterations < 100);

				luvBuffer[0] = lCenter;
				luvBuffer[1] = uCenter;
				luvBuffer[2] = vCenter;
				LuvToXyz(luvBuffer, xyzBuffer);
				XyzToRgb(xyzBuffer, rgbBuffer);

				filteringData[i + 2] = rgbBuffer[0];
				filteringData[i + 1] = rgbBuffer[1];
				filteringData[i] = rgbBuffer[2];
			}
		}

		return new RGBImage(image.Width, height, image.BytesPerLine, filteringData);
	}
		
	private static void RgbToXyz(byte[] rgb, double[] xyz)
	{
		var red = (double) rgb[0] / 255;
		var green = (double) rgb[1] / 255;
		var blue = (double) rgb[2] / 255;

		if (red > 0.04045)
		{
			red = Math.Pow((red + 0.055) / 1.055, 2.4);
		}
		else
		{
			red /= 12.92;
		}

		if (green > 0.04045)
		{
			green = Math.Pow((green + 0.055) / 1.055, 2.4);
		}
		else
		{
			green /= 12.92;
		}

		if (blue > 0.04045)
		{
			blue = Math.Pow((blue + 0.055) / 1.055, 2.4);
		}
		else
		{
			blue /= 12.92;
		}

		red *= 100;
		green *= 100;
		blue *= 100;

		xyz[0] = red * 0.4124 + green * 0.3576 + blue * 0.1805;
		xyz[1] = red * 0.2126 + green * 0.7152 + blue * 0.0722;
		xyz[2] = red * 0.0193 + green * 0.1192 + blue * 0.9505;
	}

	private static void XyzToRgb(double[] xyz, byte[] rgb)
	{
		var x = xyz[0] / 100;
		var y = xyz[1] / 100;
		var z = xyz[2] / 100;

		var red = x * 3.2406 + y * -1.5372 + z * -0.4986;
		var green = x * -0.9689 + y * 1.8758 + z * 0.0415;
		var blue = x * 0.0557 + y * -0.2040 + z * 1.0570;

		if (red > 0.0031308)
		{
			red = 1.055 * Math.Pow(red, 1 / 2.4) - 0.055;
		}
		else
		{
			red = 12.92 * red;
		}

		if (green > 0.0031308)
		{
			green = 1.055 * Math.Pow(green, 1 / 2.4) - 0.055;
		}
		else
		{
			green = 12.92 * green;
		}

		if (blue > 0.0031308)
		{
			blue = 1.055 * Math.Pow(blue, 1 / 2.4) - 0.055;
		}
		else
		{
			blue = 12.92 * blue;
		}

		rgb[0] = (byte) (red * 255);
		rgb[1] = (byte) (green * 255);
		rgb[2] = (byte) (blue * 255);
	}

	private static void XyzToLuv(double[] xyz, double[] luv)
	{
		var l = xyz[1] / 100;
		var u = 4 * xyz[0] / (xyz[0] + 15 * xyz[1] + 3 * xyz[2]);
		var v = 9 * xyz[1] / (xyz[0] + 15 * xyz[1] + 3 * xyz[2]);

		if (l > 0.008856)
		{
			l = Math.Pow(l, 1.0 / 3.0);
		}
		else
		{
			l = 7.787 * l + 16.0 / 116;
		}

		const double x = 95.047;
		const double y = 100.0;
		const double z = 108.883;

		var u2 = 4 * x / (x + 15 * y + 3 * z);
		var v2 = 9 * y / (x + 15 * y + 3 * z);

		luv[0] = 116 * l - 16;
		luv[1] = 13 * luv[0] * (u - u2);
		luv[2] = 13 * luv[0] * (v - v2);
	}

	private static void LuvToXyz(double[] luv, double[] xyz)
	{
		var y = (luv[0] + 16) / 116;

		if (Math.Pow(y, 3) > 0.008856)
		{
			y = Math.Pow(y, 3);
		}
		else
		{
			y = (y - 16.0 / 116) / 7.787;
		}

		const double localX = 95.047;
		const double localY = 100.0;
		const double localZ = 108.883;

		const double localU = 4 * localX / (localX + 15 * localY + 3 * localZ);
		const double localV = 9 * localY / (localX + 15 * localY + 3 * localZ);

		var u = luv[1] / (13 * luv[0]) + localU;
		var v = luv[2] / (13 * luv[0]) + localV;

		xyz[1] = y * 100;
		xyz[0] = -(9 * xyz[1] * u) / ((u - 4) * v - u * v);
		xyz[2] = (9 * xyz[1] - 15 * v * xyz[1] - v * xyz[0]) / (3 * v);
	}

	#endregion
}