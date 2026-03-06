using System.Numerics;

namespace DotTraceExamplesTaskSolve;

public static class ComplexArrayExtensionsV1
{
	public static Complex[] DivideByNumberV1(this Complex[] data, double divisor)
	{
		var result = new Complex[data.Length];
		for (int i = 0; i < data.Length; i++)
		{
			result[i] = data[i] / divisor;
		}

		return result;
	}

	public static double SumModulesV1(this Complex[] data)
	{
		var sum = 0.0;
		foreach (var e in data)
		{
			sum += e.Magnitude;
		}

		return sum;
	}
}