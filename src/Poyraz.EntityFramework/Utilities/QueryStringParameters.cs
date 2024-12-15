using Poyraz.EntityFramework.Attributes;
using Poyraz.EntityFramework.Specifications;
using System;
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
		internal string GetOrderQueryString<TDto>(Type entityType)
			where TDto : class
		{
			if (string.IsNullOrWhiteSpace(OrderBy))
				return null;

			if (OrderBy.Contains('.'))
				throw new InvalidCastException("Nested properties are not supported in OrderBy.");



			var dtoPropertiesWithAttributes = typeof(TDto).GetProperties()
							.Select(s => new
							{
								s.Name,
								Attr = s.GetCustomAttribute<SortEntityFieldAttribute>()
							})
							.Where(w => w.Attr != null);

			if (!dtoPropertiesWithAttributes.Any())
				return null;

			var orderParams = OrderBy.Trim().Split(',');
			for (int i = 0; i < orderParams.Length; i++)
			{
				var propertyFromQueryName = orderParams[i].Trim().Split(" ")[0];
				var entityField = dtoPropertiesWithAttributes.FirstOrDefault(a => propertyFromQueryName.Equals(a.Name, StringComparison.InvariantCultureIgnoreCase));
				if (entityField != null)
				{
					if (entityField.Attr.EntityName == entityType.Name)
						orderParams[i] = entityField.Attr.PropertyName + " " + orderParams[i].Trim().Split(" ")[1];
					else
						orderParams[i] = entityField.Attr.SortName + " " + orderParams[i].Trim().Split(" ")[1];
				}
			}

			return string.Join(",", orderParams);
		}
	}
}
