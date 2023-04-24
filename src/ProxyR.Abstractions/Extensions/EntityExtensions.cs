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
        /// Creates columns inside a data-table, from the properties of an entity type.
        /// </summary>
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
                    if (maxLengthAttribute != null)
                    {
                        column.MaxLength = maxLengthAttribute.Length;
                    }

                    var requiredAttribute = property.GetCustomAttribute<RequiredAttribute>();
                    if (requiredAttribute != null)
                    {
                        column.AllowDBNull = false;
                    }

                    var keyAttribute = property.GetCustomAttribute<KeyAttribute>();
                    if (keyAttribute != null)
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

        public static DataRow AddRow(this DataTable table, DataRow row)
        {
            table.Rows.Add(row);
            return row;
        }

        public static DataRow AddRow<TEntity>(this DataTable table, TEntity entity)
        {
            var row = ConversionUtility.EntityToDataRow(entity, table);
            table.Rows.Add(row);
            return row;
        }

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

        public static DataTable AddColumn(this DataTable table, string name, Type type)
        {
            table.Columns.Add(name, type);
            return table;
        }

        public static DataTable ToDataTable<TEntity>(this IEnumerable<TEntity> items, IDictionary<string, Type> columns = null, Action<TEntity, DataRow> onRow = null)
        {
            var table = new DataTable();

            // Setup all the columns.
            if (columns != null)
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
        /// Converts a DataRow into an Entity.
        /// </summary>
        public static TEntity ToEntity<TEntity>(this DataRow row, DbEntityMap map = null) where TEntity : class, new()
        {
            if (map == null)
            {
                map = DbEntityMap.GetOrCreate(typeof(TEntity));
            }

            // Create the entity, of which we shall fill.
            var entity = new TEntity();

            foreach (var entry in map.Entries.Values)
            {
                // Get the column from the parent table.
                var column = row.Table.Columns[entry.ColumnName];
                if (column == null)
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
        /// Converts the current entry in the DataReader to an entity.
        /// </summary>
        public static TEntity ToEntity<TEntity>(this IDataReader dataReader, DbEntityMap map = null) where TEntity : class, new()
        {
            if (map == null)
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

        public static IEnumerable<TEntity> ToEntity<TEntity>(this DataTable table, DbEntityMap map = null) where TEntity : class, new()
        {
            if (map == null)
            {
                map = DbEntityMap.GetOrCreate(typeof(TEntity));
            }

            var results = table
                .Rows
                .Cast<DataRow>()
                .Select(dr => dr.ToEntity<TEntity>(map));

            return results;
        }

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

            if (property == null)
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

        public static IOrderedQueryable<TEntity> OrderBy<TEntity>(this IQueryable<TEntity> source, string propertyName, bool isDescending = false)
            => source.OrderBy(propertyName, isDescending, useThenBy: false);

        public static IOrderedQueryable<TEntity> ThenBy<TEntity>(this IOrderedQueryable<TEntity> source, string propertyName, bool isDescending = false)
            => source.OrderBy(propertyName, isDescending, useThenBy: true);

        public static void AddRange<TEntity>(this DbSet<TEntity> dbSet, IEnumerable<TEntity> source) where TEntity : class
        {
            foreach (var item in source)
            {
                dbSet.Add(item);
            }
        }

        public static TEntity GetOrCreate<TEntity, TProp>(this DbSet<TEntity> dbSet, Expression<Func<TEntity, TProp>> selector, TProp value) where TEntity : class, new()
        {
            // Simple null check first.
            if (value == null)
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
            if (result != null)
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
            else if (defaultTypeProperty == null)
            {
                throw new InvalidOperationException("Columns $Type and EntityType do not exist in data-table, cannot be imported into JDataSet.");
            }

            // Process every row.
            foreach (var row in dataTable.Rows.Cast<DataRow>())
            {
                // Get the type from the row.
                string typeName = null;
                if (typeColumn != null)
                {
                    typeName = row[typeColumn] as string;
                    if (typeName == null && defaultTypeProperty == null)
                    {
                        throw new InvalidOperationException("Row does not have a type.");
                    }
                }

                // Get the camel-case version.
                string camelTypeName;
                if (typeName != null)
                {
                    camelTypeName = typeName
                        .Camelize()
                        .Pluralize();
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
                    if (typeProperty == null)
                    {
                        typeProperty = new JProperty(camelTypeName);
                        jDataSet.Add(typeProperty);
                    }

                    // Make sure we have an array inside.
                    if (!(typeProperty.Value is JArray jTable))
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
        /// Puts all the rows inside every DataTable into a JDataSet.
        /// A JDataSet is a JSON object, with properties for each entity-type inside the data-set.
        /// Each row will be grouped and named after the $Type field of each row.
        /// </summary>
        public static JObject ToJDataSet(this DataSet dataSet)
        {
            var jDataSet = new JObject();
            foreach (DataTable table in dataSet.Tables)
            {
                AddToJDataSet(table, jDataSet);
            }
            return jDataSet;
        }

        public static async Task<JObject> ToJDataSetAsync(this DbResult dbResult)
        {
            var dataSet = await dbResult.ToDataSetAsync().ConfigureAwait(false);
            var jDataSet = dataSet.ToJDataSet();
            return jDataSet;
        }
    }
}
