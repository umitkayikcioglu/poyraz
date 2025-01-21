using Poyraz.EntityFramework.Attributes;
using Poyraz.EntityFramework.Specifications;
using System;
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

		/// <summary>
		/// Generates an order query string by mapping DTO properties to entity properties.
		/// </summary>
		/// <typeparam name="TDto">The DTO type.</typeparam>
		/// <param name="entityType">The entity type.</param>
		/// <returns>A transformed order query string for the entity.</returns>
		internal (string OrderQuery, Dictionary<string, string> SearchFields)? GetOrderAndSearchFromQueryString<TDto>(Type entityType)
			where TDto : class
		{
			if (string.IsNullOrWhiteSpace(OrderBy) && string.IsNullOrEmpty(Search))
				return null;

			if (!string.IsNullOrEmpty(OrderBy) && OrderBy.Contains('.'))
				throw new InvalidCastException("Nested properties are not supported in OrderBy.");

			var dtoPropertiesWithAttributes = typeof(TDto).GetProperties()
							.Select(s => new
							{
								s.Name,
								s.PropertyType,
								Attr = s.GetCustomAttribute<SortEntityFieldAttribute>()
							})
							.Select(s => new
							{
								s.Name,
								s.PropertyType,
								EntityPropName = s.Attr == null ? s.Name : (s.Attr.EntityName == entityType.Name ? s.Attr.PropertyName : s.Attr.SortName)
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
					var propertyFromQueryName = orderParams[i].Trim().Split(" ")[0];
					var entityField = dtoPropertiesWithAttributes.FirstOrDefault(a => propertyFromQueryName.Equals(a.Name, StringComparison.InvariantCultureIgnoreCase));

					if (entityField != null)
						orderParams[i] = entityField.EntityPropName + " " + orderParams[i].Trim().Split(" ")[1];
				}
			}

			if (!string.IsNullOrEmpty(Search))
				SearchQuery = dtoPropertiesWithAttributes.Where(w => w.PropertyType == typeof(string)).ToDictionary((key) => key.EntityPropName, (val) => Search);

			return (string.Join(",", orderParams), SearchQuery);
		}
	}
}
