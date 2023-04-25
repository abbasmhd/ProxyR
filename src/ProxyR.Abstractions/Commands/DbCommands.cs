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
        /// <summary>
        /// Checks if an object exists in the database.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="objectName">Name of the object.</param>
        /// <param name="schemaName">Name of the schema.</param>
        /// <param name="dbObjectTypes">The database object types.</param>
        /// <returns>
        /// A <see cref="DbResult"/> object containing the result of the query.
        /// </returns>
        public static DbResult ObjectExists(string connectionString, string objectName, string schemaName = "dbo", params DbObjectType[] dbObjectTypes)
        {
            var dbObjectTypeValues = dbObjectTypes.Select(DbTypes.GetDbObjectTypeSymbol);
            var sql = $"SELECT TOP 1 1 FROM SYS.OBJECTS WHERE OBJECT_ID = OBJECT_ID('[' + @schemaName + '].[' + @objectName + ']') AND TYPE IN ({Sql.Values(dbObjectTypeValues)})";
            var results = Db.Query(connectionString, sql, new { objectName, schemaName });
            return results;
        }

        /// <summary>
        /// Retrieves the object type from the specified connection string, object name, and schema name.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="objectName">Name of the object.</param>
        /// <param name="schemaName">Name of the schema.</param>
        /// <param name="dbObjectTypes">The database object types.</param>
        /// <returns>
        /// The object type.
        /// </returns>
        public static DbResult GetObjectType(string connectionString, string objectName, string schemaName = "dbo", params DbObjectType[] dbObjectTypes)
        {
            var dbObjectTypeValues = dbObjectTypes.Select(DbTypes.GetDbObjectTypeSymbol);
            var sql = $"SELECT TOP 1 TYPE FROM SYS.OBJECTS WHERE OBJECT_ID = OBJECT_ID('[' + @schemaName + '].[' + @objectName + ']') AND TYPE IN ({Sql.Values(dbObjectTypeValues)})";
            var results = Db.Query(connectionString, sql, new { objectName, schemaName });
            return results;
        }

        /// <summary>
        /// Retrieves the parameter names of a stored procedure.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="procedureName">The name of the stored procedure.</param>
        /// <param name="schemaName">The schema name of the stored procedure.</param>
        /// <returns>A <see cref="DbResult"/> containing the parameter names.</returns>
        public static DbResult GetParameterNames(string connectionString, string procedureName, string schemaName = "dbo")
            => Db.Query(connectionString,
            "SELECT NAME FROM SYS.PARAMETERS WHERE OBJECT_ID = OBJECT_ID('[' + @schemaName + '].[' + @procedureName + ']') ORDER BY PARAMETER_ID",
            new { procedureName, schemaName });

        /// <summary>
        /// Retrieves the parameters of a stored procedure.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="procedureName">The name of the stored procedure.</param>
        /// <param name="schemaName">The schema name of the stored procedure.</param>
        /// <returns>A <see cref="DbResult"/> containing the parameters of the stored procedure.</returns>
        public static DbResult GetParameters(string connectionString, string procedureName, string schemaName = "dbo")
            => Db.Query(connectionString,
            "SELECT * FROM SYS.PARAMETERS WHERE OBJECT_ID = OBJECT_ID('[' + @schemaName + '].[' + @procedureName + ']') ORDER BY PARAMETER_ID",
            new { procedureName, schemaName });

        /// <summary>
        /// Retrieves the parameters of a stored procedure.
        /// </summary>
        /// <param name="connection">The database connection.</param>
        /// <param name="procedureName">The name of the stored procedure.</param>
        /// <param name="schemaName">The schema of the stored procedure.</param>
        /// <returns>
        /// A <see cref="DbResult"/> containing the parameters of the stored procedure.
        /// </returns>
        public static DbResult GetParameters(DbConnection connection, string procedureName, string schemaName = "dbo")
            => Db.Query(connection,
            "SELECT * FROM SYS.PARAMETERS WHERE OBJECT_ID = OBJECT_ID('[' + @schemaName + '].[' + @procedureName + ']') ORDER BY PARAMETER_ID",
            new { procedureName, schemaName });

        /// <summary>
        /// Retrieves the columns of a given table from the database.
        /// </summary>
        /// <param name="connection">The database connection.</param>
        /// <param name="tableName">The name of the table.</param>
        /// <param name="schemaName">The name of the schema (defaults to "dbo").</param>
        /// <returns>A <see cref="DbResult"/> containing the columns of the table.</returns>
        public static DbResult GetColumns(DbConnection connection, string tableName, string schemaName = "dbo")
            => Db.Query(connection,
            "SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @tableName AND TABLE_SCHEMA = @schemaName",
            new { tableName, schemaName });

        /// <summary>
        /// Gets the columns of a table from a given connection string, table name, and schema name.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="tableName">The name of the table.</param>
        /// <param name="schemaName">The name of the schema (defaults to "dbo").</param>
        /// <returns>A <see cref="DbResult"/> containing the columns of the table.</returns>
        public static DbResult GetColumns(string connectionString, string tableName, string schemaName = "dbo")
            => GetColumns(Db.CreateConnection(connectionString), tableName, schemaName);

        /// <summary>
        /// Creates a new column in a table with the specified parameters.
        /// </summary>
        /// <param name="connectionString">The connection string to the database.</param>
        /// <param name="tableName">The name of the table to add the column to.</param>
        /// <param name="fieldName">The name of the column to add.</param>
        /// <param name="typeSyntax">The type syntax of the column.</param>
        /// <param name="isNullable">Whether the column is nullable.</param>
        /// <param name="defaultValue">The default value of the column.</param>
        /// <param name="schemaName">The schema name of the table.</param>
        /// <returns>The result of the query.</returns>
        public static DbResult CreateTableColumn(string connectionString, string tableName, string fieldName, string typeSyntax, bool isNullable = true, object defaultValue = null, string schemaName = null)
        {
            var nullablePart = isNullable ? "NULL" : "NOT NULL";
            var defaultPart = defaultValue != null ? $"= {defaultValue.ToString()}" : string.Empty;

            return Db.Query(connectionString,
                $"ALTER TABLE [{schemaName ?? "dbo"}].[{tableName}] ADD [{fieldName}] {typeSyntax} {nullablePart} {defaultPart}");
        }

        /// <summary>
        /// Creates any columns that do not exist in the destination table.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="dataTable">The data table.</param>
        /// <param name="tableName">The table name.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public static async Task CreateNonExistingColumnsAsync(string connectionString, DataTable dataTable, string tableName)
        {
            // Add columns that do not exist.
            var columnNameTable = await GetColumns(connectionString, tableName).ToDataTableAsync().ConfigureAwait(false);
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

                await CreateTableColumn(connectionString, tableName, fieldName: column.ColumnName, typeSyntax).ExecuteAsync().ConfigureAwait(false);
            }
        }
    }
}
