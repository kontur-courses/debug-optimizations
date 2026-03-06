using BenchmarkDotNet.Running;
using DotTraceExamplesTaskSolve.Programs;

namespace DotTraceExamplesTaskSolve;

internal class Program
{
	public static void Main(string[] args)
	{
		BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
		// ProgramRunner.Run(new ComplexOperationTestProgram());
		// ProgramRunner.Run(new EdgePreservingSmoothingProgram());
		// ProgramRunner.Run(new MeanShiftProgram());
	}
}