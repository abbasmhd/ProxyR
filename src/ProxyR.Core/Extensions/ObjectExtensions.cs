using System;
using System.Data;
using System.Linq;
using System.Linq.Expressions;

namespace ProxyR.Core.Extensions
{
    /// <summary>
    /// This static class provides extension methods for the Object class.
    /// </summary>
    public static class ObjectExtensions
    {
        /// <summary>
        /// Compares two objects of the same type and returns a boolean value indicating whether their properties are equal.
        /// </summary>
        /// <typeparam name="TLeft">The type of the left object.</typeparam>
        /// <typeparam name="TRight">The type of the right object.</typeparam>
        /// <param name="left">The left object.</param>
        /// <param name="right">The right object.</param>
        /// <param name="propertyNames">The names of the properties to compare.</param>
        /// <returns>A boolean value indicating whether the properties of the two objects are equal.</returns>
        public static bool ArePropertiesEqual<TLeft, TRight>(this TLeft left, TRight right, params string[] propertyNames)
        {
            // Get the runtime type of the object falling back onto the compile type.
            var leftType = left?.GetType() ?? typeof(TLeft);
            var rightType = right?.GetType() ?? typeof(TRight);

            // Get the properties for the left and right types.
            var leftProperties = leftType.GetProperties();
            var rightProperties = rightType.GetProperties();

            // Joining left and right properties.
            var joinedProperties = leftProperties.Join(
                inner: rightProperties,
                outerKeySelector: s => s.Name,
                innerKeySelector: t => t.Name,
                resultSelector: (s, t) => new { Left = s, Right = t },
                comparer: StringComparer.OrdinalIgnoreCase)
            .ToArray();

            // Filter on property names, or by primitive properties.
            if (propertyNames != null)
            {
                joinedProperties = joinedProperties
                    .Where(p => propertyNames.Contains(p.Left.Name, StringComparer.OrdinalIgnoreCase))
                    .ToArray();
            }
            else
            {
                joinedProperties = joinedProperties
                    .Where(p => p.Left.PropertyType.IsPrimitive())
                    .ToArray();
            }

            // Perform the comparison.
            foreach (var joinedProperty in joinedProperties)
            {
                var leftValue = joinedProperty.Left.GetValue(left);
                var rightValue = joinedProperty.Right.GetValue(right);
                switch (leftValue)
                {
                    case null when rightValue == null:
                        continue;
                    case null:
                        return false;
                }

                if (!leftValue.Equals(rightValue))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Clones an object of type TSource to an object of type TTarget.
        /// </summary>
        /// <typeparam name="TSource">The type of the source object.</typeparam>
        /// <typeparam name="TTarget">The type of the target object.</typeparam>
        /// <param name="source">The source object.</param>
        /// <returns>A clone of the source object.</returns>
        public static TTarget Clone<TSource, TTarget>(this TSource source) where TTarget : new()
        {
            var target = new TTarget();
            Clone(source, target);
            return target;
        }

        /// <summary>
        /// Clones the properties of one object to another.
        /// </summary>
        /// <typeparam name="TSource">The type of the source object.</typeparam>
        /// <typeparam name="TTarget">The type of the target object.</typeparam>
        /// <param name="source">The source object.</param>
        /// <param name="target">The target object.</param>
        /// <returns>The source object.</returns>
        public static TSource Clone<TSource, TTarget>(this TSource source, TTarget target)
        {
            var sourceType = source?.GetType() ?? typeof(TSource);
            var targetType = target?.GetType() ?? typeof(TTarget);

            var sourceProperties = sourceType.GetProperties();
            var targetProperties = targetType.GetProperties();

            var joinedProperties = sourceProperties.Join(
                    inner: targetProperties,
                    outerKeySelector: s => s.Name,
                    innerKeySelector: t => t.Name,
                    resultSelector: (s, t) => new { Source = s, Target = t },
                    comparer: StringComparer.OrdinalIgnoreCase)
                .ToArray();

            foreach (var joinedProperty in joinedProperties)
            {
                var value = joinedProperty.Source.GetValue(source);
                joinedProperty.Target.SetValue(target, value);
            }

            return source;
        }

        /// <summary>
        /// Gets the name of the property from an expression.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <param name="selector">The selector.</param>
        /// <returns>The name of the property.</returns>
        public static string GetExpressionPropertyName<TEntity, TKey>(Expression<Func<TEntity, TKey>> selector) => ((MemberExpression)selector.Body).Member.Name;
    }
}
