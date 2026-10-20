using FluentAssertions;
using ProxyR.OpenAPI.DocumentFilters;
using Xunit;

namespace ProxyR.OpenAPI.Tests
{
    /// <summary>
    /// Regression tests for the suffix-strip index fix (#63).
    /// </summary>
    public class ProxyRDocumentFilterTests
    {
        [Fact]
        public void ComputePathPart_WithPrefixAndSuffix_StripsBothEnds()
        {
            ProxyRDocumentFilter.ComputePathPart("Query_Users_Grid_GRID", "Query_", "_GRID")
                .Should().Be("Users_Grid");
        }

        [Fact]
        public void ComputePathPart_WithPrefixOnly_StripsPrefix()
        {
            ProxyRDocumentFilter.ComputePathPart("Query_Users", "Query_", "")
                .Should().Be("Users");
        }

        [Fact]
        public void ComputePathPart_WithSuffixOnly_StripsSuffixFromEnd()
        {
            // Regression: the old implementation removed from index
            // prefix.Length - suffix.Length (here: 0), mangling the start.
            ProxyRDocumentFilter.ComputePathPart("Users_Grid", "", "_GRID")
                .Should().Be("Users");
        }

        [Fact]
        public void ComputePathPart_WithSuffixLongerThanName_DoesNotThrow()
        {
            var act = () => ProxyRDocumentFilter.ComputePathPart("Ab_GRID", "", "_GRID_VALUE");
            act.Should().NotThrow();
        }

        [Fact]
        public void ComputePathPart_WithNoPrefixOrSuffix_IsUnchanged()
        {
            ProxyRDocumentFilter.ComputePathPart("Users", "", "")
                .Should().Be("Users");
        }
    }
}
