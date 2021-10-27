using System;
using System.Data;
using System.Linq;
using System.Linq.Expressions;

namespace ProxyR.Core.Extensions
{
    public static class ObjectExtensions
    {
        /// <summary>
        /// Tests every primitive property value to see if they are equal.
        /// </summary>
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
        /// Creates a new instance of the target type, and copies the properties with same names from the source object.
        /// </summary>
        public static TTarget Clone<TSource, TTarget>(this TSource source) where TTarget : new()
        {
            var target = new TTarget();
            Copy(source, target);
            return target;
        }

        /// <summary>
        /// Copies all the properties from one object to another.
        /// But only where the properties have the same name.
        /// </summary>
        public static TSource Copy<TSource, TTarget>(this TSource source, TTarget target)
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
        /// Gets a property name given an selector expression.
        /// </summary>
        public static string GetExpressionPropertyName<TEntity, TKey>(Expression<Func<TEntity, TKey>> selector) => ((MemberExpression)selector.Body).Member.Name;
    }
}
