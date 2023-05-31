using ProxyR.Abstractions.Extensions;
using ProxyR.Abstractions.Utilities;
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
    /// Allows execution of an SQL command in a variety of ways that allows the caller to decide.
    /// </summary>
    public class DbResult
    {
        private readonly Func<Task<DbCommandFactoryResult>> _commandFactory;
        private DbTransaction _transaction;
        private int _timeout = 30;
        private Action<string> _messageReceiver;

        /// <summary>
        /// Constructor for DbResult class.
        /// </summary>
        /// <param name="commandFactory">Function to create a DbCommandFactoryResult.</param>
        /// <returns>
        /// An instance of the DbResult class.
        /// </returns>
        public DbResult(Func<Task<DbCommandFactoryResult>> commandFactory)
        {
            _commandFactory = commandFactory;
        }

        /// <summary>
        /// Sets the action to be called when a message is received.
        /// </summary>
        /// <param name="onMessage">The action to be called when a message is received.</param>
        /// <returns>The current instance of the <see cref="DbResult"/> class.</returns>
        public DbResult WithMessageReceiver(Action<string> onMessage)
        {
            _messageReceiver = onMessage;
            return this;
        }

        /// <summary>
        /// Set the timeout of executed commands.
        /// </summary>
        /// <param name="timeout">Number of seconds before the command will timeout.</param>
        public DbResult WithTimeout(int timeout)
        {
            _timeout = timeout;
            return this;
        }

        /// <summary>
        /// Sets the transaction for the next executed command.
        /// </summary>
        public DbResult WithTransaction(DbTransaction transaction)
        {
            _transaction = transaction;
            return this;
        }

        /// <summary>
        /// Executes and receives the first entity from the result-set.
        /// </summary>
        public TEntity FirstOrDefault<TEntity>() where TEntity : class, new()
        {
            var result = GetAllEntities<TEntity>();
            return result.FirstOrDefault();
        }

        /// <summary>
        /// Executes and receives the first entity from the result-set.
        /// </summary>
        public async Task<TEntity> FirstOrDefaultAsync<TEntity>() where TEntity : class, new()
        {
            var result = await GetAllEntitiesAsync<TEntity>().ConfigureAwait(false);
            return result.FirstOrDefault();
        }

        /// <summary>
        /// Executes and receives the first entity from the result-set that matches the given predicate.
        /// The predicate is filtered locally, not on the server.
        /// </summary>
        public TEntity FirstOrDefault<TEntity>(Func<TEntity, bool> predicate) where TEntity : class, new()
        {
            var result = GetAllEntities<TEntity>();
            return result.FirstOrDefault(predicate);
        }

        /// <summary>
        /// Executes and receives the first entity from the result-set that matches the given predicate.
        /// The predicate is filtered locally, not on the server.
        /// </summary>
        public async Task<TEntity> FirstOrDefaultAsync<TEntity>(Func<TEntity, bool> predicate) where TEntity : class, new()
        {
            var result = await GetAllEntitiesAsync<TEntity>().ConfigureAwait(false);
            return result.FirstOrDefault(predicate);
        }

        /// <summary>
        /// Executes and receives all the results as a list of entities.
        /// </summary>
        public List<TEntity> ToList<TEntity>() where TEntity : class, new()
        {
            var results = GetAllEntities<TEntity>();
            return results.ToList();
        }

        /// <summary>
        /// Executes and receives all the results as a list of entities.
        /// </summary>
        public async Task<List<TEntity>> ToListAsync<TEntity>() where TEntity : class, new()
        {
            var results = await GetAllEntitiesAsync<TEntity>().ConfigureAwait(false);
            return results.ToList();
        }

        /// <summary>
        /// Executes and receives all the results as an array of entities.
        /// </summary>
        public TEntity[] ToArray<TEntity>() where TEntity : class, new()
        {
            var results = GetAllEntities<TEntity>();
            return results.ToArray();
        }

        /// <summary>
        /// Executes and receives all the results as an array of entities.
        /// </summary>
        public async Task<TEntity[]> ToArrayAsync<TEntity>() where TEntity : class, new()
        {
            var results = await GetAllEntitiesAsync<TEntity>().ConfigureAwait(false);
            return results.ToArray();
        }

        public IEnumerable<TEntity> ToEnumerable<TEntity>() where TEntity : class, new()
        {
            var map = DbEntityMap.GetOrCreate<TEntity>();
            return EnumerateReaderEntities<TEntity>(map);
        }

        private IEnumerable<TEntity> EnumerateReaderEntities<TEntity>(DbEntityMap map) where TEntity : class, new()
        {
            using (var reader = ToDataReaderAsync().Result)
            {
                while (reader.Read())
                {
                    var entity = reader.ToEntity<TEntity>(map);
                    yield return entity;
                }
            }
        }

        private IEnumerable<TEntity> GetAllEntities<TEntity>() where TEntity : class, new()
        {
            // Select the results into a DataTable.
            var table = ToDataTable();

            // Create the map now, so that we
            var map = DbEntityMap.GetOrCreate<TEntity>();

            // Transform into entities.
            var results = table
                .Rows
                .Cast<DataRow>()
                .Select(s => s.ToEntity<TEntity>(map));

            return results;
        }

        private async Task<IEnumerable<TEntity>> GetAllEntitiesAsync<TEntity>() where TEntity : class, new()
        {
            // Select the results into a DataTable.
            var table = await ToDataTableAsync().ConfigureAwait(false);

            // Create the map now, so that we
            var map = DbEntityMap.GetOrCreate<TEntity>();

            // Transform into entities.
            var results = table
                .Rows
                .Cast<DataRow>()
                .Select(s => s.ToEntity<TEntity>(map));

            return results;
        }

        /// <summary>
        /// Executes the command without pulling any results.
        /// </summary>
        public int Execute(DbTransaction transaction = null)
        {
            using (var commandFactoryResult = _commandFactory().Result)
            {
                var command = commandFactoryResult.Command;
                ApplyCommandSettings(commandFactoryResult, transaction);
                var rowsAffected = command.ExecuteNonQuery();
                return rowsAffected;
            }
        }

        /// <summary>
        /// Executes the command without pulling any results.
        /// </summary>
        public async Task<int> ExecuteAsync(DbTransaction transaction = null)
        {
            using (var commandFactoryResult = await _commandFactory().ConfigureAwait(false))
            {
                var command = commandFactoryResult.Command;
                ApplyCommandSettings(commandFactoryResult, transaction);
                var rowsAffected = await command.ExecuteNonQueryAsync().ConfigureAwait(false);
                return rowsAffected;
            }
        }

        /// <summary>
        /// Executes the command and pulls the first-field of the first-row.
        /// </summary>
        public TScalar ToScalar<TScalar>()
        {
            using (var commandFactoryResult = _commandFactory().Result)
            {
                var command = commandFactoryResult.Command;
                ApplyCommandSettings(commandFactoryResult);
                var value = command.ExecuteScalar();
                var convertedValue = (TScalar)ConversionUtility.Convert(value, typeof(TScalar));
                return convertedValue;
            }
        }

        /// <summary>
        /// Executes the command and pulls the first-field of the first-row.
        /// </summary>
        public async Task<TScalar> ToScalarAsync<TScalar>()
        {
            using (var commandFactoryResult = await _commandFactory().ConfigureAwait(false))
            {
                var command = commandFactoryResult.Command;
                ApplyCommandSettings(commandFactoryResult);
                var value = await command.ExecuteScalarAsync().ConfigureAwait(false);
                var convertedValue = (TScalar)ConversionUtility.Convert(value, typeof(TScalar));
                return convertedValue;
            }
        }

        /// <summary>
        /// Gets an array of the first column.
        /// </summary>
        public async Task<TScalar[]> ToScalarArrayAsync<TScalar>()
        {
            var result = await ToScalarListAsync<TScalar>().ConfigureAwait(false);
            return result.ToArray();
        }

        /// <summary>
        /// Gets a list of the first column.
        /// </summary>
        public async Task<IList<TScalar>> ToScalarListAsync<TScalar>()
        {
            var list = new List<TScalar>();

            using (var commandFactoryResult = await _commandFactory().ConfigureAwait(false))
            {
                var command = commandFactoryResult.Command;
                ApplyCommandSettings(commandFactoryResult);

                using (var reader = await command.ExecuteReaderAsync(CommandBehavior.CloseConnection).ConfigureAwait(false))
                {
                    while (await reader.ReadAsync().ConfigureAwait(false))
                    {
                        var value = reader[0];
                        var convertedValue = (TScalar)Convert.ChangeType(value, typeof(TScalar));
                        list.Add(convertedValue);
                    }
                }
            }

            return list;
        }

        /// <summary>
        /// Executes the command and streams the results as they come-in in the form of DataRows.
        /// </summary>
        public IEnumerable<DataRow> ToRowEnumerable()
        {
            using (var commandFactoryResult = _commandFactory().Result)
            {
                var command = commandFactoryResult.Command;
                ApplyCommandSettings(commandFactoryResult);

                using (var reader = command.ExecuteReader(CommandBehavior.CloseConnection))
                {
                    var table = new DataTable();
                    var hasDiscoveredColumnTypes = false;

                    // Create the columns from the column names.
                    CreateTableColumns(reader, table);

                    // Read each row into the table.
                    while (reader.Read())
                    {
                        // Have we set the column-types yet?
                        if (!hasDiscoveredColumnTypes)
                        {
                            SetTableColumnTypes(reader, table);
                            hasDiscoveredColumnTypes = true;
                        }

                        // Read the values into the row, and yield the row, without adding to the table.
                        var row = table.NewRow();
                        reader.GetValues(row.ItemArray);

                        yield return row;
                    }
                }
            }
        }

        /// <summary>
        /// Executes the command, and returns a reader to receive the data.
        /// </summary>
        /// <param name="closeConnection"></param>
        /// <returns></returns>
        public async Task<DbDataReader> ToDataReaderAsync(bool closeConnection = true)
        {
            var commandFactoryResult = await _commandFactory().ConfigureAwait(false);
            var command = commandFactoryResult.Command;
            ApplyCommandSettings(commandFactoryResult);

            var reader = await command.ExecuteReaderAsync(closeConnection ? CommandBehavior.CloseConnection : CommandBehavior.Default).ConfigureAwait(false);
            return reader;
        }

        /// <summary>
        /// Executes the command and pulls in all the results into a DataTable for a single result-set.
        /// </summary>
        public DataTable ToDataTable(bool closeConnection = true, DbTransaction transaction = null)
        {
            // Read the first table only.
            var dataSet = ToDataSet(1, closeConnection, transaction);
            if (dataSet.Tables.Count < 1)
            {
                throw new InvalidOperationException("No result-sets were read, no data-table to return.");
            }

            // Return that table.
            var table = dataSet.Tables[0];
            return table;
        }

        /// <summary>
        /// Executes the command and pulls in all the results into a DataTable for a single result-set.
        /// </summary>
        public async Task<DataTable> ToDataTableAsync(bool closeConnection = true, DbTransaction transaction = null)
        {
            // Read the first table only.
            var dataSet = await ToDataSetAsync(1, closeConnection, transaction).ConfigureAwait(false);
            if (dataSet.Tables.Count < 1)
            {
                throw new InvalidOperationException("No result-sets were read, no data-table to return.");
            }

            // Return that table.
            var table = dataSet.Tables[0];
            return table;
        }

        /// <summary>
        /// Executes the command and pulls in all the result-sets into a DataSet.
        /// </summary>
        public DataSet ToDataSet(int? maxTables = null, bool closeConnection = true, DbTransaction transaction = null)
        {
            //var dataSet = ToDataSetAsync().Result;
            //return dataSet;

            using (var commandFactoryResult = _commandFactory().Result)
            {
                var command = commandFactoryResult.Command;
                ApplyCommandSettings(commandFactoryResult, transaction);

                using (var reader = command.ExecuteReader(closeConnection ? CommandBehavior.CloseConnection : CommandBehavior.Default))
                {
                    var dataSet = new DataSet();

                    // Iterate throuhg every result-set.
                    do
                    {
                        var table = ReadDataTable(reader);
                        if (table == null)
                        {
                            break;
                        }

                        dataSet.Tables.Add(table);

                        // Mave we got too many tables?
                        if (maxTables != null && maxTables >= dataSet.Tables.Count)
                        {
                            break;
                        }

                    }
                    while (reader.NextResult());

                    return dataSet;
                }
            }
        }

        /// <summary>
        /// Executes the command and pulls in all the result-sets into a DataSet.
        /// </summary>
        public async Task<DataSet> ToDataSetAsync(int? maxTables = null, bool closeConnection = true, DbTransaction transaction = null)
        {
            using (var commandFactoryResult = await _commandFactory().ConfigureAwait(false))
            {
                var command = commandFactoryResult.Command;
                ApplyCommandSettings(commandFactoryResult, transaction);

                using (var reader = await command.ExecuteReaderAsync(closeConnection ? CommandBehavior.CloseConnection : CommandBehavior.Default).ConfigureAwait(false))
                {
                    var dataSet = new DataSet();

                    // Iterate throuhg every result-set.
                    do
                    {
                        var table = await ReadDataTableAsync(reader).ConfigureAwait(false);
                        if (table == null)
                        {
                            break;
                        }

                        dataSet.Tables.Add(table);

                        // Mave we got too many tables?
                        if (maxTables != null && maxTables >= dataSet.Tables.Count)
                        {
                            break;
                        }

                    }
                    while (await reader.NextResultAsync().ConfigureAwait(false));

                    return dataSet;
                }
            }
        }

        /// <summary>
        /// Reads the entries of DataReader into a DataTable, used internally in some methods.
        /// </summary>
        private static async Task<DataTable> ReadDataTableAsync(DbDataReader reader)
        {
            var table = new DataTable();
            var hasDiscoveredColumnTypes = false;

            // Create the columns from the column names.
            if (!CreateTableColumns(reader, table))
            {
                return null;
            }

            // Read each row into the table.
            while (await reader.ReadAsync().ConfigureAwait(false))
            {

                // Have we set the column-types yet?
                if (!hasDiscoveredColumnTypes)
                {
                    SetTableColumnTypes(reader, table);
                    hasDiscoveredColumnTypes = true;
                }

                // Read the values into the row, and adding to the table..
                var row = table.NewRow();
                for (var fieldIndex = 0; fieldIndex < reader.FieldCount; fieldIndex++)
                {
                    row[fieldIndex] = reader[fieldIndex];
                }

                table.Rows.Add(row);

            }

            return table;
        }

        /// <summary>
        /// Reads the entries of DataReader into a DataTable, used internally in some methods.
        /// </summary>
        private static DataTable ReadDataTable(DbDataReader reader)
        {
            var table = new DataTable();
            var hasDiscoveredColumnTypes = false;

            // Create the columns from the column names.
            if (!CreateTableColumns(reader, table))
            {
                return null;
            }

            // Read each row into the table.
            while (reader.Read())
            {
                // Have we set the column-types yet?
                if (!hasDiscoveredColumnTypes)
                {
                    SetTableColumnTypes(reader, table);
                    hasDiscoveredColumnTypes = true;
                }

                // Read the values into the row, and adding to the table..
                var row = table.NewRow();
                for (var fieldIndex = 0; fieldIndex < reader.FieldCount; fieldIndex++)
                {
                    row[fieldIndex] = reader[fieldIndex];
                }

                table.Rows.Add(row);
            }

            return table;
        }

        /// <summary>
        /// Modifies existing columns inside a DataTable, with the DataTypes from a row in a reader.
        /// </summary>
        private static void SetTableColumnTypes(DbDataReader reader, DataTable table)
        {
            // Go trhough each field, setting the table column type.
            for (var fieldIndex = 0; fieldIndex < reader.FieldCount; fieldIndex++)
            {
                var type = reader.GetFieldType(fieldIndex);
                table.Columns[fieldIndex].DataType = type;
            }
        }

        /// <summary>
        /// Creates columns inside a DataTable, from a DataReader's SchemaTable,
        /// this can be read with or without rows existing.
        /// </summary>
        private static bool CreateTableColumns(DbDataReader reader, DataTable table)
        {
            // Add the columns from the schema-table.
            // These will be available even if there are no rows.
            var schemaTable = reader.GetSchemaTable();
            if (schemaTable == null)
            {
                return false;
            }

            var columnNameOrdinal = schemaTable.Columns["ColumnName"].Ordinal;
            foreach (var schemaRow in schemaTable.Rows.Cast<DataRow>())
            {
                var name = (string)schemaRow[columnNameOrdinal];
                table.Columns.Add(name);
            }

            return true;
        }

        private void ApplyCommandSettings(DbCommandFactoryResult commandFactoryResult, DbTransaction transaction = null)
        {
            var connection = (SqlConnection)commandFactoryResult.Connection;
            var command = (SqlCommand)commandFactoryResult.Command;

            // Setup the transaction and the timeout.
            command.Transaction = (SqlTransaction)(transaction ?? _transaction);
            command.CommandTimeout = _timeout;

            // Wite up the message receiver.
            if (_messageReceiver != null)
            {
                connection.InfoMessage += (s, e) =>
                {
                    foreach (var error in e.Errors.Cast<SqlError>())
                    {
                        if (error?.Message != null)
                        {
                            _messageReceiver(error?.Message);
                        }
                    }
                };
            }

        }
    }

}
