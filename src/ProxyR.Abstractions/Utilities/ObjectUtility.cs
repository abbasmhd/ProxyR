using ProxyR.Core.Extensions;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace ProxyR.Abstractions.Utilities
{
    /// <summary>
    /// This static class provides utility methods for working with objects.
    /// </summary>
    public static class ObjectUtility
    {
        /// <summary>
        /// Compares two objects of the same type to determine if their properties are equal.
        /// </summary>
        /// <typeparam name="TLeft">The type of the left object.</typeparam>
        /// <typeparam name="TRight">The type of the right object.</typeparam>
        /// <param name="left">The left object.</param>
        /// <param name="right">The right object.</param>
        /// <param name="propertyNames">The names of the properties to compare.</param>
        /// <returns>True if the properties are equal, otherwise false.</returns>
        public static bool ArePropertiesEqual<TLeft, TRight>(TLeft left, TRight right, params string[] propertyNames)
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
            if (propertyNames is not null)
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
                if (leftValue is null && rightValue is null)
                {
                    continue;
                }

                if (leftValue is null && rightValue is not null)
                {
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
        /// Creates a new instance of type TTarget and copies the properties of TSource to it.
        /// </summary>
        /// <typeparam name="TSource">The type of the source object.</typeparam>
        /// <typeparam name="TTarget">The type of the target object.</typeparam>
        /// <param name="source">The source object.</param>
        /// <returns>A new instance of type TTarget with the properties of TSource.</returns>
        public static TTarget Clone<TSource, TTarget>(TSource source) where TTarget : new()
        {
            var target = new TTarget();
            Copy(source, target);
            return target;
        }

        /// <summary>
        /// Copies the properties of one object to another.
        /// </summary>
        /// <typeparam name="TSource">The type of the source object.</typeparam>
        /// <typeparam name="TTarget">The type of the target object.</typeparam>
        /// <param name="source">The source object.</param>
        /// <param name="target">The target object.</param>
        /// <returns>The source object.</returns>
        public static TSource Copy<TSource, TTarget>(TSource source, TTarget target)
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
        public static string GetExpressionPropertyName<TEntity, TKey>(Expression<Func<TEntity, TKey>> selector)
        {
            var propertyExpression = (MemberExpression)selector.Body;
            var propertyName = propertyExpression.Member.Name;

            return propertyName;
        }
    }
}
