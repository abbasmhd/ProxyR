using FluentAssertions;
using ProxyR.Abstractions;
using ProxyR.Abstractions.Parameters;
using ProxyR.Middleware.Helpers;
using Xunit;

namespace ProxyR.Middleware.Tests
{
    /// <summary>
    /// Tests for the paging-limit enforcement (#67).
    /// </summary>
    public class RequestLimitTests
    {
        [Fact]
        public void ApplyRequestLimits_WithMaxTake_ClampsTake()
        {
            var parameters = new ProxyRQueryParameters { Take = 1_000_000 };
            var options = new ProxyROptions { MaxTake = 500 };

            StatementBuilder.ApplyRequestLimits(parameters, options);

            parameters.Take.Should().Be(500);
        }

        [Fact]
        public void ApplyRequestLimits_WithTakeBelowMax_IsUnchanged()
        {
            var parameters = new ProxyRQueryParameters { Take = 100 };
            var options = new ProxyROptions { MaxTake = 500 };

            StatementBuilder.ApplyRequestLimits(parameters, options);

            parameters.Take.Should().Be(100);
        }

        [Fact]
        public void ApplyRequestLimits_WithNoMaxTake_IsUnbounded()
        {
            var parameters = new ProxyRQueryParameters { Take = 1_000_000 };
            var options = new ProxyROptions();

            StatementBuilder.ApplyRequestLimits(parameters, options);

            parameters.Take.Should().Be(1_000_000);
        }

        [Fact]
        public void ApplyRequestLimits_WhenLoadAllDisallowed_ForcesLoadAllOff()
        {
            var parameters = new ProxyRQueryParameters { LoadAll = true };
            var options = new ProxyROptions { AllowLoadAll = false };

            StatementBuilder.ApplyRequestLimits(parameters, options);

            parameters.LoadAll.Should().BeFalse();
        }

        [Fact]
        public void ApplyRequestLimits_WhenLoadAllAllowed_PreservesLoadAll()
        {
            var parameters = new ProxyRQueryParameters { LoadAll = true };
            var options = new ProxyROptions { AllowLoadAll = true };

            StatementBuilder.ApplyRequestLimits(parameters, options);

            parameters.LoadAll.Should().BeTrue();
        }

        [Fact]
        public void ApplyRequestLimits_WithNullParameters_IsNoOp()
        {
            var act = () => StatementBuilder.ApplyRequestLimits(null, new ProxyROptions());
            act.Should().NotThrow();
        }
    }
}
