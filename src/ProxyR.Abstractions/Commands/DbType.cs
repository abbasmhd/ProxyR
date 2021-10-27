using System;

namespace ProxyR.Abstractions.Commands
{

    public class DbType
    {
        public string DbTypeName { get; set; }
        public Type ClrType { get; set; }
        public string JsType { get; internal set; }
        public int? DefaultLength { get; set; }
        public double? DefaultPrecision { get; set; }
    }

    public class DbType<T> : DbType
    {
        public DbType(string dbTypeName, string jsType)
        {
            DbTypeName = dbTypeName;
            JsType = jsType;
            ClrType = typeof(T);
        }

        public DbType(string dbTypeName, string jsType, int? defaultLength = null) : this(dbTypeName, jsType)

        {
            DefaultLength = defaultLength;
        }

        public DbType(string dbTypeName, string jsType, double? defaultPrecision = null) : this(dbTypeName, jsType)
        {
            DefaultPrecision = defaultPrecision;
        }
    }
}
