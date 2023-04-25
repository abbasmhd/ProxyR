using System;

namespace ProxyR.Abstractions.Commands
{

    /// <summary>
    /// Represents a type in a database.
    /// </summary>
    public class DbType
    {
        /// <summary>
        /// Property to get and set the database type name.
        /// </summary>
        public string DbTypeName { get; set; }

        /// <summary>
        /// Gets or sets the CLR type of the object.
        /// </summary>
        public Type ClrType { get; set; }

        /// <summary>
        /// Property to get and set the type of JavaScript used.
        /// </summary>        
        public string JsType { get; internal set; }

        /// <summary>
        /// Gets or sets the default length of the object.
        /// </summary>
        public int? DefaultLength { get; set; }

        /// <summary>
        /// Gets or sets the default precision for calculations.
        /// </summary>
        public double? DefaultPrecision { get; set; }
    }

    /// <summary>
    /// Represents a generic database type for the specified type parameter.
    /// </summary>
    public class DbType<T> : DbType
    {
        /// <summary>
        /// Constructor for the DbType class.
        /// </summary>
        /// <param name="dbTypeName">The name of the database type.</param>
        /// <param name="jsType">The JavaScript type.</param>
        /// <returns>
        /// A new instance of the DbType class.
        /// </returns>
        public DbType(string dbTypeName, string jsType)
        {
            DbTypeName = dbTypeName;
            JsType = jsType;
            ClrType = typeof(T);
        }

        /// <summary>
        /// Constructor for DbType class.
        /// </summary>
        /// <param name="dbTypeName">Name of the database type.</param>
        /// <param name="jsType">JavaScript type.</param>
        /// <param name="defaultLength">Default length of the type.</param>
        /// <returns>
        /// A new instance of the DbType class.
        /// </returns>
        public DbType(string dbTypeName, string jsType, int? defaultLength = null) : this(dbTypeName, jsType)

        {
            DefaultLength = defaultLength;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DbType"/> class with the specified database type name, JavaScript type and default precision.
        /// </summary>
        /// <param name="dbTypeName">The database type name.</param>
        /// <param name="jsType">The JavaScript type.</param>
        /// <param name="defaultPrecision">The default precision.</param>
        /// <returns>A new instance of the <see cref="DbType"/> class.</returns>
        public DbType(string dbTypeName, string jsType, double? defaultPrecision = null) : this(dbTypeName, jsType)
        {
            DefaultPrecision = defaultPrecision;
        }
    }
}
