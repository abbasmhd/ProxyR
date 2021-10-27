using ProxyR.Abstractions.Builder;
using ProxyR.Abstractions.Execution;
using System;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace ProxyR.Abstractions.Commands
{
    public static class DbCommands
    {
        public static DbResult ObjectExists(string connectionString, string objectName, string schemaName = "dbo", params DbObjectType[] dbObjectTypes)
        {
            var dbObjectTypeValues = dbObjectTypes.Select(DbTypes.GetDbObjectTypeSymbol);

            var sql = $"SELECT TOP 1 1 FROM SYS.OBJECTS WHERE OBJECT_ID = OBJECT_ID('[' + @schemaName + '].[' + @objectName + ']') AND TYPE IN ({Sql.Values(dbObjectTypeValues)})";

            var results = Db.Query(connectionString, sql, new { objectName, schemaName });

            return results;
        }

        public static DbResult GetParameterNames(string connectionString, string procedureName, string schemaName = "dbo")
            => Db.Query(connectionString,
            "SELECT NAME FROM SYS.PARAMETERS WHERE OBJECT_ID = OBJECT_ID('[' + @schemaName + '].[' + @procedureName + ']') ORDER BY PARAMETER_ID",
            new { procedureName, schemaName });

        public static DbResult GetParameters(string connectionString, string procedureName, string schemaName = "dbo")
            => Db.Query(connectionString,
            "SELECT * FROM SYS.PARAMETERS WHERE OBJECT_ID = OBJECT_ID('[' + @schemaName + '].[' + @procedureName + ']') ORDER BY PARAMETER_ID",
            new { procedureName, schemaName });

        public static DbResult GetParameters(DbConnection connection, string procedureName, string schemaName = "dbo")
            => Db.Query(connection,
            "SELECT * FROM SYS.PARAMETERS WHERE OBJECT_ID = OBJECT_ID('[' + @schemaName + '].[' + @procedureName + ']') ORDER BY PARAMETER_ID",
            new { procedureName, schemaName });

        public static DbResult GetColumns(DbConnection connection, string tableName, string schemaName = "dbo")
            => Db.Query(connection,
            "SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @tableName AND TABLE_SCHEMA = @schemaName",
            new { tableName, schemaName });

        public static DbResult GetColumns(string connectionString, string tableName, string schemaName = "dbo")
            => GetColumns(Db.CreateConnection(connectionString), tableName, schemaName);

        public static DbResult CreateTableColumn(string connectionString, string tableName, string fieldName, string typeSyntax, bool isNullable = true, object defaultValue = null, string schemaName = null)
        {
            var nullablePart = isNullable ? "NULL" : "NOT NULL";
            var defaultPart = defaultValue != null ? $"= {defaultValue.ToString()}" : string.Empty;

            return Db.Query(connectionString,
                $"ALTER TABLE [{schemaName ?? "dbo"}].[{tableName}] ADD [{fieldName}] {typeSyntax} {nullablePart} {defaultPart}");
        }

        public static async Task CreateNonExistingColumnsAsync(string connectionString, DataTable dataTable, string tableName)
        {
            // Add columns that do not exist.
            var columnNameTable = await GetColumns(connectionString, tableName).ToDataTableAsync();
            var columnNames = columnNameTable
                .Rows
                .Cast<DataRow>()
                .Select(row => (string)row["COLUMN_NAME"])
                .ToArray();

            // Get any columns that do not exist in the destination table.
            var nonExistingColumns = dataTable.Columns.Cast<DataColumn>()
                .Where(c => !columnNames.Contains(c.ColumnName, StringComparer.OrdinalIgnoreCase))
                .ToArray();

            // Create the non-existing columns on the table.
            foreach (var column in nonExistingColumns)
            {
                var typeSyntax = DbTypes.GetDbTypeSyntax(column.DataType);
                if (typeSyntax == null)
                {
                    continue;
                }

                await CreateTableColumn(connectionString, tableName, fieldName: column.ColumnName, typeSyntax).ExecuteAsync();
            }
        }
    }
}
