using System;
using System.Linq;

namespace ProxyR.Abstractions.Commands
{
    public static class DbTypes
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="DbType"/> class.
        /// </summary>
        /// <param name="dbTypeName">Name of the database type.</param>
        /// <param name="typeName">Name of the type.</param>
        /// <param name="maxLength">The maximum length.</param>
        private static readonly DbType[] _types = {
            new DbType<string>        ("NVARCHAR"        , "string", Int32.MaxValue)
          , new DbType<object>        ("NVARCHAR"        , "string", Int32.MaxValue)
          , new DbType<string>        ("VARCHAR"         , "string", Int32.MaxValue)
          , new DbType<byte[]>        ("VARBINARY"       , "string", Int32.MaxValue)
          , new DbType<string>        ("NCHAR"           , "string")
          , new DbType<string>        ("CHAR"            , "string")
          , new DbType<string>        ("NTEXt"           , "string")
          , new DbType<string>        ("TEXT"            , "string")
          , new DbType<string>        ("TIMESTAMP"       , "string")
          , new DbType<double>        ("FLOAT"           , "number")
          , new DbType<decimal>       ("DECIMAL"         , "number", 18.10d)
          , new DbType<float>         ("REAL"            , "number")
          , new DbType<decimal>       ("MONEY"           , "number")
          , new DbType<int>           ("INT"             , "number")
          , new DbType<int>           ("INTEGER"         , "number")
          , new DbType<bool>          ("BIT"             , "boolean")
          , new DbType<byte>          ("TINYINT"         , "number")
          , new DbType<short>         ("SMALLINT"        , "number")
          , new DbType<long>          ("BIGINT"          , "number")
          , new DbType<Guid>          ("UNIQUEIDENTIFIER", "string")
          , new DbType<DateTime>      ("DATETIME"        , "string")
          , new DbType<DateTime>      ("DATETIME2"       , "string")
          , new DbType<DateTime>      ("DATE"            , "string")
          , new DbType<TimeSpan>      ("TIME"            , "string")
          , new DbType<DateTimeOffset>("DATETIMEOFFSET"  , "string")
        };

        /// <summary>
        /// Gets the database type syntax for the given CLR type.
        /// </summary>
        /// <param name="clrType">The CLR type.</param>
        /// <param name="length">The length.</param>
        /// <param name="precision">The precision.</param>
        /// <returns>The database type syntax.</returns>
        public static string GetDbTypeSyntax(Type clrType, int? length = null, double? precision = null)
        {
            var dbType = _types.First(t => t.ClrType == clrType);
            var syntax = dbType.DbTypeName;

            if (dbType.DbTypeName.IndexOf("char", StringComparison.InvariantCultureIgnoreCase) > -1
                || dbType.DbTypeName.IndexOf("binary", StringComparison.InvariantCultureIgnoreCase) > -1)
            {
                var resolvedLength = length ?? dbType.DefaultLength;

                syntax += resolvedLength == null || resolvedLength == Int32.MaxValue || resolvedLength < 0
                    ? "(MAX)"
                    : $"({resolvedLength})";
            }

            var resolvedPrecision = precision ?? dbType.DefaultPrecision;
            if (resolvedPrecision != null)
            {
                syntax += $"({resolvedPrecision.ToString().Replace(".", ",")})";
            }

            return syntax;
        }

        /// <summary>
        /// Maps a JavaScript type to a DbType.
        /// </summary>
        /// <param name="jsType">The JavaScript type.</param>
        /// <returns>The DbType.</returns>
        public static DbType FromJsType(string jsType)
        {
            return jsType.ToLower() switch
            {
                "string"  => _types.First(x => x.DbTypeName == "NVARCHAR"),
                "number"  => _types.First(x => x.DbTypeName == "DECIMAL"),
                "boolean" => _types.First(x => x.DbTypeName == "BIT"),
                _         => _types.First(x => x.DbTypeName == "NVARCHAR"),
            };
        }

        /// <summary>
        /// Gets the JavaScript type name for the given database type name.
        /// </summary>
        /// <param name="dbTypeName">The database type name.</param>
        /// <returns>The JavaScript type name.</returns>
        public static string ToJsType(string dbTypeName)
        {
            var result = _types.First(x => x.DbTypeName.Equals(dbTypeName, StringComparison.InvariantCultureIgnoreCase));
            return result.JsType;
        }

        /// <summary>
        /// Gets the symbol for the specified DbObjectType.
        /// </summary>
        /// <param name="x">The DbObjectType.</param>
        /// <returns>The symbol for the specified DbObjectType.</returns>
        public static string GetDbObjectTypeSymbol(DbObjectType x)
        {
            return x switch
            {
                DbObjectType.InlineTableValuedFunction => "IF",
                DbObjectType.ServiceQueue              => "SQ",
                DbObjectType.ForeignKeyConstraint      => "F",
                DbObjectType.UserTable                 => "U",
                DbObjectType.DefaultConstraint         => "D",
                DbObjectType.PrimaryKeyConstraint      => "PK",
                DbObjectType.SystemTable               => "S",
                DbObjectType.InternalTable             => "IT",
                DbObjectType.TableValuedFunction       => "TF",
                DbObjectType.View                      => "V",
                _ => throw new NotSupportedException($"DbObjectType [{x}] is not supported"),
            };
        }

        /// <summary>
        /// Converts a string to a DbObjectType enum value.
        /// </summary>
        /// <param name="value">The string to convert.</param>
        /// <returns>The DbObjectType enum value.</returns>
        public static DbObjectType ToDbObjectType(this string value)
        {
            value = value?.Trim().ToUpper();
            return value switch
            {
                "IF" => DbObjectType.InlineTableValuedFunction,
                "SQ" => DbObjectType.ServiceQueue,
                "F"  => DbObjectType.ForeignKeyConstraint,
                "U"  => DbObjectType.UserTable,
                "D"  => DbObjectType.DefaultConstraint,
                "PK" => DbObjectType.PrimaryKeyConstraint,
                "S"  => DbObjectType.SystemTable,
                "IT" => DbObjectType.InternalTable,
                "TF" => DbObjectType.TableValuedFunction,
                "V"  => DbObjectType.View,
                _    => DbObjectType.NotSuported,
            };
        }

        /// <summary>
        /// Converts a SQL type string to a .NET type string.
        /// </summary>
        /// <param name="dbTypeString">The SQL type string.</param>
        /// <returns>
        /// The .NET type string.
        /// </returns>
        public static string FromDbType(string dbTypeString)
        {
            if (!Enum.TryParse(dbTypeString, out SQLType typeCode))
            {
                throw new Exception("sql type not found");
            }
            return typeCode switch
            {
                SQLType.varbinary or SQLType.binary or SQLType.filestream or SQLType.image or SQLType.rowversion or SQLType.timestamp => "byte[]",
                SQLType.varchar or SQLType.nvarchar or SQLType.nchar or SQLType.text or SQLType.ntext or SQLType.xml => "string",
                SQLType.smalldatetime or SQLType.datetime or SQLType.date or SQLType.datetime2 => "DateTime",
                SQLType.@decimal or SQLType.money or SQLType.numeric or SQLType.smallmoney => "decimal",
                SQLType.tinyint          => "byte",
                SQLType.@char            => "char",
                SQLType.bigint           => "long",
                SQLType.bit              => "bool",
                SQLType.datetimeoffset   => "DateTimeOffset",
                SQLType.@float           => "double",
                SQLType.@int             => "int",
                SQLType.real             => "Single",
                SQLType.smallint         => "short",
                SQLType.uniqueidentifier => "Guid",
                SQLType.sql_variant      => "object",
                SQLType.time             => "TimeSpan",
                _ => throw new Exception("none equal type"),
            };
        }

        /// <summary>
        /// Enum containing the different types of SQL data types.
        /// </summary>
        public enum SQLType
        {
            varbinary,
            binary,
            image,
            varchar,
            @char,
            nvarchar,
            nchar,
            text,
            ntext,
            uniqueidentifier,
            rowversion,
            bit,
            tinyint,
            smallint,
            @int,
            bigint,
            smallmoney,
            money,
            numeric,
            @decimal,
            real,
            @float,
            smalldatetime,
            datetime,
            sql_variant,
            table,
            cursor,
            timestamp,
            xml,
            date,
            datetime2,
            datetimeoffset,
            filestream,
            time,
        }
    }
}
