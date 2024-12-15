using Poyraz.EntityFramework.Abstractions;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Poyraz.EntityFramework.Specifications.Evaluators
{
	internal static class SpecificationOrderEvaluator
	{
		private static IOrderedQueryable<T> ApplyOrder<T>(IQueryable<T> source, string propertyName, bool descending, bool anotherLevel)
		{
			ParameterExpression param = Expression.Parameter(typeof(T), string.Empty); // I don't care about some naming

			string[] properties = propertyName.Split('.');
			MemberExpression property = Expression.PropertyOrField(param, properties[0]);

			for (int i = 1; i < properties.Length; i++)
			{
				property = Expression.PropertyOrField(property, properties[i]);
			}

			LambdaExpression sort = Expression.Lambda(property, param);
			MethodCallExpression call = Expression.Call(
				typeof(Queryable),
				(!anotherLevel ? "OrderBy" : "ThenBy") + (descending ? "Descending" : string.Empty),
				new[] { typeof(T), property.Type },
				source.Expression,
				Expression.Quote(sort));

			return (IOrderedQueryable<T>)source.Provider.CreateQuery<T>(call);
		}
		private static IOrderedQueryable<T> OrderBy<T>(this IQueryable<T> source, string propertyName)
		{
			return ApplyOrder(source, propertyName, false, false);
		}
		private static IOrderedQueryable<T> OrderByDescending<T>(this IQueryable<T> source, string propertyName)
		{
			return ApplyOrder(source, propertyName, true, false);
		}
		private static IOrderedQueryable<T> ThenBy<T>(this IOrderedQueryable<T> source, string propertyName)
		{
			return ApplyOrder(source, propertyName, false, true);
		}
		private static IOrderedQueryable<T> ThenByDescending<T>(this IOrderedQueryable<T> source, string propertyName)
		{
			return ApplyOrder(source, propertyName, true, true);
		}

		internal static IOrderedQueryable<T> ApplySort<T>(this IQueryable<T> source, string orderQueryString) where T : IEntity
		{
			if (string.IsNullOrWhiteSpace(orderQueryString) && typeof(T).IsAssignableTo(typeof(ISimpleAuditable)))
			{
				return source.OrderBy(nameof(ISimpleAuditable.CreatedAt));
			}

			var orderParams = orderQueryString.Trim().Split(',');
			var propertyInfos = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

			IOrderedQueryable<T> orderQueryable = null;
			foreach (var param in orderParams)
			{
				if (string.IsNullOrWhiteSpace(param))
					continue;

				var propertyFromQueryName = param.Trim().Split(" ")[0];

				if (propertyFromQueryName.IndexOf(".") == -1)
				{
					var objectProperty = propertyInfos.FirstOrDefault(pi => pi.Name.Equals(propertyFromQueryName, StringComparison.InvariantCultureIgnoreCase));
					if (objectProperty != null)
					{
						propertyFromQueryName = objectProperty.Name;
					}
					else
						continue;
				}

				if (orderQueryable == null)
				{
					if (param.Trim().EndsWith("desc", StringComparison.InvariantCultureIgnoreCase))
						orderQueryable = source.OrderByDescending(propertyFromQueryName);
					else
						orderQueryable = source.OrderBy(propertyFromQueryName);
				}
				else
				{
					if (param.Trim().EndsWith("desc", StringComparison.InvariantCultureIgnoreCase))
						orderQueryable = orderQueryable.ThenByDescending(propertyFromQueryName);
					else
						orderQueryable = orderQueryable.ThenBy(propertyFromQueryName);
				}
			}

			if (orderQueryable == null && typeof(T).IsAssignableTo(typeof(ISimpleAuditable)))
				return source.OrderBy(nameof(ISimpleAuditable.CreatedAt));

			return orderQueryable;
		}
	}
}
