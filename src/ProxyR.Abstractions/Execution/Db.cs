using ProxyR.Abstractions.Commands;
using ProxyR.Abstractions.Utilities;
using ProxyR.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;


namespace ProxyR.Abstractions.Execution
{
    /// <summary>
    ///     Central place to run ad-hoc queries.
    /// </summary>
    public static class Db
    {
        /// <summary>
        ///     Inserts all the rows from a data-table into a target database table.
        /// </summary>
        /// <param name="connectionString">The connection-string to the database.</param>
        /// <param name="sourceTable">The table containing the source records to insert.</param>
        /// <param name="tableName">The name of the table to insert the records into.</param>
        public static async Task BulkCopyAsync(string connectionString, DataTable sourceTable, string tableName)
        {
            using (var connection = CreateConnection(connectionString))
            {
                await connection.OpenAsync();
                await BulkCopyAsync((SqlConnection)connection, sourceTable, tableName);
            }
        }

        /// <summary>
        ///     Inserts all the rows from a data-table into a target database table.
        /// </summary>
        /// <param name="connection">The connection to the database where the records are to be inserted.</param>
        /// <param name="sourceTable">The table containing the source records to insert.</param>
        /// <param name="tableName">The name of the table to insert the records into.</param>
        /// <param name="transaction">The optional transaction to encapsulate the inserts into.</param>
        public static async Task BulkCopyAsync(SqlConnection connection, DataTable sourceTable, string tableName, SqlTransaction transaction = null)
        {
            using (var bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, transaction))
            {

                // Set the target table name.
                bulkCopy.BulkCopyTimeout = 28800;
                bulkCopy.DestinationTableName = tableName;

                // Get the columns.
                var table = await DbCommands.GetColumns(connection, tableName)
                    .ToDataTableAsync(closeConnection: false, transaction: transaction);

                var columns = table.Rows
                    .Cast<DataRow>()
                    .Select(row => (string)row["COLUMN_NAME"])
                    .ToArray();

                // Add the mappings, assuming the source and destination column names are the same.
                foreach (DataColumn column in sourceTable.Columns)
                {
                    // Find the target column, so we can get the correct casing.
                    var targetColumnName = columns.FirstOrDefault(c => c.Equals(column.ColumnName, StringComparison.OrdinalIgnoreCase));

                    if (targetColumnName == null)
                    {
                        continue;
                    }

                    // Map the source-cased column to the target cased column.
                    bulkCopy.ColumnMappings.Add(column.ColumnName, targetColumnName);
                }

                // Write the table to the server.
                await bulkCopy.WriteToServerAsync(sourceTable);

            }
        }

        /// <summary>
        ///     Creates an SQL query using an already created connection.
        ///     The query will not actually be executed, but will be deferred for the caller to execute the query.
        /// </summary>
        /// <param name="connection">The connection to use when the returning result executes the query.</param>
        /// <param name="sql">The name of the procedure or the SQL text to execute.</param>
        /// <param name="parameters">
        ///     Parameters can be given as an ordered list, an anonymous object of parameters, or a dictionary
        ///     of parameters.
        /// </param>
        /// <returns>DbResult that offers numerous ways to execute the command and receive the results.</returns>
        public static DbResult Query(DbConnection connection, string sql, params object[] parameters)
        {
            var commandFactory = CreateCommandFactory(connection, sql, parameters);
            var result = new DbResult(commandFactory);

            return result;
        }

        /// <summary>
        ///     Creates an SQL query using a fresh connection.
        ///     The query will not actually be executed, but will be deferred for the caller to execute the query.
        /// </summary>
        /// <param name="connectionString">The connection-string to the database.</param>
        /// <param name="sql">The name of the procedure or the SQL text to execute.</param>
        /// <param name="parameters">
        ///     Parameters can be given as an ordered list, an anonymous object of parameters, or a dictionary
        ///     of parameters.
        /// </param>
        /// <returns>DbResult that offers numerous ways to execute the command and receive the results.</returns>
        public static DbResult Query(string connectionString, string sql, params object[] parameters)
        {
            var commandFactory = CreateCommandFactory(connectionString, sql, parameters);
            var result = new DbResult(commandFactory);

            return result;
        }

        /// <summary>
        ///     Creates a command factory, whereby the connection has already been created.
        /// </summary>
        private static Func<Task<DbCommandFactoryResult>> CreateCommandFactory(DbConnection connection, string sql, params object[] parameters)
        {
            var result = (Func<Task<DbCommandFactoryResult>>)(async () =>
            {
                var command = CreateCommand(connection, sql, parameters);

                if (connection.State == ConnectionState.Closed)
                {
                    await connection.OpenAsync();
                }

                return new DbCommandFactoryResult
                {
                    OwnsConnection = false,
                    Connection = connection,
                    Command = command,
                    ConnectionString = connection.ConnectionString
                };
            });

            return result;
        }

        /// <summary>
        ///     Creates a command factory, that also creates and opens a new connection.
        /// </summary>
        public static Func<Task<DbCommandFactoryResult>> CreateCommandFactory(string connectionString, string sql, params object[] parameters)
        {
            var result = (Func<Task<DbCommandFactoryResult>>)(async () =>
            {
                var connection = CreateConnection(connectionString);
                var command = CreateCommand(connection, sql, parameters);
                await connection.OpenAsync();

                return new DbCommandFactoryResult
                {
                    OwnsConnection = true,
                    Connection = connection,
                    Command = command,
                    ConnectionString = connectionString
                };
            });

            return result;
        }

        /// <summary>
        ///     Creates a new connection based upon a given connection-string or a configured connection-string.
        /// </summary>
        public static DbConnection CreateConnection(string connectionString)
        {
            // Create the connection.
            var connection = new SqlConnection(connectionString);

            return connection;
        }

        /// <summary>
        ///     Creates a command that can be executed from the main parameters.
        /// </summary>
        public static DbCommand CreateCommand(DbConnection connection, string sql, params object[] parameters)
        {
            // Setup the command.
            var command = connection.CreateCommand();
            command.CommandText = sql;

            // Does the command look like a stored procedure?
            // Does it have any spaces inside it?
            if (!sql.Contains(" "))
            {
                command.CommandType = CommandType.StoredProcedure;
            }

            // No parameters to add?
            if (parameters == null || parameters.Length == 0)
            {
                return command;
            }

            // Is there a single parameter object?
            if (parameters.Length == 1
                && !(parameters[0] is object[])
                && !parameters[0].GetType().IsDbPrimitive())
            {

                // Is it not a dictionary? If not, we'll convert into one.
                if (!(parameters[0] is IDictionary<string, object> dictionary))
                {
                    dictionary = ConversionUtility.ObjectToDictionary(parameters[0], useDbNulls: true);
                }

                // Must be an object, of whom's properties are the parameters.
                foreach (var (key, o) in dictionary)
                {
                    // Convert null to DBNull.
                    var value = o ?? DBNull.Value;
                    command.Parameters.Add(new SqlParameter(key, value));
                }

                return command;
            }

            // Add each one as a parameter.
            for (var parameterIndex = 0; parameterIndex < parameters.Length; parameterIndex++)
            {
                command.Parameters.Add(new SqlParameter($"@{parameterIndex}", parameters[parameterIndex]));
            }

            return command;
        }

    }

}
