using System;
using System.Linq;
using BenchmarkDotNet.Attributes;

namespace BenchmarksTaskSolve.Benchmarks;

public class C2
{
	public int N;
	public string Str;

	#region Equality members

	private bool Equals(C2 other)
	{
		return N == other.N && string.Equals(Str, other.Str);
	}

	public override bool Equals(object obj)
	{
		if (ReferenceEquals(null, obj)) return false;
		if (ReferenceEquals(this, obj)) return true;
		if (obj.GetType() != this.GetType()) return false;
		return Equals((C2)obj);
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
public struct S2 : IEquatable<S2>
{
	public int N;
	public string Str;

	public bool Equals(S2 other)
	{
		return N == other.N && Str == other.Str;
	}

	public override bool Equals(object obj)
	{
		return obj is S2 other && Equals(other);
	}

	public override int GetHashCode()
	{
		unchecked
		{
			return (N * 397) ^ (Str != null ? Str.GetHashCode() : 0);
		}
	}
}

[SimpleJob(warmupCount: 5, iterationCount: 7)]
public class StructVsClassBenchmark2
{
	private C2[] classArr;
	private S2[] structArr;

	[GlobalSetup]
	public void Setup()
	{
		classArr = Enumerable.Range(0, 1000).Select(x => new C2 { N = x, Str = Guid.NewGuid().ToString() }).ToArray();
		structArr = classArr.Select(x => new S2 { N = x.N, Str = x.Str }).ToArray();
	}

	[Benchmark]
	public bool Class() => classArr.Contains(new C2 { N = 100, Str = "something" });

	[Benchmark]
	public bool Struct() => structArr.Contains(new S2 { N = 100, Str = "something" });
}