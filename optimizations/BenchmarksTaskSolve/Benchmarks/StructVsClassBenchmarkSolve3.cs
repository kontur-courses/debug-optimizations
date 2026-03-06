using System;
using System.Linq;
using BenchmarkDotNet.Attributes;

namespace BenchmarksTaskSolve.Benchmarks;

public class C3
{
	public int N;
	public string Str;

	#region Equality members

	private bool Equals(C3 other)
	{
		return N == other.N && string.Equals(Str, other.Str);
	}

	public override bool Equals(object obj)
	{
		if (ReferenceEquals(null, obj)) return false;
		if (ReferenceEquals(this, obj)) return true;
		if (obj.GetType() != this.GetType()) return false;
		return Equals((C3)obj);
	}

	public override int GetHashCode()
	{
		unchecked
		{
			return (N * 397) ^ (Str?.GetHashCode() ?? 0);
		}
	}

	#endregion
}

public record struct S3(int N, string Str);

[SimpleJob(warmupCount: 5, iterationCount: 7)]
public class StructVsClassBenchmark3
{
	private C3[] classArr;
	private S3[] structArr;

	[GlobalSetup]
	public void Setup()
	{
		classArr = Enumerable.Range(0, 1000).Select(x => new C3 { N = x, Str = Guid.NewGuid().ToString() }).ToArray();
		structArr = classArr.Select(x => new S3 { N = x.N, Str = x.Str }).ToArray();
	}

	[Benchmark]
	public bool Class() => classArr.Contains(new C3 { N = 100, Str = "something" });

	[Benchmark]
	public bool Struct() => structArr.Contains(new S3 { N = 100, Str = "something" });
}