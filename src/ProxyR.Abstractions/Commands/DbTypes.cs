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
          , new DbType<double>        ("FLOAT"           , "number")
          , new DbType<decimal>       ("DECIMAL"         , "number", 18.10d)
          , new DbType<float>         ("REAL"            , "number")
          , new DbType<decimal>       ("MONEY"           , "number")
          , new DbType<int>           ("INT"             , "number")
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
    }
}
