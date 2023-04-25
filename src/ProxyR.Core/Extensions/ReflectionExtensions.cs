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
        /// Checks if the given type is a primitive type or a DataTable.
        /// Boolean, Byte, SByte, Int16, UInt16, Int32, UInt32, Int64, UInt64, IntPtr, UIntPtr, Char, Double, Single, DateTime, DateTimeOffset, Byte[], DataTable.
        /// </summary>
        public static bool IsDbPrimitive(this Type type) => type != null && (type.IsPrimitive() || type == typeof(DataTable));

        /// <summary>
        /// Checks if the specified type is defined.
        /// </summary>
        /// <typeparam name="T">The type to check.</typeparam>
        /// <param name="type">The type to check.</param>
        /// <param name="inherits">Indicates whether to search for the type in the inheritance chain.</param>
        /// <returns>True if the type is defined; otherwise, false.</returns>
        public static bool IsDefined<T>(this Type type, bool inherits = true) => type.IsDefined(typeof(T), inherits);

        /// <summary>
        /// Checks if the given type is a primitive type or a nullable primitive type.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>True if the type is a primitive type or a nullable primitive type, false otherwise.</returns>
        public static bool IsPrimitive(this Type type) => type.IsPrimitive
                                                       || type == typeof(decimal)
                                                       || type == typeof(string)
                                                       || type == typeof(Guid)
                                                       || type == typeof(DateTime)
                                                       || type == typeof(DateTimeOffset)
                                                       || type == typeof(byte[])
                                                       || type.IsDefinedNullable() && type.GetNullableUnderlyingType().IsPrimitive();

        /// <summary>
        /// Checks if the given type implements IEnumerable interface.
        /// </summary>
        public static bool IsEnumerable(this Type type) => type.GetInterface(nameof(IEnumerable)) != null;

        /// <summary>
        /// Checks if the given type is assignable from the Task type.
        /// </summary>
        public static bool IsTask(this Type type) => typeof(Task).IsAssignableFrom(type);

        /// <summary>
        /// Gets the type of the elements in the given enumerable type.
        /// </summary>
        /// <param name="type">The type of the enumerable.</param>
        /// <returns>The type of the elements in the given enumerable type.</returns>
        /// <exception cref="ArgumentException">Thrown when the given type is not an enumerable type.</exception>
        public static Type GetEnumerableType(this Type type) => IsEnumerable(type) ? type.GetGenericArguments()[0] : throw new ArgumentException("Given type is not enumerable.", nameof(type));

        /// <summary>
        /// Checks if the given type has the specified interface.
        /// </summary>
        public static bool HasInterface<TInterface>(this Type type) => type.GetInterfaces().Any(i => i == typeof(TInterface));

        /// <summary>
        /// Checks if the given type is a nullable type.
        /// </summary>
        public static bool IsDefinedNullable(this Type type) => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);

        /// <summary>
        /// Gets the underlying type of a nullable type.
        /// </summary>
        public static Type GetNullableUnderlyingType(this Type type) => !type.IsDefinedNullable() ? type : Nullable.GetUnderlyingType(type);
    }

}
