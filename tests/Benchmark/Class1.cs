using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Benchmark
{

    [RankColumn()]
    [MemoryDiagnoser]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    public class ClassBenchmark
    {

        [Benchmark(Baseline = true)]
        public static void Benchmark1()
        {

        }


        [Benchmark]

        public static void Benchmark2()
        {

        }

    }
}
