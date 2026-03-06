using BenchmarkDotNet.Running;

namespace BenchmarksTaskSolve;

class Program
{
	static void Main(string[] args)
	{
		BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
	}
}