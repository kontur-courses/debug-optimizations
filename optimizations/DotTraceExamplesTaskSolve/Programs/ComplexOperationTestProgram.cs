using System;
using System.Numerics;

namespace DotTraceExamplesTaskSolve.Programs;

public class ComplexOperationTestProgram : IProgram
{
	private const int ElementsNumber = 13000000;

	public void Run()
	{
		var data = new Complex[ElementsNumber];
		data.DivideByNumberV1(Math.PI).SumModulesV1();
	}
}