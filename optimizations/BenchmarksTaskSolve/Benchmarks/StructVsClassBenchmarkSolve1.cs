using System;
using System.Linq;
using BenchmarkDotNet.Attributes;

namespace BenchmarksTaskSolve.Benchmarks;

public class C1
{
	public int N;
	public string Str;

	#region Equality members

	private bool Equals(C1 other)
	{
		return N == other.N && string.Equals(Str, other.Str);
	}

	public override bool Equals(object obj)
	{
		if (ReferenceEquals(null, obj)) return false;
		if (ReferenceEquals(this, obj)) return true;
		if (obj.GetType() != this.GetType()) return false;
		return Equals((C1)obj);
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
public struct S1
{
	public int N;
	public string Str;

	private bool Equals(S1 other)
	{
		return N == other.N && string.Equals(Str, other.Str);
	}

	public override bool Equals(object obj)
	{
		if (ReferenceEquals(null, obj)) return false;
		if (obj.GetType() != this.GetType()) return false;
		return Equals((S1)obj);
	}

	public override int GetHashCode()
	{
		unchecked
		{
			return (N * 397) ^ (Str?.GetHashCode() ?? 0);
		}
	}
}

[SimpleJob(warmupCount: 5, iterationCount: 7)]
public class StructVsClassBenchmark1
{
	private C1[] classArr;
	private S1[] structArr;

	[GlobalSetup]
	public void Setup()
	{
		classArr = Enumerable.Range(0, 1000).Select(x => new C1 { N = x, Str = Guid.NewGuid().ToString() }).ToArray();
		structArr = classArr.Select(x => new S1 { N = x.N, Str = x.Str }).ToArray();
	}

	[Benchmark]
	public bool Class() => classArr.Contains(new C1 { N = 100, Str = "something" });

	[Benchmark]
	public bool Struct() => structArr.Contains(new S1 { N = 100, Str = "something" });
}