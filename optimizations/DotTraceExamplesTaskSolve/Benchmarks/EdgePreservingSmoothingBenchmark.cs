using System.IO;
using BenchmarkDotNet.Attributes;
using ImageProcessing;

namespace DotTraceExamplesTaskSolve.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(warmupCount: 5, iterationCount: 5)]
public class EdgePreservingSmoothingBenchmark
{
	private RGBImage image;

	[IterationSetup]
	public void Setup()
	{
		const string fileName = @"TestImages\TestImage.jpg";
		using var fileStream = File.OpenRead(fileName);
		image = RGBImage.FromStream(fileStream);
	}

	[Benchmark]
	public void EdgePreservingSmoothing()
	{
		image.EdgePreservingSmoothingV1();
	}

	[Benchmark]
	public void EdgePreservingSmoothingWithFastPow()
	{
		image.EdgePreservingSmoothingV2();
	}

	[Benchmark]
	public void EdgePreservingSmoothingWithFastPowAndInlining()
	{
		image.EdgePreservingSmoothingV3();
	}
}