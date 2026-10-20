using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProxyR.Core.Extensions;

namespace ProxyR.Core.Tests
{
    /// <summary>
    /// Regression tests for the ToUpperFirst inverted-guard fix (#62).
    /// </summary>
    [TestClass]
    public class StringExtensionsTests
    {
        [TestMethod]
        public void ToUpperFirst_WithLowercaseStart_UppercasesFirstCharacter()
        {
            "hello world".ToUpperFirst().Should().Be("Hello world");
        }

        [TestMethod]
        public void ToUpperFirst_WithUppercaseStart_IsUnchanged()
        {
            "Hello".ToUpperFirst().Should().Be("Hello");
        }

        [TestMethod]
        public void ToUpperFirst_WithEmptyString_DoesNotThrow()
        {
            var act = () => "".ToUpperFirst();
            act.Should().NotThrow();
            "".ToUpperFirst().Should().Be("");
        }

        [TestMethod]
        public void ToUpperFirst_WithNull_DoesNotThrow()
        {
            string source = null;
            var act = () => source.ToUpperFirst();
            act.Should().NotThrow();
            source.ToUpperFirst().Should().BeNull();
        }

        [TestMethod]
        public void ToUpperFirst_StartingWithNonLetter_IsUnchanged()
        {
            "123abc".ToUpperFirst().Should().Be("123abc");
        }

        [TestMethod]
        public void ToCapitalWordCase_ConvertsSnakeAndCamelCase()
        {
            // Depends on ToUpperFirst for word casing.
            "camelCaseWord".ToCapitalWordCase().Should().Be("Camel Case Word");
            "snake_case_word".ToCapitalWordCase().Should().Be("Snake Case Word");
            "kebab-case-word".ToCapitalWordCase().Should().Be("Kebab Case Word");
        }
    }
}
