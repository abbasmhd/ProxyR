using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using ProxyR.Abstractions.Execution;
using ProxyR.Abstractions.Utilities;
using ProxyR.Core.Extensions;

namespace ProxyR.Abstractions.Extensions
{
    public static class EntityExtensions
    {
        /// <summary>
        /// Adds columns to a DataTable based on the properties of a specified entity type.
        /// </summary>
        /// <typeparam name="TEntity">The type of entity to use for creating columns.</typeparam>
        /// <param name="table">The DataTable to add columns to.</param>
        /// <returns>The modified DataTable.</returns>
        public static DataTable AddColumns<TEntity>(this DataTable table)
        {
            // Get a list of all properties for that type.
            var properties = typeof(TEntity)
                .GetProperties()
                .Where(p => p.PropertyType.IsPrimitive());

            // List to build up the primary key.
            var primaryKey = new List<DataColumn>();

            // Setup all the columns.
            foreach (var property in properties)
            {
                if (!table.Columns.Contains(property.Name))
                {
                    var column = table.Columns.Add(property.Name, property.PropertyType.GetNullableUnderlyingType());

                    var maxLengthAttribute = property.GetCustomAttribute<MaxLengthAttribute>();
                    if (maxLengthAttribute is not null)
                    {
                        column.MaxLength = maxLengthAttribute.Length;
                    }

                    var requiredAttribute = property.GetCustomAttribute<RequiredAttribute>();
                    if (requiredAttribute is not null)
                    {
                        column.AllowDBNull = false;
                    }

                    var keyAttribute = property.GetCustomAttribute<KeyAttribute>();
                    if (keyAttribute is not null)
                    {
                        column.AllowDBNull = false;
                        primaryKey.Add(column);
                    }
                }
            }

            // Setup the primary-key.
            if (primaryKey.Any())
            {
                table.PrimaryKey = primaryKey.ToArray();
            }

            return table;
        }

        /// <summary>
        /// Adds a DataRow to a DataTable and returns the added row.
        /// </summary>
        /// <param name="table">The DataTable to add the row to.</param>
        /// <param name="row">The DataRow to add to the DataTable.</param>
        /// <returns>The added DataRow.</returns>
        public static DataRow AddRow(this DataTable table, DataRow row)
        {
            table.Rows.Add(row);
            return row;
        }

        /// <summary>
        /// Converts an entity to a DataRow, adds the DataRow to a DataTable, and returns the added row.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity to convert.</typeparam>
        /// <param name="table">The DataTable to add the row to.</param>
        /// <param name="entity">The entity to convert and add to the DataTable.</param>
        /// <returns>The added DataRow.</returns>
        public static DataRow AddRow<TEntity>(this DataTable table, TEntity entity)
        {
            var row = ConversionUtility.EntityToDataRow(entity, table);
            table.Rows.Add(row);
            return row;
        }

        /// <summary>
        /// Adds a collection of DataRows to a DataTable and invokes an action for each added row.
        /// </summary>
        /// <param name="table">The DataTable to add the rows to.</param>
        /// <param name="rows">The collection of DataRows to add to the DataTable.</param>
        /// <param name="onRow">The action to invoke for each added row.</param>
        /// <returns>The modified DataTable.</returns>
        public static DataTable AddRows(this DataTable table, IEnumerable<DataRow> rows, Action<DataRow> onRow = null)
        {
            // Fill up the data-table from the source.
            foreach (var row in rows)
            {
                // Append a new row to the table.
                table.AddRow(row);

                // Call the given row-filler.
                onRow?.Invoke(row);
            }

            return table;
        }

        /// <summary>
        /// Converts a collection of entities to DataRows, adds the DataRows to a DataTable, and invokes an action for each added row.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entities to convert.</typeparam>
        /// <param name="table">The DataTable to add the rows to.</param>
        /// <param name="items">The collection of entities to convert and add to the DataTable.</param>
        /// <param name="onRow">The action to invoke for each added row.</param>
        /// <returns>The modified DataTable.</returns>
        public static DataTable AddRows<TEntity>(this DataTable table, IEnumerable<TEntity> items, Action<TEntity, DataRow> onRow = null)
        {
            // Create the columns from the entity.
            table.AddColumns<TEntity>();

            // Fill up the data-table from the source.
            foreach (var item in items)
            {
                // Append a new row to the table.
                var row = table.AddRow(item);

                // Call the given row-filler.
                onRow?.Invoke(item, row);
            }

            return table;
        }

        /// <summary>
        /// Adds a column to a DataTable with the specified name and type.
        /// </summary>
        /// <param name="table">The DataTable to add the column to.</param>
        /// <param name="name">The name of the new column.</param>
        /// <param name="type">The data type of the new column.</param>
        /// <returns>The modified DataTable.</returns>
        public static DataTable AddColumn(this DataTable table, string name, Type type)
        {
            table.Columns.Add(name, type);
            return table;
        }

        /// <summary>
        /// Converts an IEnumerable of a specified entity type to a DataTable.
        /// </summary>
        /// <typeparam name="TEntity">The type of entity to convert to a DataTable.</typeparam>
        /// <param name="items">The IEnumerable of entities to convert.</param>
        /// <param name="columns">An optional dictionary specifying the columns to create in the DataTable.</param>
        /// <param name="onRow">An optional action to perform on each entity when creating a new row in the DataTable.</param>
        /// <returns>The resulting DataTable.</returns>
        public static DataTable ToDataTable<TEntity>(this IEnumerable<TEntity> items, IDictionary<string, Type> columns = null, Action<TEntity, DataRow> onRow = null)
        {
            var table = new DataTable();

            // Setup all the columns.
            if (columns is not null)
            {
                foreach (var column in columns)
                {
                    table.Columns.Add(column.Key, column.Value);
                }
            }

            // Add the row.
            table.AddRows(items, onRow);

            return table;
        }

        /// <summary>
        /// Converts a DataRow into an instance of the specified entity type.
        /// </summary>
        /// <typeparam name="TEntity">The type of entity to create.</typeparam>
        /// <param name="row">The DataRow to convert to an entity.</param>
        /// <param name="map">An optional mapping of database columns to entity properties.</param>
        /// <returns>The resulting entity instance.</returns>
        public static TEntity ToEntity<TEntity>(this DataRow row, DbEntityMap map = null) where TEntity : class, new()
        {
            if (map is null)
            {
                map = DbEntityMap.GetOrCreate(typeof(TEntity));
            }

            // Create the entity, of which we shall fill.
            var entity = new TEntity();

            foreach (var entry in map.Entries.Values)
            {
                // Get the column from the parent table.
                var column = row.Table.Columns[entry.ColumnName];
                if (column is null)
                {
                    continue;
                }

                // Get the value from the field.
                var value = row[column.Ordinal];

                // Set the field on the entity.
                entry.SetValue(entity, value);

            }

            return entity;
        }

        /// <summary>
        /// Converts the current entry in the DataReader to an instance of the specified entity type.
        /// </summary>
        /// <typeparam name="TEntity">The type of entity to create.</typeparam>
        /// <param name="dataReader">The DataReader to convert to an entity.</param>
        /// <param name="map">An optional mapping of database columns to entity properties.</param>
        /// <returns>The resulting entity instance.</returns>
        public static TEntity ToEntity<TEntity>(this IDataReader dataReader, DbEntityMap map = null) where TEntity : class, new()
        {
            if (map is null)
            {
                map = DbEntityMap.GetOrCreate(typeof(TEntity));
            }

            // Create the entity, of which we shall fill.
            var entity = new TEntity();

            foreach (var entry in map.Entries.Values)
            {
                // Ensure the column exists, so we don't trigger an exception.
                if (!dataReader.HasColumn(entry.ColumnName))
                {
                    continue;
                }

                // Get the value.
                var value = dataReader[entry.ColumnName];

                // Set the field on the entity.
                entry.SetValue(entity, value);
            }

            return entity;
        }

        /// <summary>
        /// Extension method for the DataTable class that converts the data in the table into a collection of entities of the specified type.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entities to create.</typeparam>
        /// <param name="table">The DataTable to convert into entities.</param>
        /// <param name="map">Optional DbEntityMap that maps the table columns to the entity properties.</param>
        /// <returns>An IEnumerable of entities of the specified type.</returns>
        public static IEnumerable<TEntity> ToEntity<TEntity>(this DataTable table, DbEntityMap map = null) where TEntity : class, new()
        {
            if (map is null)
            {
                map = DbEntityMap.GetOrCreate(typeof(TEntity));
            }

            var results = table
                .Rows
                .Cast<DataRow>()
                .Select(dr => dr.ToEntity<TEntity>(map));

            return results;
        }

        /// <summary>
        /// Extension method for the IQueryable interface that sorts the elements of a sequence of entities based on a specified property name.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entities to sort.</typeparam>
        /// <param name="source">The IQueryable to sort.</param>
        /// <param name="propertyName">The name of the property to sort by.</param>
        /// <param name="isDescending">Optional flag indicating whether to sort in descending order.</param>
        /// <param name="useThenBy">Optional flag indicating whether to use the ThenBy method for subsequent sorting.</param>
        /// <returns>An IOrderedQueryable of entities of the specified type.</returns>
        private static IOrderedQueryable<TEntity> OrderBy<TEntity>(this IQueryable<TEntity> source, string propertyName, bool isDescending = false, bool useThenBy = false)
        {
            if (String.IsNullOrWhiteSpace(propertyName))
            {
                throw new ArgumentNullException(nameof(propertyName));
            }

            // Get the property on the entity.
            var property = typeof(TEntity)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(p => p.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase));

            if (property is null)
            {
                throw new InvalidOperationException($"Could not sort by {propertyName}, as property does not exist.");
            }

            // Create the property expression, used to create the order-by selector.
            var paramExpression = Expression.Parameter(typeof(TEntity), "e");
            var propExpression = Expression.Property(paramExpression, property.Name);

            // What method should we call?
            var actionName = useThenBy ? "ThenBy" : "OrderBy";
            var direction = isDescending ? "Descending" : String.Empty;
            var orderMethodName = String.Concat(actionName, direction);

            // Create the key-selector. and apply the sort.
            var funcType = typeof(Func<,>).MakeGenericType(typeof(TEntity), property.PropertyType);
            var expressionType = typeof(Expression<>).MakeGenericType(funcType);
            var lambda = typeof(Expression).GetMethods(BindingFlags.Static | BindingFlags.Public)
                .Where(m => m.Name == "Lambda")
                .Where(m => m.IsGenericMethodDefinition);

            var genericLambda = lambda.First().MakeGenericMethod(funcType);
            var selector = genericLambda.Invoke(null, new object[] { propExpression, new[] { paramExpression } });

            // Add the order-by method to the query.
            var orderBy = typeof(Queryable)
                .GetMethods(BindingFlags.Static | BindingFlags.Public)
                .First(m => m.Name == orderMethodName)
                .MakeGenericMethod(typeof(TEntity), property.PropertyType);

            var query = (IOrderedQueryable<TEntity>)orderBy.Invoke(null, new object[] { source, selector });

            return query;
        }

        /// <summary>
        /// Extension method for the IQueryable interface that sorts the elements of a sequence of entities based on a specified property name.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entities to sort.</typeparam>
        /// <param name="source">The IQueryable to sort.</param>
        /// <param name="propertyName">The name of the property to sort by.</param>
        /// <param name="isDescending">Optional flag indicating whether to sort in descending order.</param>
        /// <returns>An IOrderedQueryable of entities of the specified type.</returns>
        public static IOrderedQueryable<TEntity> OrderBy<TEntity>(this IQueryable<TEntity> source, string propertyName, bool isDescending = false)
            => source.OrderBy(propertyName, isDescending, useThenBy: false);

        /// <summary>
        /// Extension method for the IOrderedQueryable interface that performs a subsequent ordering of the elements in a sequence based on a specified property name.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entities to sort.</typeparam>
        /// <param name="source">The IOrderedQueryable to sort.</param>
        /// <param name="propertyName">The name of the property to sort by.</param>
        /// <param name="isDescending">Optional flag indicating whether to sort in descending order.</param>
        /// <returns>An IOrderedQueryable of entities of the specified type.</returns>
        public static IOrderedQueryable<TEntity> ThenBy<TEntity>(this IOrderedQueryable<TEntity> source, string propertyName, bool isDescending = false)
            => source.OrderBy(propertyName, isDescending, useThenBy: true);

        /// <summary>
        /// Extension method for the DbSet class that adds a range of entities to the set.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entities to add.</typeparam>
        /// <param name="dbSet">The DbSet to add the entities to.</param>
        /// <param name="source">An IEnumerable of the entities to add.</param>
        public static void AddRange<TEntity>(this DbSet<TEntity> dbSet, IEnumerable<TEntity> source) where TEntity : class
        {
            foreach (var item in source)
            {
                dbSet.Add(item);
            }
        }

        /// <summary>
        /// Extension method for the DbSet class that retrieves an entity from the set based on a specified property value, or creates a new entity if one does not exist.
        /// </summary>
        /// <typeparam name="TEntity">The type of entity to retrieve or create.</typeparam>
        /// <typeparam name="TProp">The type of the property to search for.</typeparam>
        /// <param name="dbSet">The DbSet to search for the entity.</param>
        /// <param name="selector">An expression that specifies the property to search for.</param>
        /// <param name="value">The value to search for.</param>
        /// <returns>The entity with the specified property value, or a new entity if one does not exist.</returns>
        public static TEntity GetOrCreate<TEntity, TProp>(this DbSet<TEntity> dbSet, Expression<Func<TEntity, TProp>> selector, TProp value) where TEntity : class, new()
        {
            // Simple null check first.
            if (value is null)
            {
                return null;
            }

            // Get the property name.
            var propertyExpression = (MemberExpression)selector.Body;
            var propertyName = ObjectUtility.GetExpressionPropertyName(selector);

            // Build an expression to find the object.
            var predicate = Expression.Lambda<Func<TEntity, bool>>(Expression.Equal(propertyExpression, Expression.Constant(value, value.GetType())), selector.Parameters.First());

            // Compile so that we may search uncommitted data first.
            var localPredicate = predicate.Compile();

            // Search the local store first.
            // Then search the database.
            var result = dbSet.Local.FirstOrDefault(localPredicate) ?? dbSet.FirstOrDefault(predicate);

            // Is there an object to return?
            if (result is not null)
            {
                return result;
            }

            // Create the object, and set the property's value.
            result = new TEntity();
            var property = typeof(TEntity).GetProperty(propertyName);
            property?.SetValue(result, value);

            // Make sure its added, so that it can be found for subsequent calls.
            dbSet.Add(result);

            return result;
        }

        /// <summary>
        /// Puts all the rows inside a DataTable into a JDataSet.
        /// Each row will be grouped into properties of the JDataSet, named after the $Type field of each row.
        /// </summary>
        /// <param name="dataTable">The DataTable to add to the JDataSet.</param>
        /// <param name="jDataSet">The JDataSet to add the rows to.</param>
        /// <param name="defaultTypeProperty">The name of the default type property to use if the DataTable does not contain a $Type or EntityType column.</param>
        /// <returns>The modified DataTable.</returns>
        public static DataTable AddToJDataSet(this DataTable dataTable, JObject jDataSet, string defaultTypeProperty = null)
        {
            // Get the type column.
            DataColumn typeColumn = null;
            if (dataTable.Columns.Contains("$Type"))
            {
                typeColumn = dataTable.Columns["$Type"];
            }
            else if (dataTable.Columns.Contains("EntityType"))
            {
                typeColumn = dataTable.Columns["EntityType"];
            }
            else if (defaultTypeProperty is null)
            {
                throw new InvalidOperationException("Columns $Type and EntityType do not exist in data-table, cannot be imported into JDataSet.");
            }

            // Process every row.
            foreach (var row in dataTable.Rows.Cast<DataRow>())
            {
                // Get the type from the row.
                string typeName = null;
                if (typeColumn is not null)
                {
                    typeName = row[typeColumn] as string;
                    if (typeName is null && defaultTypeProperty is null)
                    {
                        throw new InvalidOperationException("Row does not have a type.");
                    }
                }

                // Get the camel-case version.
                string camelTypeName;
                if (typeName is not null)
                {
                    camelTypeName = typeName.Camelize().Pluralize();
                }
                else
                {
                    camelTypeName = defaultTypeProperty;
                }

                JObject jRow;
                if (typeName?.ToLower() == "$root")
                {
                    // We will be applying properties to the
                    // root object... the data-set object.
                    jRow = jDataSet;
                }
                else
                {
                    // Does the dataset have that property.
                    var typeProperty = jDataSet.Property(camelTypeName);
                    if (typeProperty is null)
                    {
                        typeProperty = new JProperty(camelTypeName);
                        jDataSet.Add(typeProperty);
                    }

                    // Make sure we have an array inside.
                    if (typeProperty.Value is not JArray jTable)
                    {
                        jTable = new JArray();
                        typeProperty.Value = jTable;
                    }

                    // Create and add row to the table array.
                    jRow = new JObject();
                    jTable.Add(jRow);
                }

                // Add the row into the array.
                foreach (DataColumn column in dataTable.Columns)
                {
                    // Let's not include the type column.
                    if (column == typeColumn)
                    {
                        continue;
                    }

                    // Get the value of the row.
                    var value = row[column];

                    // Ensure NULLs are given properly.
                    if (value is DBNull)
                    {
                        value = null;
                    }

                    // camel-case the property name.
                    var camelPropertyName = column.ColumnName.Camelize();
                    jRow.Add(new JProperty(camelPropertyName, value));
                }
            }

            return dataTable;
        }

        /// <summary>
        /// Converts a given DataSet to a JDataSet, which is a JSON object containing properties for each entity-type present in the DataSet.
        /// Each DataTable inside the DataSet is converted to a JObject and added to the JDataSet.
        /// The JObject is named after the $Type field of each row in the DataTable and contains all the rows of the DataTable.
        /// </summary>
        /// <param name="dataSet">The DataSet to convert to a JDataSet.</param>
        /// <returns>A JObject representing the converted JDataSet.</returns>
        public static JObject ToJDataSet(this DataSet dataSet)
        {
            var jDataSet = new JObject();
            foreach (DataTable table in dataSet.Tables)
            {
                AddToJDataSet(table, jDataSet);
            }
            return jDataSet;
        }

        /// <summary>
        /// Converts a DbResult object to a JDataSet asynchronously.
        /// The method first converts the DbResult to a DataSet using ToDataSetAsync() method of the DbResult object.
        /// Then it uses the ToJDataSet() method of the DataSet to convert it to a JDataSet.
        /// </summary>
        /// <param name="dbResult">The DbResult object to convert to a JDataSet.</param>
        /// <returns>A JObject representing the converted JDataSet.</returns>
        public static async Task<JObject> ToJDataSetAsync(this DbResult dbResult)
        {
            var dataSet = await dbResult.ToDataSetAsync().ConfigureAwait(false);
            var jDataSet = dataSet.ToJDataSet();
            return jDataSet;
        }
    }
}
