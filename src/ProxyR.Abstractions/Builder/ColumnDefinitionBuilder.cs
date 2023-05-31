using System;

namespace ProxyR.Abstractions.Builder
{

    /// <summary>
    /// Builds a column definition string for use in a CREATE TABLE statement.
    /// </summary>
    /// <returns>A string representing the column definition.</returns>
    public class ColumnDefinitionBuilder
    {
        private readonly string _columnName;
        private readonly string _type;
        private bool? _isNullable;
        private string _defaultExpression;
        private int _columnNamePadding;
        private bool _doPadding;
        private string _collation;

        /// <summary>
        /// Constructor for ColumnDefinitionBuilder class.
        /// </summary>
        /// <param name="columnName">Name of the column.</param>
        /// <param name="type">Type of the column.</param>
        /// <returns>
        /// An instance of ColumnDefinitionBuilder.
        /// </returns>
        public ColumnDefinitionBuilder(string columnName, string type)
        {
            _columnName = columnName;
            _type = type.ToUpper();
        }

        /// <summary>
        /// Sets the nullability of the column.
        /// </summary>
        /// <param name="isNullable">A boolean value indicating whether the column is nullable.</param>
        /// <returns>The current ColumnDefinitionBuilder instance.</returns>
        public ColumnDefinitionBuilder IsNullable(bool? isNullable)
        {
            _isNullable = isNullable;
            return this;
        }

        /// <summary>
        /// Sets the default expression for the column.
        /// </summary>
        /// <param name="defaultExpression">The default expression to set.</param>
        /// <returns>The ColumnDefinitionBuilder instance.</returns>
        public ColumnDefinitionBuilder DefaultExpression(string defaultExpression)
        {
            _defaultExpression = defaultExpression;
            return this;
        }

        /// <summary>
        /// Sets the padding for the column name.
        /// </summary>
        /// <param name="columnNamePadding">The padding for the column name.</param>
        /// <returns>The ColumnDefinitionBuilder instance.</returns>
        public ColumnDefinitionBuilder ColumnNamePadding(int columnNamePadding)
        {
            _columnNamePadding = columnNamePadding;
            return this;
        }

        /// <summary>
        /// Sets the doPadding flag for the ColumnDefinitionBuilder.
        /// </summary>
        /// <param name="doPadding">The doPadding flag.</param>
        /// <returns>The ColumnDefinitionBuilder.</returns>
        public ColumnDefinitionBuilder DoPadding(bool doPadding)
        {
            _doPadding = doPadding;
            return this;
        }

        /// <summary>
        /// Sets the collation for the column.
        /// </summary>
        /// <param name="collation">The collation to set.</param>
        /// <returns>The ColumnDefinitionBuilder instance.</returns>
        public ColumnDefinitionBuilder Collation(string collation)
        {
            _collation = collation;
            return this;
        }

        /// <summary>
        /// Builds a string representation of a column definition.
        /// </summary>
        /// <returns>A string representation of a column definition.</returns>
        public string Build()
        {
            var columnPart = $"[{_columnName}]".PadRight(_doPadding ? _columnNamePadding : 0);

            var collationPart = String.Empty;

            if (_type.IndexOf("CHAR", StringComparison.InvariantCultureIgnoreCase) > -1)
            {
                collationPart = $"COLLATE {(String.IsNullOrWhiteSpace(_collation) ? "DATABASE_DEFAULT" : _collation)}";
            }

            var nullablePart = String.Empty;

            if (_isNullable != null)
            {
                nullablePart = _isNullable == true ? "NULL" : "NOT NULL";
            }

            var defaultPart = String.Empty;

            if (_defaultExpression != null)
            {
                defaultPart = String.IsNullOrWhiteSpace(_defaultExpression)
                    ? $"= ''"
                    : $"= {_defaultExpression}";
            }

            var result = String.Join(" ",
                    columnPart,
                    _type.PadRight(_doPadding ? 16 : 0),
                    collationPart.PadRight(_doPadding ? 20 : 0),
                    nullablePart.PadRight(_doPadding ? 4 : 0),
                    defaultPart)
                .Trim();

            return result;
        }
    }
}
