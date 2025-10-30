using Poyraz.EntityFramework.Attributes;
using Poyraz.EntityFramework.Specifications;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Poyraz.EntityFramework.Utilities
{
	public class QueryStringParameters
	{
		private int _pageSize = SpecificationConstants.DefaultPageSize;

		public int PageNumber { get; set; } = 1;
		public int PageSize
		{
			get => _pageSize;
			set => _pageSize = (value > SpecificationConstants.MaxPageSize) ? SpecificationConstants.MaxPageSize : value;
		}
		public string OrderBy { get; set; }
		public string Search { get; set; }
		public string FullTextSearch { get; set; }

		/// <summary>
		/// Generates an order query string by mapping DTO properties to entity properties.
		/// </summary>
		/// <typeparam name="TDto">The DTO type.</typeparam>
		/// <param name="entityType">The entity type.</param>
		/// <returns>A transformed order query string for the entity.</returns>
		internal (string OrderQuery, Dictionary<string, string> SearchFields)? GetOrderAndSearchFromQueryString<TDto>(Type entityType)
			where TDto : class
		{
			if (string.IsNullOrWhiteSpace(OrderBy) && string.IsNullOrEmpty(FullTextSearch))
				return null;

			if (!string.IsNullOrEmpty(OrderBy) && OrderBy.Contains('.'))
				throw new InvalidCastException("Nested properties are not supported in OrderBy.");

			// Generate a unique cache key based on TDto and entityType
			string cacheKey = $"{typeof(TDto).FullName}:{entityType.FullName}";

			// Retrieve or add the mapping to the cache
			var dtoPropertiesWithAttributes = Cache.GetOrAdd(cacheKey, key =>
			{
				var entityProps = entityType.GetProperties();

				return typeof(TDto).GetProperties()
					.Select(s => new
					{
						s.Name,
						s.PropertyType,
						Attr = s.GetCustomAttribute<SortEntityFieldAttribute>(),
						NonSearchAttr = s.GetCustomAttribute<NonSearchAttribute>() != null
					})
					.Where(w => w.Attr != null || entityProps.Any(c => c.Name == w.Name))
					.Select(s => new DtoPropertyMapping
					{
						Name = s.Name,
						PropertyType = s.PropertyType,
						EntityPropName = s.Attr == null
							? s.Name
							: (s.Attr.EntityName == entityType.Name ? s.Attr.PropertyName : s.Attr.SortName),
						EntityPropType = s.Attr == null ? entityProps.FirstOrDefault(c => c.Name == s.Name)?.PropertyType : s.PropertyType,
						NonSearch = s.NonSearchAttr
					})
					.ToList();
			});

			if (!dtoPropertiesWithAttributes.Any())
				return null;

			string[] orderParams = Array.Empty<string>();
			Dictionary<string, string> SearchQuery = null;

			if (!string.IsNullOrEmpty(OrderBy))
			{
				orderParams = OrderBy.Trim().Split(',');
				for (int i = 0; i < orderParams.Length; i++)
				{
					var parts = orderParams[i].Trim().Split(" ");
					var propertyFromQueryName = parts[0];
					var direction = parts.Length > 1 ? parts[1] : "asc";
					var entityField = dtoPropertiesWithAttributes.FirstOrDefault(a => propertyFromQueryName.Equals(a.Name, StringComparison.InvariantCultureIgnoreCase));

					if (entityField != null)
						orderParams[i] = entityField.EntityPropName + " " + direction;
				}
			}

			if (!string.IsNullOrEmpty(FullTextSearch))
				SearchQuery = dtoPropertiesWithAttributes
					.Where(w => w.EntityPropType == typeof(string) && !string.IsNullOrEmpty(w.EntityPropName) && !w.NonSearch)
					.ToDictionary(k => k.EntityPropName, _ => FullTextSearch);

			return (string.Join(",", orderParams), SearchQuery);
		}

		// Static cache to store reflection results
		private static readonly ConcurrentDictionary<string, List<DtoPropertyMapping>> Cache = new ConcurrentDictionary<string, List<DtoPropertyMapping>>();
		private class DtoPropertyMapping
		{
			public string Name { get; set; }
			public Type PropertyType { get; set; }
			public string EntityPropName { get; set; }
			public Type EntityPropType { get; set; }
			public bool NonSearch { get; set; }
		}
	}

	public class DateRange
	{
		public DateOnly? Start { get; set; }
		public DateOnly? End { get; set; }
	}
}
