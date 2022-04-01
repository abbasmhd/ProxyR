using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using ProxyR.Abstractions.Builder;
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
    public class SqlUtilityBenchmark
    {

        private const string identifier = "test;/";
        private const string chainOfParts = "test1.test2.test3";
        private const string name1 = nameof(name1);
        private const string name2 = nameof(name2);
        private const string name3 = nameof(name3);
        private const string name4 = nameof(name4);

        [Benchmark]
        public void Sanitize()
        {
            Sql.Sanitize(identifier);
        }

        [Benchmark]
        public void ColumnReferences()
        {
            Sql.ColumnReferences(name1, name2, name3, name4);
        }

        [Benchmark]
        public void ColumnLines()
        {
            Sql.ColumnLines(name1, name2, name3, name4);
        }

        [Benchmark]
        public void SplitIdentifierParts()
        {
            Sql.SplitIdentifierParts(chainOfParts);
        }

        [Benchmark]
        public void GetSchemaAndObjectName()
        {
            Sql.GetSchemaAndObjectName(identifier);
        }

        [Benchmark]
        public void ParenthesisLines()
        {
            Sql.ParenthesisLines(name1, name2, name3, name4);
        }

        [Benchmark]
        public void Values()
        {
            Sql.Values(name1, name2, name3, name4);
        }

        [Benchmark]
        public void CommaLines()
        {
            Sql.CommaLines(name1, name2, name3, name4);
        }

    }
}
