using System.ComponentModel.DataAnnotations;
using System.Data;
using FluentAssertions;
using ProxyR.Abstractions.Extensions;

namespace ProxyR.Abstractions.Tests
{
    public class DataTableExtensionsTests
    {
        [Fact]
        public void AddColumns_ShouldAddColumnsToDataTable()
        {
            // Arrange
            var table = new DataTable();

            // Act
            table.AddColumns<TestEntity>();

            // Assert
            table.Columns.Should().HaveCount(3);
            table.Columns.Should().Contain(c => c.ColumnName == "Id" && c.DataType == typeof(int) && c.AllowDBNull == false && c.Unique == true);
            table.Columns.Should().Contain(c => c.ColumnName == "Name" && c.DataType == typeof(string) && c.MaxLength == 50 && c.AllowDBNull == false);
            table.Columns.Should().Contain(c => c.ColumnName == "Age" && c.DataType == typeof(int) && c.AllowDBNull == true);

            table.PrimaryKey.Should().HaveCount(1);
            table.PrimaryKey.Should().Contain(c => c.ColumnName == "Id");
        }

        [Fact]
        public void AddColumns_ShouldNotAddExistingColumnsToDataTable()
        {
            // Arrange
            var table = new DataTable();
            table.Columns.Add("Id", typeof(int));

            // Act
            table.AddColumns<TestEntity>();

            // Assert
            table.Columns.Should().HaveCount(3);
            table.Columns.Should().Contain(c => c.ColumnName == "Id" && c.DataType == typeof(int));
        }

        [Fact]
        public void AddRow_ShouldAddRowToDataTable()
        {
            // Arrange
            var table = new DataTable();
            table.Columns.Add("Id", typeof(int));
            table.Columns.Add("Name", typeof(string));
            var row = table.NewRow();
            row["Id"] = 1;
            row["Name"] = "John";

            // Act
            var addedRow = table.AddRow(row);

            // Assert
            table.Rows.Should().HaveCount(1);
            table.Rows.Should().Contain(addedRow);
            addedRow["Id"].Should().Be(1);
            addedRow["Name"].Should().Be("John");
        }

        [Fact]
        public void AddRow_ShouldThrowException_WhenRowIsNull()
        {
            // Arrange
            var table = new DataTable();

            // Act & Assert
            table.Invoking(t => t.AddRow(null))
            .Should().Throw<ArgumentNullException>()
            .WithMessage("'row' argument cannot be null. (Parameter 'row')");
        }

        [Fact]
        public void AddRow_ShouldAddRowToDataTableAsTestEntity()
        {
            // Arrange
            var table = new DataTable();
            table.AddColumns<TestEntity>();
            var entity = new TestEntity() { Id = 1, Age = 26, Name = "Test" };

            // Act
            var result = table.AddRow(entity);

            // Assert
            result.Should().NotBeNull();
            table.Rows.Should().Contain(result);
        }

        [Fact]
        public void AddRow_ShouldReturnDataRowAsTestEntity()
        {
            // Arrange
            var table = new DataTable();
            table.AddColumns<TestEntity>();
            var entity = new TestEntity() { Id = 1, Age = 26, Name = "Test" };

            // Act
            var result = table.AddRow(entity);

            // Assert
            result.Should().BeOfType<DataRow>();
        }

        [Fact]
        public void AddRow_ShouldReturnDataRowWithCorrectTable()
        {
            // Arrange
            var table = new DataTable();
            table.AddColumns<TestEntity>();
            var entity = new TestEntity() { Id = 1, Age = 26, Name = "Test" };

            // Act
            var result = table.AddRow(entity);

            // Assert
            result.Table.Should().BeEquivalentTo(table);
        }

        private class TestEntity
        {
            [Key]
            public int Id { get; set; }
            [MaxLength(50)]
            [Required]
            public string? Name { get; set; }
            public int Age { get; set; }
        }
    }
}