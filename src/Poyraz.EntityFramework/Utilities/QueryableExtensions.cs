using Microsoft.EntityFrameworkCore;
using Poyraz.EntityFramework.Abstractions;
using Poyraz.EntityFramework.Specifications.Evaluators;
using Poyraz.Helpers.Primitives;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
			Expression<Func<TEntity, bool>>? searchFieldsExp = null;
			Expression<Func<TEntity, bool>>? dynamicSearchExp = null;

			// Apply custom search filter
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
					searchFieldsExp = Expression.Lambda<Func<TEntity, bool>>(containsExpression, parameter);
				}
			}

			var result = queryStringParameters.GetOrderAndSearchFromQueryString<TDto>(query.ElementType);
			// Apply search query string
			if (result.HasValue && result.Value.SearchFields != null)
			{
				dynamicSearchExp = SpecificationOrderEvaluator.ApplySearch<TEntity>(result.Value.SearchFields);
			}

			if (searchFieldsExp != null && dynamicSearchExp != null)
			{
				var parameter = Expression.Parameter(typeof(TEntity), "w");
				var body = Expression.OrElse(
					Expression.Invoke(searchFieldsExp, parameter),
					Expression.Invoke(dynamicSearchExp, parameter)
				);

				searchFieldsExp = Expression.Lambda<Func<TEntity, bool>>(body, parameter);
			}
			else if (searchFieldsExp == null && dynamicSearchExp != null)
			{
				searchFieldsExp = dynamicSearchExp;
			}


			if (searchFieldsExp != null)
				query = query.Where(searchFieldsExp);

			// Get total count
			int totalCount = await query.CountAsync();

			// Apply order query string
			if (result.HasValue)
				query = SpecificationOrderEvaluator.ApplySort(query, result.Value.OrderQuery);

			// Apply paging
			int skip = 0;
			if (queryStringParameters.PageNumber > 0)
				skip = (queryStringParameters.PageNumber - 1) * queryStringParameters.PageSize;
			query = query.Skip(skip).Take(queryStringParameters.PageSize);

			// convert to dto model
			var resultDtoList = await query.Select(projection).ToArrayAsync();

			return new ResultList<TDto>(resultDtoList, totalCount);
		}

		public static IQueryable<T> ApplyDateRangeFilter<T, TProperty>(this IQueryable<T> query, DateRange range, Expression<Func<T, TProperty>> selector)
		{
			if (range == null)
				return query;

			bool nullableType = Nullable.GetUnderlyingType(typeof(TProperty)) != null;
			var propertyType = Nullable.GetUnderlyingType(typeof(TProperty)) ?? typeof(TProperty);
			var memberExpression = selector.Body;

			if (propertyType == typeof(DateOnly))
			{
				if (range.Start.HasValue)
				{
					query = ApplyComparisonFilter(query, memberExpression, range.Start.Value, nullableType, true, selector.Parameters);
				}

				if (range.End.HasValue)
				{
					query = ApplyComparisonFilter(query, memberExpression, range.End.Value, nullableType, false, selector.Parameters);
				}
			}
			else if (propertyType == typeof(DateTime))
			{
				if (range.Start.HasValue)
				{
					var startValue = range.Start.Value.ToDateTime(TimeOnly.MinValue);
					query = ApplyComparisonFilter(query, memberExpression, startValue, nullableType, true, selector.Parameters);
				}

				if (range.End.HasValue)
				{
					var endValue = range.End.Value.ToDateTime(TimeOnly.MaxValue);
					query = ApplyComparisonFilter(query, memberExpression, endValue, nullableType, false, selector.Parameters);
				}
			}
			else
			{
				throw new InvalidOperationException("Unsupported property type.");
			}

			return query;
		}

		private static IQueryable<T> ApplyComparisonFilter<T, TValue>(IQueryable<T> query, Expression memberExpression, TValue value, bool isNullable, bool isGreaterThanOrEqual, ReadOnlyCollection<ParameterExpression> parameters)
		{
			Expression finalExpr;
			var constantExpr = Expression.Constant(value, typeof(TValue));

			if (isNullable)
			{
				var hasValueExpr = Expression.Property(memberExpression, "HasValue");
				var valueExpr = Expression.Property(memberExpression, "Value");

				Expression comparisonExpr;
				if (isGreaterThanOrEqual)
					comparisonExpr = Expression.GreaterThanOrEqual(valueExpr, constantExpr);
				else
					comparisonExpr = Expression.LessThanOrEqual(valueExpr, constantExpr);

				finalExpr = Expression.AndAlso(hasValueExpr, comparisonExpr);
			}
			else
			{
				if (isGreaterThanOrEqual)
					finalExpr = Expression.GreaterThanOrEqual(memberExpression, constantExpr);
				else
					finalExpr = Expression.LessThanOrEqual(memberExpression, constantExpr);
			}

			return query.Where(Expression.Lambda<Func<T, bool>>(finalExpr, parameters));
		}

	}
}
