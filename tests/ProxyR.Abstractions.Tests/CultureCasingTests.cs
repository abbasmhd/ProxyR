using System.Globalization;
using System.Threading;
using FluentAssertions;
using ProxyR.Abstractions.Commands;
using ProxyR.Core.Extensions;
using Xunit;

namespace ProxyR.Abstractions.Tests
{
    /// <summary>
    /// Regression tests for culture-invariant casing (#66) —
    /// the Turkish-I problem where i/I round-trips through dotless forms.
    /// </summary>
    public class CultureCasingTests : IDisposable
    {
        private readonly CultureInfo _originalCulture;

        public CultureCasingTests()
        {
            _originalCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = new CultureInfo("tr-TR");
        }

        public void Dispose()
        {
            Thread.CurrentThread.CurrentCulture = _originalCulture;
        }

        [Fact]
        public void ToDbObjectType_WithLowercaseIF_UnderTurkishCulture_Parses()
        {
            // "if".ToUpper() in tr-TR produces "İF", which failed the switch.
            DbTypes.ToDbObjectType("if").Should().Be(ProxyR.Abstractions.Commands.DbObjectType.InlineTableValuedFunction);
        }

        [Fact]
        public void FromJsType_WithMixedCase_UnderTurkishCulture_Parses()
        {
            DbTypes.FromJsType("STRING").Should().Be(DbTypes.FromJsType("string"));
            DbTypes.FromJsType("NUMBER").Should().Be(DbTypes.FromJsType("number"));
        }

        [Fact]
        public void ToCamelCase_UnderTurkishCulture_UsesInvariantLowercase()
        {
            // "ID".ToLower() in tr-TR produces "ıd".
            "ID".ToCamelCase().Should().Be("id");
        }
    }
}
