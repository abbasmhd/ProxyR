using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using ProxyR.Abstractions;
using ProxyR.Abstractions.Builder;
using ProxyR.Abstractions.Parameters;
using ProxyR.Middleware.Helpers;
using Xunit;

namespace ProxyR.Middleware.Tests
{
    /// <summary>
    /// Minimal IQueryCollection stub for feeding query-string values
    /// into StatementBuilder.GetODataQueryStringParameters.
    /// </summary>
    internal class FakeQueryCollection : IQueryCollection
    {
        private readonly Dictionary<string, StringValues> _values;

        public FakeQueryCollection(Dictionary<string, StringValues> values) => _values = values;

        public StringValues this[string key] => _values.TryGetValue(key, out var value) ? value : StringValues.Empty;

        public int Count => _values.Count;

        public ICollection<string> Keys => _values.Keys;

        public bool ContainsKey(string key) => _values.ContainsKey(key);

        public bool TryGetValue(string key, out StringValues value) => _values.TryGetValue(key, out value!);

        public IEnumerator<KeyValuePair<string, StringValues>> GetEnumerator() => _values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    /// <summary>
    /// Regression tests for the ORDER BY SQL-injection fix (#59):
    /// user-controlled sort columns ($orderby / JSON orderby) must be
    /// sanitized before being spliced into the generated SQL.
    /// </summary>
    public class StatementBuilderTests
    {
        private static SqlBuilder Build(ProxyRQueryParameters parameters)
        {
            var statement = new SqlBuilder();
            var paramBuilder = new ParameterBuilder();

            StatementBuilder.BuildSqlUnit(
                statement,
                paramBuilder,
                parameters,
                schema: "dbo",
                name: "Query_Users_GRID",
                arguments: Array.Empty<string>(),
                isView: false);

            return statement;
        }

        [Fact]
        public void SortColumnName_WithInjectedSql_IsSanitized()
        {
            var parameters = new ProxyRQueryParameters();
            parameters.Sort.Add(new ProxyRSortParameters
            {
                ColumnName = "name] DESC; SELECT 1--"
            });

            var sql = Build(parameters).ToString();

            sql.Should().Contain("[name__DESC__SELECT_1__]");
            sql.Should().NotContain("SELECT 1");
        }

        [Fact]
        public void SortColumnName_WithBracketEscapeAttempt_IsSanitized()
        {
            var parameters = new ProxyRQueryParameters();
            parameters.Sort.Add(new ProxyRSortParameters
            {
                ColumnName = "foo]]]]"
            });

            var sql = Build(parameters).ToString();

            // ']' is not a valid identifier character, so it must be replaced.
            sql.Should().NotContain("]]");
        }

        [Fact]
        public void SortFromQueryString_WithInjectedSql_IsSanitized()
        {
            var parameters = new ProxyRQueryParameters();
            var query = new FakeQueryCollection(new Dictionary<string, StringValues>
            {
                ["$orderby"] = "name] DESC; SELECT 1--"
            });

            StatementBuilder.GetODataQueryStringParameters(query, parameters);

            // $orderby is split on spaces, so only "name]" survives as the
            // column name — but it must still be sanitized.
            parameters.Sort.Single().ColumnName.Should().Be("name]");

            var sql = Build(parameters).ToString();

            sql.Should().Contain("[name_]");
            sql.Should().NotContain("SELECT 1--");
        }

        [Fact]
        public void SortFromQueryString_WithValidColumn_IsPreserved()
        {
            var parameters = new ProxyRQueryParameters();
            var query = new FakeQueryCollection(new Dictionary<string, StringValues>
            {
                ["$orderby"] = "Name desc"
            });

            StatementBuilder.GetODataQueryStringParameters(query, parameters);

            var sql = Build(parameters).ToString();

            sql.Should().Contain("[Name] DESC");
        }

        [Fact]
        public void SortByColumnPosition_IsPreserved()
        {
            var parameters = new ProxyRQueryParameters();
            parameters.Sort.Add(new ProxyRSortParameters
            {
                Literal = "1",
                IsDescending = true
            });

            var sql = Build(parameters).ToString();

            sql.Should().Contain("1 DESC");
        }
    }
}
