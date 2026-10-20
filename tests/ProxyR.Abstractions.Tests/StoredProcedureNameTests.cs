using FluentAssertions;
using ProxyR.Abstractions.Execution;
using Xunit;

namespace ProxyR.Abstractions.Tests
{
    /// <summary>
    /// Tests for the stored-proc name heuristic (#71).
    /// </summary>
    public class StoredProcedureNameTests
    {
        [Theory]
        [InlineData("dbo.MyProcedure")]
        [InlineData("MyProcedure")]
        [InlineData("[dbo].[MyProcedure]")]
        [InlineData("[MyProcedure]")]
        public void IsStoredProcedureName_WithValidNames_ReturnsTrue(string sql)
        {
            Db.IsStoredProcedureName(sql).Should().BeTrue();
        }

        [Theory]
        [InlineData("SELECT")]
        [InlineData("SELECT 1")]
        [InlineData("SELECT * FROM Users;--")]
        [InlineData("")]
        [InlineData(null)]
        public void IsStoredProcedureName_WithStatements_ReturnsFalse(string sql)
        {
            Db.IsStoredProcedureName(sql).Should().BeFalse();
        }
    }
}
