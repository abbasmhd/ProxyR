using Newtonsoft.Json.Linq;
using ProxyR.Abstractions.Builder;

namespace ProxyR.Abstractions.Tests
{
    public class SqlTests
    {

        [Theory]
        [InlineData("myIdentifier")]
        [InlineData("my_identifier")]
        [InlineData("__my_identifier__")]
        [InlineData("hello_world_123")]
        public void Sanitize_ReturnsSanitizedString(string identifier)
        {
            // Arrange

            // Act
            string result = Sql.Sanitize(identifier);

            // Assert
            result.Should().MatchRegex(@"^[A-Za-z0-9_]+$")
                .And.NotContain("-")
                .And.NotContain(" ");
        }

        [Fact]
        public void ColumnReferences_Returns_Correct_Format()
        {
            // Arrange
            string[] names = new[] { "ColumnA", "ColumnB", "ColumnC" };

            // Act
            string result = Sql.ColumnReferences(names);

            // Assert
            result.Should().Be("[ColumnA], [ColumnB], [ColumnC]");
        }

        [Fact]
        public void ColumnLines_Returns_Correct_Format()
        {
            // Arrange
            string[] names = new[] { "ColumnA", "ColumnB", "ColumnC" };

            // Act
            string result = Sql.ColumnLines(names);

            // Assert
            result.Should().Be("[ColumnA]\n, [ColumnB]\n, [ColumnC]");
        }

        [Fact]
        public void SplitIdentifierParts_Splits_Into_Array_Of_Strings()
        {
            // Arrange
            string chainOfParts = "Part1.Part2.Part3";

            // Act
            string[] result = Sql.SplitIdentifierParts(chainOfParts);

            // Assert
            result.Should().BeEquivalentTo(new[] { "Part1", "Part2", "Part3" });
        }

        [Theory]
        [InlineData("TableName", "dbo", "TableName")]
        [InlineData("Schema.TableName", "Schema", "TableName")]
        public void GetSchemaAndObjectName_ShouldReturnCorrectResult(string identifier, string expectedSchema, string expectedObject)
        {
            // Act
            (string Schema, string Object) = Sql.GetSchemaAndObjectName(identifier);

            // Assert
            Schema.Should().Be(expectedSchema);
            Object.Should().Be(expectedObject);
        }

        [Fact]
        public void GetSchemaAndObjectName_ShouldThrowException_WhenGivenInvalidSqlIdentifier()
        {
            // Arrange
            var invalidIdentifier = "Invalid.Identifier.Test";

            // Act
            Func<(string Schema, string Object)> act = () => Sql.GetSchemaAndObjectName(invalidIdentifier);

            // Assert
            act.Should().ThrowExactly<InvalidOperationException>()
                .WithMessage($"The given SQL identifier [{invalidIdentifier}] cannot be split.");
        }

        [Fact]
        public void GetPropertyValues_ShouldReturnCorrectValues()
        {
            // Arrange
            var obj = new { Prop1 = "value1", Prop2 = 2 };
            var properties = obj.GetType().GetProperties();

            // Act
            var result = Sql.GetPropertyValues(obj, properties);

            // Assert
            result[0].Should().BeEquivalentTo(obj.Prop1);
            result[1].Should().BeEquivalentTo(obj.Prop2);
        }

        [Fact]
        public void ParenthesisLines_ShouldReturnCorrectResult()
        {
            // Arrange
            var contents = new[] { "Line 1", "Line 2", "Line 3" };
            var expected = $"({contents[0]})\n, ({contents[1]})\n, ({contents[2]})";

            // Act
            var result = Sql.ParenthesisLines(contents);

            // Assert
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData(new object[] { "value1", 2 }, "'value1', 2")]
        [InlineData(new object[] { "value2", 3, true }, "'value2', 3, 1")]
        [InlineData(new object?[] { null }, "NULL")]
        public void Values_ShouldReturnCorrectResult(object?[] testData, string expected)
        {
            // Arrange
            var values = (IEnumerable<object?>)testData;

            // Act
            var result = Sql.Values(values);

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public void Values_WhenObject_ShouldReturnCorrectResult()
        {
            // Arrange
            var values = new object?[] { new JValue(123), new JValue(2.5f), new JValue("string"), new JValue(true), new JValue(DateTime.Parse("2023-05-01")), new JValue(Guid.Parse("e4279bc6-9e39-486a-9565-284d33b66b84")), new JValue(new byte[] { 0x01, 0x02, 0x03 }) };
            var expected = "123, 2.5, 'string', 1, '2023-05-01 00:00:00.000', 'e4279bc6-9e39-486a-9565-284d33b66b84', CONVERT(VARBINARY(MAX), '0x010203', 1)";

            // Act
            var result = Sql.Values(values);

            // Assert
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData("string", "'string'")]
        [InlineData(123, "123")]
        [InlineData(2.5f, "2.5")]
        [InlineData(true, "1")]
        [InlineData((object?)null, "NULL")]
        [InlineData("string with 'single quote'", "'string with ''single quote'''")]
        [InlineData("string with newline\n", "'string with newline\n'")]
        [InlineData("string with carriage return\r", "'string with carriage return\r'")]
        [InlineData("string with tab\t", "'string with tab\t'")]
        [InlineData("string with backslash\\", "'string with backslash\\'")]
        [InlineData("2023-05-01T00:00:00Z", "'2023-05-01T00:00:00Z'")]
        public void Quote_ShouldReturnCorrectResult(object? input, string expected)
        {
            // Act
            var result = Sql.Quote(input);

            // Assert
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData(new byte[] { 0x01, 0x02, 0x03 }, "010203")]
        [InlineData(new byte[] { 0xff, 0x00, 0xaa }, "FF00AA")]
        [InlineData(new byte[] { 0x00 }, "00")]
        [InlineData(new byte[] { }, "")]
        public void BytesToHex_ShouldReturnCorrectResult(byte[] input, string expected)
        {
            // Act
            var result = Sql.BytesToHex(input);

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public void SelectQuoted_ShouldReturnCorrectResult()
        {
            // Arrange
            var values = new object?[] { "value1", 2, 3.5f, null, new JValue(DateTime.Parse("2023-05-01")), new JValue(true), new byte[] { 0x01, 0x02, 0x03 }, Guid.Parse("e4279bc6-9e39-486a-9565-284d33b66b84") };

            var expected = new[] { "'value1'", "2", "3.5", "NULL", "'2023-05-01 00:00:00.000'", "1", "CONVERT(VARBINARY(MAX), '0x010203', 1)", "'e4279bc6-9e39-486a-9565-284d33b66b84'" };

            // Act
            var results = Sql.SelectQuoted(values);

            // Assert
            foreach (var result in results)
            {
                expected.Should().Contain(result);
            }
        }

        [Theory]
        [InlineData("Id", "int",           true,  "0", 15, true,  "Latin1_General_CI_AS", "[Id]            INT                                   NULL = 0")]
        [InlineData("Id", "int",           false, "0", 15, true,  "Latin1_General_CI_AS", "[Id]            INT                                   NOT NULL = 0")]
        [InlineData("Id", "int",           null,  "0", 15, true,  "Latin1_General_CI_AS", "[Id]            INT                                        = 0")]
        [InlineData("Id", "int",           true,  "0", 15, false, "",                     "[Id] INT  NULL = 0")]
        [InlineData("Id", "varchar(Max)",  true,  "",  15, true,  "Latin1_General_CI_AS", "[Id]            VARCHAR(MAX)     COLLATE Latin1_General_CI_AS NULL = ''")]
        [InlineData("Id", "varchar(100)",  true,  "",  15, true,  "Latin1_General_CI_AS", "[Id]            VARCHAR(100)     COLLATE Latin1_General_CI_AS NULL = ''")]
        [InlineData("Id", "nvarchar(100)", true,  "",  15, true,  "Latin1_General_CI_AS", "[Id]            NVARCHAR(100)    COLLATE Latin1_General_CI_AS NULL = ''")]
        [InlineData("Id", "nvarchar(100)", false, "",  15, false, "Latin1_General_CI_AS", "[Id] NVARCHAR(100) COLLATE Latin1_General_CI_AS NOT NULL = ''")]
        [InlineData("Id", "nvarchar(100)", false, "",  15, false, "",                     "[Id] NVARCHAR(100) COLLATE DATABASE_DEFAULT NOT NULL = ''")]
        public void ColumnDefinition_ReturnsCorrectResult(string columnName, string type, bool? isNullable, string defaultExpression, int columnNamePadding, bool doPadding, string? collation, string expected)
        {
            // Act
            string result = new ColumnDefinitionBuilder(columnName, type)
                .IsNullable(isNullable)
                .DefaultExpression(defaultExpression)
                .ColumnNamePadding(columnNamePadding)
                .DoPadding(doPadding)
                .Collation(collation)
                .Build();

            // Assert
            result.Should().Be(expected);
        }
    }

}