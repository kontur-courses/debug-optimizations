using System;
using System.Runtime.CompilerServices;
using ImageProcessing;

namespace DotTraceExamplesTaskSolve;

public static class ImageProcessingAlgorithmsV3
{
	#region EdgePreservingSmoothing

	// Multiplication factor for convolution mask in edge preserving smoothing
	private const int MultiplicationFactor = 10;

	public static RGBImage EdgePreservingSmoothingV3(this RGBImage image)
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

				var c1 = Pow(
					1 - (Math.Abs(centerRed - imageData[id1 + 2]) + Math.Abs(centerGreen - imageData[id1 + 1]) +
					     Math.Abs(centerBlue - imageData[id1])), MultiplicationFactor);
				var c2 = Pow(
					1 - (Math.Abs(centerRed - imageData[id2 + 2]) + Math.Abs(centerGreen - imageData[id2 + 1]) +
					     Math.Abs(centerBlue - imageData[id2])), MultiplicationFactor);
				var c3 = Pow(
					1 - (Math.Abs(centerRed - imageData[id3 + 2]) + Math.Abs(centerGreen - imageData[id3 + 1]) +
					     Math.Abs(centerBlue - imageData[id3])), MultiplicationFactor);
				var c4 = Pow(
					1 - (Math.Abs(centerRed - imageData[id4 + 2]) + Math.Abs(centerGreen - imageData[id4 + 1]) +
					     Math.Abs(centerBlue - imageData[id4])), MultiplicationFactor);
				var c5 = Pow(
					1 - (Math.Abs(centerRed - imageData[id5 + 2]) + Math.Abs(centerGreen - imageData[id5 + 1]) +
					     Math.Abs(centerBlue - imageData[id5])), MultiplicationFactor);
				var c6 = Pow(
					1 - (Math.Abs(centerRed - imageData[id6 + 2]) + Math.Abs(centerGreen - imageData[id6 + 1]) +
					     Math.Abs(centerBlue - imageData[id6])), MultiplicationFactor);
				var c7 = Pow(
					1 - (Math.Abs(centerRed - imageData[id7 + 2]) + Math.Abs(centerGreen - imageData[id7 + 1]) +
					     Math.Abs(centerBlue - imageData[id7])), MultiplicationFactor);
				var c8 = Pow(
					1 - (Math.Abs(centerRed - imageData[id8 + 2]) + Math.Abs(centerGreen - imageData[id8 + 1]) +
					     Math.Abs(centerBlue - imageData[id8])), MultiplicationFactor);

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

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static double Pow(double powerBase, int exponent)
	{
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

	private static readonly Lazy<LUV[]> CachedLuvSpace = new(() => new LUV[rgbChannelLength * rgbChannelLength * rgbChannelLength]);
	private static readonly Lazy<RGB[]> CachedRgbSpace = new(() => new RGB[chanelLLength * chanelULength * chanelVLength]);

	private static LUV[] GetCachedLuvSpace()
	{
		lock (CachedLuvSpace)
		{
			if (!CachedLuvSpace.IsValueCreated)
			{
				FillLuvSpaceCash();
			}
		}

		return CachedLuvSpace.Value;
	}
	private static RGB[] GetCachedRGBSpace()
	{
		lock (CachedRgbSpace)
		{
			if (!CachedRgbSpace.IsValueCreated)
			{
				FillRGBSpaceCash();
			}
		}

		return CachedRgbSpace.Value;
	}

	private const int rgbChannelLength = 256;

	private static void FillLuvSpaceCash()
	{
		var cachedLuvSpaceValue = CachedLuvSpace.Value;
		var rgbBuffer = new byte[3];
		var xyzBuffer = new double[3];
		var luvBuffer = new short[3];

		for (var r = 0; r < rgbChannelLength; r++)
		{
			var rPart = r * rgbChannelLength;
			rgbBuffer[0] = (byte) r;

			for (var g = 0; g < rgbChannelLength; g++)
			{
				var gPart = (rPart + g) * rgbChannelLength;
				rgbBuffer[1] = (byte) g;

				for (var b = 0; b < rgbChannelLength; b++)
				{
					var index = gPart + b;
					rgbBuffer[2] = (byte) b;
					RgbToXyz(rgbBuffer, xyzBuffer);
					XyzToLuv(xyzBuffer, luvBuffer);
					cachedLuvSpaceValue[index].L = luvBuffer[0];
					cachedLuvSpaceValue[index].U = luvBuffer[1];
					cachedLuvSpaceValue[index].V = luvBuffer[2];
				}
			}
		}
	}

	private const int chanelLLength = 100;
	private const int chanelULength = 354;
	private const int chanelVLength = 262;
	private const int chanelUShift = 134;
	private const int chanelVShift = 140;
	private static void FillRGBSpaceCash()
	{
		var cachedRgbSpaceValue = CachedRgbSpace.Value;
		var rgbBuffer = new byte[3];
		var xyzBuffer = new double[3];
		var luvBuffer = new short[3];

		for (var l = 0; l < chanelLLength; l++)
		{
			var lPart = l * chanelULength;
			luvBuffer[0] = (short)l;

			for (var u = 0; u < chanelULength; u++)
			{
				var luPart = (lPart + u) * chanelVLength;
				luvBuffer[1] =(short)( u - chanelUShift);

				for (var v = 0; v < chanelVLength; v++)
				{
					var index = luPart + v;
					luvBuffer[2] = (short)(v - chanelVShift);
					LuvToXyz(luvBuffer, xyzBuffer);
					XyzToRgb(xyzBuffer, rgbBuffer);
					cachedRgbSpaceValue[index].Red = rgbBuffer[0];
					cachedRgbSpaceValue[index].Green = rgbBuffer[1];
					cachedRgbSpaceValue[index].Blue = rgbBuffer[2];
				}
			}
		}
	}

	public struct LUV : IEquatable<LUV>
	{
		public short L;
		public short U;
		public short V;

		public LUV(byte l, short u, short v)
		{
			L = l;
			U = u;
			V = v;
		}

		/// <inheritdoc />
		public bool Equals(LUV other)
		{
			return L.Equals(other.L) && U.Equals(other.U) && V.Equals(other.V);
		}

		/// <inheritdoc />
		public override bool Equals(object obj)
		{
			return obj is LUV other && Equals(other);
		}

		/// <inheritdoc />
		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = L.GetHashCode();
				hashCode = (hashCode * 397) ^ U.GetHashCode();
				hashCode = (hashCode * 397) ^ V.GetHashCode();
				return hashCode;
			}
		}
	}

	public struct RGB : IEquatable<RGB>
	{
		public byte Red;

		public byte Green;

		public byte Blue;

		/// <inheritdoc/>
		public bool Equals( RGB other)
		{
			return Red == other.Red && Green == other.Green && Blue == other.Blue;
		}

		/// <inheritdoc/>
		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
			{
				return false;
			}

			return obj is RGB other && Equals(other);
		}

		/// <inheritdoc/>
		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = Red.GetHashCode();
				hashCode = (hashCode * 397) ^ Green.GetHashCode();
				hashCode = (hashCode * 397) ^ Blue.GetHashCode();
				return hashCode;
			}
		}
	}

	public static RGBImage MeanShiftV3(this RGBImage image)
	{
		var width = image.Width;
		var height = image.Height;
		var imageData = image.ImageData;
		var bytesPerLine = image.BytesPerLine;
		var bytesPerPixel = RGBImage.BytesPerPixel;
		var luvImage = new double[imageData.Length];
		var filteringData = new byte[imageData.Length];
		var luvCash = GetCachedLuvSpace();
		var rgbCash = GetCachedRGBSpace();

		for (var y = 0; y < height; y++)
		{
			for (var x = 0; x < width; x++)
			{
				var curElem = bytesPerLine + x * bytesPerPixel;
				var r = image.ImageData[curElem + 2];
				var g = image.ImageData[curElem + 1];
				var b = image.ImageData[curElem];

				var luv = luvCash[r * 65536 + g * 256 + b];

				luvImage[curElem] = luv.L;
				luvImage[curElem + 1] = luv.U;
				luvImage[curElem + 2] = luv.V;
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
				var lCenter = luvImage[i];
				var uCenter = luvImage[i + 1];
				var vCenter = luvImage[i + 2];
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
									var curElem = y2 * bytesPerLine + x2 * bytesPerPixel;
									var l2 = luvImage[curElem];
									var u2 = luvImage[curElem + 1];
									var v2 = luvImage[curElem + 2];

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

				var l = (short) lCenter;
				var u = (short) uCenter;
				var v = (short) vCenter;
				var rgb = rgbCash[l * chanelULength * chanelVLength + 
				                  (u + chanelUShift) * chanelVLength
				                  + v + chanelVShift];

				luvImage[i] = lCenter;
				luvImage[i + 1] = uCenter;
				luvImage[i + 2] = vCenter;

				filteringData[i + 2] = rgb.Red;
				filteringData[i + 1] = rgb.Green;
				filteringData[i] = rgb.Blue;
			}
		}

		return new RGBImage(image.Width, height, image.BytesPerLine, filteringData);
	}

	public static void RgbToXyz(byte[] rgb, double[] xyz)
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

	public static void XyzToRgb(double[] xyz, byte[] rgb)
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

	public static void XyzToLuv(double[] xyz, short[] luv)
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

		luv[0] = (short) (116 * l - 16);
		luv[1] = (short) (13 * luv[0] * (u - u2));
		luv[2] = (short) (13 * luv[0] * (v - v2));
	}

	public static void LuvToXyz(short[] luv, double[] xyz)
	{
		var y = (double) (luv[0] + 16) / 116;

		if (Pow(y, 3) > 0.008856)
		{
			y = Pow(y, 3);
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

		var u = (double) luv[1] / (13 * luv[0]) + localU;
		var v = (double) luv[2] / (13 * luv[0]) + localV;

		xyz[1] = y * 100;
		xyz[0] = -(9 * xyz[1] * u) / ((u - 4) * v - u * v);
		xyz[2] = (9 * xyz[1] - 15 * v * xyz[1] - v * xyz[0]) / (3 * v);
	}

	#endregion
}