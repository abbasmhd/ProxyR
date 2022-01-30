using System;
using System.Linq;

namespace ProxyR.Abstractions.Commands
{
    public static class DbTypes
    {

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

        public static DbType FromJsType(string jsType)
        {
            switch (jsType.ToLower())
            {
                case "string":
                    return _types.First(x => x.DbTypeName == "NVARCHAR");
                case "number":
                    return _types.First(x => x.DbTypeName == "DECIMAL");
                case "boolean":
                    return _types.First(x => x.DbTypeName == "BIT");
                default:
                    return _types.First(x => x.DbTypeName == "NVARCHAR");
            }
        }

        public static string ToJsType(string dbTypeName)
        {
            var result = _types.First(x => x.DbTypeName.Equals(dbTypeName, StringComparison.InvariantCultureIgnoreCase));
            return result.JsType;
        }

        public static string GetDbObjectTypeSymbol(DbObjectType x)
        {
            switch (x)
            {
                case DbObjectType.InlineTableValuedFunction:
                    return "IF";
                case DbObjectType.ServiceQueue:
                    return "SQ";
                case DbObjectType.ForeignKeyConstraint:
                    return "F";
                case DbObjectType.UserTable:
                    return "U";
                case DbObjectType.DefaultConstraint:
                    return "D";
                case DbObjectType.PrimaryKeyConstraint:
                    return "PK";
                case DbObjectType.SystemTable:
                    return "S";
                case DbObjectType.InternalTable:
                    return "IT";
                case DbObjectType.TableValuedFunction:
                    return "TF";
                default:
                    throw new NotSupportedException($"DbObjectType [{x}] is not supported");
            }
        }

        public static string FromDbType(string dbTypeString)
        {
            if (!Enum.TryParse(dbTypeString, out SQLType typeCode))
            {
                throw new Exception("sql type not found");
            }
            switch (typeCode)
            {
                case SQLType.varbinary:
                case SQLType.binary:
                case SQLType.filestream:
                case SQLType.image:
                case SQLType.rowversion:
                case SQLType.timestamp: //?
                    return "byte[]";
                case SQLType.tinyint:
                    return "byte";
                case SQLType.varchar:
                case SQLType.nvarchar:
                case SQLType.nchar:
                case SQLType.text:
                case SQLType.ntext:
                case SQLType.xml:
                    return "string";
                case SQLType.@char:
                    return "char";
                case SQLType.bigint:
                    return "long";
                case SQLType.bit:
                    return "bool";
                case SQLType.smalldatetime:
                case SQLType.datetime:
                case SQLType.date:
                case SQLType.datetime2:
                    return "DateTime";
                case SQLType.datetimeoffset:
                    return "DateTimeOffset";
                case SQLType.@decimal:
                case SQLType.money:
                case SQLType.numeric:
                case SQLType.smallmoney:
                    return "decimal";
                case SQLType.@float:
                    return "double";
                case SQLType.@int:
                    return "int";
                case SQLType.real:
                    return "Single";
                case SQLType.smallint:
                    return "short";
                case SQLType.uniqueidentifier:
                    return "Guid";
                case SQLType.sql_variant:
                    return "object";
                case SQLType.time:
                    return "TimeSpan";
                default:
                    throw new Exception("none equal type");
            }
        }

        public enum SQLType
        {
            varbinary,//(1)
            binary,//(1)
            image,
            varchar,
            @char,
            nvarchar,//(1)
            nchar,//(1)
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
