using System;
using System.Data;
using FluentAssertions;
using ProxyR.Abstractions.Builder;
using ProxyR.Abstractions.Commands;
using Xunit;

namespace ProxyR.Abstractions.Tests
{
    /// <summary>
    /// Tests for identifier sanitization in the DDL builder helpers (#61).
    /// </summary>
    public class DdlBuilderTests
    {
        [Fact]
        public void CreateTable_WithInjectedTableName_IsSanitized()
        {
            var builder = new SqlBuilder()
                .CreateTable("Users]; DROP TABLE Secrets;--", "[Id] INT");

            var sql = builder.ToString();

            sql.Should().NotContain("DROP TABLE Secrets");
            sql.Should().Contain("[dbo].[Users___DROP_TABLE_Secrets___]");
        }

        [Fact]
        public void CreateTable_WithTwoPartName_PreservesBothParts()
        {
            var builder = new SqlBuilder()
                .CreateTable("sales.Customers", "[Id] INT");

            var sql = builder.ToString();

            sql.Should().Contain("[sales].[Customers]");
        }

        [Fact]
        public void DropTableIfExists_WithInjectedName_IsSanitized()
        {
            var builder = new SqlBuilder()
                .DropTableIfExists("Users]; SELECT 1--");

            var sql = builder.ToString();

            sql.Should().NotContain("SELECT 1--");
            sql.Should().Contain("[dbo].[Users___SELECT_1__]");
        }

        [Fact]
        public void AddColumn_WithInjectedColumnName_IsSanitized()
        {
            var builder = new SqlBuilder()
                .AddColumn("dbo.Users", "Name]; ALTER TABLE x;--", "NVARCHAR(100)");

            var sql = builder.ToString();

            sql.Should().NotContain("ALTER TABLE x");
            sql.Should().Contain("[Name___ALTER_TABLE_x___]");
        }

        [Fact]
        public void AddPrimaryKeyConstraint_WithInjectedColumnName_IsSanitized()
        {
            var builder = new SqlBuilder()
                .AddPrimaryKeyConstraint("PK_Users", "Users", "Id] DROP TABLE x;--");

            var sql = builder.ToString();

            sql.Should().NotContain("DROP TABLE x");
            sql.Should().Contain("[Id__DROP_TABLE_x___]");
        }

        [Fact]
        public void BuildCreateTableColumnSql_WithInjectedFieldName_IsSanitized()
        {
            var sql = DbCommands.BuildCreateTableColumnSql(
                "Users]; DROP TABLE Secrets;--",
                "Name]; SELECT 1;--",
                "NVARCHAR(100)");

            sql.Should().NotContain("DROP TABLE Secrets");
            sql.Should().NotContain("SELECT 1");
            sql.Should().StartWith("ALTER TABLE [dbo].[Users___DROP_TABLE_Secrets___]");
            sql.Should().Contain("[Name___SELECT_1___]");
        }

        [Fact]
        public void BuildCreateTableColumnSql_WithInjectedTypeSyntax_Throws()
        {
            var act = () => DbCommands.BuildCreateTableColumnSql(
                "Users",
                "Name",
                "NVARCHAR(100); DROP TABLE Secrets;--");

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void BuildCreateTableColumnSql_WithNonPrimitiveDefault_Throws()
        {
            var act = () => DbCommands.BuildCreateTableColumnSql(
                "Users",
                "Name",
                "NVARCHAR(100)",
                defaultValue: new { Evil = "--" });

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void BuildCreateTableColumnSql_WithStringDefault_IsQuoted()
        {
            var sql = DbCommands.BuildCreateTableColumnSql(
                "Users",
                "Name",
                "NVARCHAR(100)",
                defaultValue: "O'Brien");

            sql.Should().Contain("= 'O''Brien'");
        }

        [Fact]
        public void BuildCreateTableColumnSql_WithExplicitSchema_IsUsed()
        {
            var sql = DbCommands.BuildCreateTableColumnSql(
                "Users", "Name", "INT", schemaName: "sales");

            sql.Should().StartWith("ALTER TABLE [sales].[Users]");
        }
    }
}
