namespace RotationSolver.Basic.Helpers
{
	internal static class ReflectionHelper
	{
		private static readonly System.Collections.Concurrent.ConcurrentDictionary<(Type type, Type ofT), PropertyInfo[]> s_staticPropsCache = new();
		private static readonly System.Collections.Concurrent.ConcurrentDictionary<Type, MethodInfo[]> s_methodsCache = new();

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

			var allProperties = type.GetProperties(BindingFlags.Static | BindingFlags.Public);
			List<PropertyInfo> filteredProperties = [];
			foreach (var prop in allProperties)
			{
				if (typeof(T).IsAssignableFrom(prop.PropertyType) &&
					prop.GetCustomAttribute<ObsoleteAttribute>() == null)
				{
					filteredProperties.Add(prop);
				}
			}

			var baseProperties = type.BaseType?.GetStaticProperties<T>() ?? [];

			var result = new PropertyInfo[filteredProperties.Count + baseProperties.Length];
			filteredProperties.CopyTo(result, 0);
			if (baseProperties.Length > 0)
			{
				Array.Copy(baseProperties, 0, result, filteredProperties.Count, baseProperties.Length);
			}

			s_staticPropsCache[(type, typeof(T))] = result;
			return result;
		}

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

			var allMethods = type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			List<MethodInfo> filteredMethods = [];
			foreach (var method in allMethods)
			{
				if (!method.IsConstructor)
				{
					filteredMethods.Add(method);
				}
			}

			var baseMethods = type.BaseType?.GetAllMethodInfo() ?? [];

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

		internal static PropertyInfo? GetPropertyInfo(this Type type, string name)
		{
			ArgumentNullException.ThrowIfNull(type);

			if (string.IsNullOrEmpty(name))
			{
				throw new ArgumentException("Property name cannot be null or empty", nameof(name));
			}

			var property = type.GetProperty(name, BindingFlags.Static | BindingFlags.Public);

			return property ?? type.BaseType?.GetPropertyInfo(name);
		}

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

			var method = type.GetMethod(name, BindingFlags.Static | BindingFlags.Public);

			return method ?? type.BaseType?.GetMethodInfo(name);
		}
	}
}