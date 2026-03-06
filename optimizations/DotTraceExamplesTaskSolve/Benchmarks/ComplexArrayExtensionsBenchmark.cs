using System;
using System.Numerics;
using BenchmarkDotNet.Attributes;

namespace DotTraceExamplesTaskSolve.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(warmupCount: 5, iterationCount: 5)]
public class ComplexArrayExtensionsBenchmark
{
	private Complex[] data;
	private Complex[] dividedData;

	[GlobalSetup]
	public void Setup()
	{
		data = new Complex[13000000];
		dividedData = data.DivideByNumberV1(Math.PI);
	}

	[Benchmark]
	public void DivideByNumber()
	{
		data.DivideByNumberV1(Math.PI);
	}

	[Benchmark]
	public void SumModules()
	{
		dividedData.SumModulesV1();
	}

	[Benchmark]
	public void DivideByNumberV2()
	{
		data.DivideByNumberV2(Math.PI);
	}

	[Benchmark]
	public void SumModulesV2()
	{
		dividedData.SumModulesV2();
	}
}