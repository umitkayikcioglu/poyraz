using Microsoft.EntityFrameworkCore;
using Poyraz.EntityFramework.Abstractions;
using Poyraz.EntityFramework.Specifications.Evaluators;
using Poyraz.Helpers.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Poyraz.EntityFramework.Utilities
{
	public static class QueryableExtensions
	{
		public static async Task<ResultList<TDto>> ApplyQueryStringParametersAsync<TEntity, TDto>(this IQueryable<TEntity> query, QueryStringParameters queryStringParameters, Expression<Func<TEntity, TDto>> projection, List<Expression<Func<TEntity, string>>> searchFields = null)
			where TEntity : IEntity
			where TDto : class
		{

			// Apply search filter
			if (!string.IsNullOrWhiteSpace(queryStringParameters.Search) && searchFields != null && searchFields.Count > 0)
			{
				// Combine fields dynamically
				var parameter = Expression.Parameter(typeof(TEntity), "w");
				var searchValue = Expression.Constant(queryStringParameters.Search);

				Expression? containsExpression = null;

				foreach (var field in searchFields)
				{
					// Generate: w.Field.Contains(queryStringParameters.Search)
					var fieldExpression = Expression.Invoke(field, parameter);
					var containsCall = Expression.Call(fieldExpression, "Contains", null, searchValue);

					containsExpression = containsExpression == null
						? containsCall
						: Expression.OrElse(containsExpression, containsCall);
				}

				if (containsExpression != null)
				{
					var lambda = Expression.Lambda<Func<TEntity, bool>>(containsExpression, parameter);
					query = query.Where(lambda);
				}
			}

			// Get total count
			int totalCount = await query.CountAsync();

			// Apply order query string
			string orderQueryString = queryStringParameters.GetOrderQueryString<TDto>(query.ElementType);
			query = SpecificationOrderEvaluator.ApplySort(query, orderQueryString);

			// Apply paging
			int skip = 0;
			if (queryStringParameters.PageNumber > 0)
				skip = (queryStringParameters.PageNumber - 1) * queryStringParameters.PageSize;
			query = query.Skip(skip).Take(queryStringParameters.PageSize);

			// convert to dto model
			var resultDtoList = await query.Select(projection).ToArrayAsync();

			return new ResultList<TDto>(resultDtoList, totalCount);
		}

	}
}
