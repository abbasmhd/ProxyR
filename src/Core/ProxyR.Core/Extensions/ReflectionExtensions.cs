using System;
using System.Collections;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace ProxyR.Core.Extensions
{
    public static class ReflectionExtensions
    {
        /// <summary>
        /// Checks if the type is one of the following DB primitives:
        /// Boolean, Byte, SByte, Int16, UInt16, Int32, UInt32, Int64, UInt64, IntPtr, UIntPtr, Char, Double, Single, DateTime, DateTimeOffset, Byte[], DataTable.
        /// </summary>
        public static bool IsDbPrimitive(this Type type) => type != null && (type.IsPrimitive() || type == typeof(DataTable));

        public static bool IsDefined<T>(this Type type, bool inherits = true) => type.IsDefined(typeof(T), inherits);

        public static bool IsPrimitive(this Type type) => type.IsPrimitive
                                                       || type == typeof(decimal)
                                                       || type == typeof(string)
                                                       || type == typeof(Guid)
                                                       || type == typeof(DateTime)
                                                       || type == typeof(DateTimeOffset)
                                                       || type == typeof(byte[])
                                                       || type.IsDefinedNullable() && type.GetNullableUnderlyingType().IsPrimitive();

        public static bool IsEnumerable(this Type type) => type.GetInterface(nameof(IEnumerable)) != null;

        public static bool IsTask(this Type type) => typeof(Task).IsAssignableFrom(type);

        public static Type GetEnumerableType(this Type type) => IsEnumerable(type) ? type.GetGenericArguments()[0] : throw new ArgumentException("Given type is not enumerable.", nameof(type));

        public static bool HasInterface<TInterface>(this Type type) => type.GetInterfaces().Any(i => i == typeof(TInterface));

        public static bool IsDefinedNullable(this Type type) => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);

        public static Type GetNullableUnderlyingType(this Type type) => !type.IsDefinedNullable() ? type : Nullable.GetUnderlyingType(type);
    }

}
