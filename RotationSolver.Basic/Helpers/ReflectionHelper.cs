namespace RotationSolver.Basic.Helpers
{
    /// <summary>
    /// Helper class for reflection-related operations.
    /// </summary>
    internal static class ReflectionHelper
    {
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<(Type type, Type ofT), PropertyInfo[]> s_staticPropsCache = new();
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<Type, MethodInfo[]> s_methodsCache = new();

        /// <summary>
        /// Gets the static properties of a specified type.
        /// </summary>
        /// <typeparam name="T">The type of the properties to get.</typeparam>
        /// <param name="type">The type to get the properties from.</param>
        /// <returns>An array of static properties of the specified type.</returns>
        internal static PropertyInfo[] GetStaticProperties<T>(this Type? type)
        {
            if (type == null)
            {
                return [];
            }

            if (s_staticPropsCache.TryGetValue((type, typeof(T)), out var cached))
            {
                return cached;
            }

            PropertyInfo[] allProperties = type.GetProperties(BindingFlags.Static | BindingFlags.Public);
            List<PropertyInfo> filteredProperties = [];
            foreach (PropertyInfo prop in allProperties)
            {
                if (typeof(T).IsAssignableFrom(prop.PropertyType) &&
                    prop.GetCustomAttribute<ObsoleteAttribute>() == null)
                {
                    filteredProperties.Add(prop);
                }
            }

            PropertyInfo[] baseProperties = type.BaseType?.GetStaticProperties<T>() ?? [];

            // Combine filteredProperties and baseProperties
            PropertyInfo[] result = new PropertyInfo[filteredProperties.Count + baseProperties.Length];
            filteredProperties.CopyTo(result, 0);
            if (baseProperties.Length > 0)
            {
                Array.Copy(baseProperties, 0, result, filteredProperties.Count, baseProperties.Length);
            }

            s_staticPropsCache[(type, typeof(T))] = result;
            return result;
        }

        /// <summary>
        /// Gets all method information from a specified type.
        /// </summary>
        /// <param name="type">The type to get the methods from.</param>
        /// <returns>An enumerable of method information.</returns>
        internal static IEnumerable<MethodInfo> GetAllMethodInfo(this Type? type)
        {
            if (type == null)
            {
                return [];
            }

            if (s_methodsCache.TryGetValue(type, out var cached))
            {
                return cached;
            }

            MethodInfo[] allMethods = type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            List<MethodInfo> filteredMethods = [];
            foreach (MethodInfo method in allMethods)
            {
                if (!method.IsConstructor)
                {
                    filteredMethods.Add(method);
                }
            }

            IEnumerable<MethodInfo> baseMethods = type.BaseType?.GetAllMethodInfo() ?? [];

            // Combine filteredMethods and baseMethods without LINQ
            List<MethodInfo> resultList = new(filteredMethods.Count + 8);
            resultList.AddRange(filteredMethods);
            foreach (var m in baseMethods)
            {
                resultList.Add(m);
            }

            MethodInfo[] result = [.. resultList];
            s_methodsCache[type] = result;
            return result;
        }

        /// <summary>
        /// Gets the property information for a specified property name.
        /// </summary>
        /// <param name="type">The type to get the property from.</param>
        /// <param name="name">The name of the property.</param>
        /// <returns>The property information if found, otherwise null.</returns>
        internal static PropertyInfo? GetPropertyInfo(this Type type, string name)
        {
            ArgumentNullException.ThrowIfNull(type);

            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Property name cannot be null or empty", nameof(name));
            }

            PropertyInfo? property = type.GetProperty(name, BindingFlags.Static | BindingFlags.Public);

            return property ?? type.BaseType?.GetPropertyInfo(name);
        }

        /// <summary>
        /// Gets the method information for a specified method name.
        /// </summary>
        /// <param name="type">The type to get the method from.</param>
        /// <param name="name">The name of the method.</param>
        /// <returns>The method information if found, otherwise null.</returns>
        internal static MethodInfo? GetMethodInfo(this Type? type, string name)
        {
            if (type == null)
            {
                return null;
            }

            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Method name cannot be null or empty", nameof(name));
            }

            MethodInfo? method = type.GetMethod(name, BindingFlags.Static | BindingFlags.Public);

            return method ?? type.BaseType?.GetMethodInfo(name);
        }
    }
}