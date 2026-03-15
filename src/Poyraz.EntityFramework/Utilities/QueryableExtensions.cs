using Microsoft.EntityFrameworkCore;
using Poyraz.EntityFramework.Abstractions;
using Poyraz.EntityFramework.Specifications.Evaluators;
using Poyraz.Helpers.Primitives;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Poyraz.EntityFramework.Utilities
{
	public static class QueryableExtensions
	{
		/// TODO: Time zone handling should be improved in the future to be more flexible and not hardcoded. For now, we are using "Europe/Istanbul" as the GMT+3 time zone for date range filtering.
		private static TimeZoneInfo _gmt3TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");

		public static IQueryable<TEntity> ApplySpecification<TEntity>(this IQueryable<TEntity> query, ISpecification<TEntity> specification)
			where TEntity : class, IEntity
		{
			return SpecificationEvaluator<TEntity>.GetQuery(query, specification);
		}

		/// <summary>
		/// Applies filtering, sorting, searching and pagination based on the provided QueryStringParameters.
		/// </summary>
		/// <typeparam name="TEntity"></typeparam>
		/// <typeparam name="TDto"></typeparam>
		/// <param name="query"></param>
		/// <param name="queryStringParameters"></param>
		/// <param name="projection"></param>
		/// <param name="searchFields"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public static async Task<ResultList<TDto>> ApplyQueryStringParametersAsync<TEntity, TDto>(this IQueryable<TEntity> query, QueryStringParameters queryStringParameters, Expression<Func<TEntity, TDto>> projection, List<Expression<Func<TEntity, string>>> searchFields = null, CancellationToken cancellationToken = default)
			where TEntity : IEntity
			where TDto : class
		{
			Expression<Func<TEntity, bool>>? searchFieldsExp = null;

			// Apply custom search filter using EF.Functions.Like
			if (!string.IsNullOrWhiteSpace(queryStringParameters.Search) && searchFields?.Count > 0)
			{
				var parameter = Expression.Parameter(typeof(TEntity), "w");
				var searchPattern = Expression.Constant($"%{queryStringParameters.Search}%");
				var efFunctions = Expression.Property(null, typeof(EF).GetProperty(nameof(EF.Functions))!);

				Expression? likeExpression = null;

				foreach (var field in searchFields)
				{
					MemberExpression? member = GetMemberAccess(parameter, field.Body);

					if (member == null)
						continue;

					var likeMethod = typeof(DbFunctionsExtensions).GetMethod(nameof(DbFunctionsExtensions.Like), new[] { typeof(DbFunctions), typeof(string), typeof(string) });

					var likeCall = Expression.Call(likeMethod!, efFunctions, member, searchPattern);

					likeExpression = likeExpression == null
						? likeCall
						: Expression.OrElse(likeExpression, likeCall);
				}

				if (likeExpression != null)
				{
					searchFieldsExp = Expression.Lambda<Func<TEntity, bool>>(likeExpression, parameter);
				}
			}

			if (searchFields == null && string.IsNullOrWhiteSpace(queryStringParameters.FullTextSearch)
				&& !string.IsNullOrWhiteSpace(queryStringParameters.Search))
			{
				queryStringParameters.FullTextSearch = queryStringParameters.Search;
			}

			var result = queryStringParameters.GetOrderAndSearchFromQueryString<TDto>(query.ElementType);

			if (!string.IsNullOrWhiteSpace(queryStringParameters.FullTextSearch))
			{
				Expression<Func<TEntity, bool>>? dynamicSearchExp = null;
				if (result?.SearchFields != null)
				{
					dynamicSearchExp = SpecificationOrderEvaluator.ApplySearch<TEntity>(result.Value.SearchFields);
				}

				if (searchFieldsExp != null && dynamicSearchExp != null)
				{
					// Merge expressions manually to avoid Invoke
					var param = Expression.Parameter(typeof(TEntity), "w");
					var left = RebindParameter(searchFieldsExp.Body, searchFieldsExp.Parameters[0], param);
					var right = RebindParameter(dynamicSearchExp.Body, dynamicSearchExp.Parameters[0], param);
					var merged = Expression.OrElse(left, right);
					searchFieldsExp = Expression.Lambda<Func<TEntity, bool>>(merged, param);
				}
				else if (searchFieldsExp == null && dynamicSearchExp != null)
				{
					searchFieldsExp = dynamicSearchExp;
				}
			}

			if (searchFieldsExp != null)
				query = query.Where(searchFieldsExp);

			int totalCount = await query.CountAsync(cancellationToken);

			query = SpecificationOrderEvaluator.ApplySort(query, result.HasValue ? result.Value.OrderQuery : string.Empty);

			query = query.ApplyPagination(queryStringParameters);

			return await query.ToResultListAsync(projection, totalCount, cancellationToken);
		}

		public static IQueryable<TEntity> ApplyPagination<TEntity>(this IQueryable<TEntity> query, QueryStringParameters queryStringParameters)
			where TEntity : IEntity
		{
			if (queryStringParameters == null)
				return query;

			if (queryStringParameters.PageNumber > 0)
			{
				int skip = (queryStringParameters.PageNumber - 1) * queryStringParameters.PageSize;
				query = query.Skip(skip);
			}

			return query.Take(queryStringParameters.PageSize);
		}

		public static IQueryable<TEntity> ApplySort<TEntity, TDto>(this IQueryable<TEntity> query, QueryStringParameters queryStringParameters)
			where TEntity : IEntity
			where TDto : class
		{
			if (queryStringParameters == null)
				return query;

			var result = queryStringParameters.GetOrderAndSearchFromQueryString<TDto>(query.ElementType);
			return SpecificationOrderEvaluator.ApplySort(query, result.HasValue ? result.Value.OrderQuery : string.Empty);
		}

		public static IQueryable<T> ApplyRangeFilter<T, TProperty>(this IQueryable<T> query, IRangeFilter range, Expression<Func<T, TProperty>> selector)
		{
			if (range == null)
				return query;

			bool nullableType = Nullable.GetUnderlyingType(typeof(TProperty)) != null;
			var propertyType = Nullable.GetUnderlyingType(typeof(TProperty)) ?? typeof(TProperty);
			var memberExpression = selector.Body;

			if (range is DateRange dateRange)
			{
				if (propertyType == typeof(DateOnly))
				{
					if (dateRange.Start.HasValue)
					{
						query = ApplyComparisonFilter(query, memberExpression, dateRange.Start.Value, nullableType, true, selector.Parameters);
					}

					if (dateRange.End.HasValue)
					{
						query = ApplyComparisonFilter(query, memberExpression, dateRange.End.Value, nullableType, false, selector.Parameters);
					}
				}
				else if (propertyType == typeof(DateTime))
				{
					if (dateRange.Start.HasValue)
					{
						var startValue = TimeZoneInfo.ConvertTimeToUtc(dateRange.Start.Value.ToDateTime(TimeOnly.MinValue), _gmt3TimeZone);
						query = ApplyComparisonFilter(query, memberExpression, startValue, nullableType, true, selector.Parameters);
					}

					if (dateRange.End.HasValue)
					{
						var endValue = TimeZoneInfo.ConvertTimeToUtc(dateRange.End.Value.ToDateTime(TimeOnly.MaxValue), _gmt3TimeZone);
						query = ApplyComparisonFilter(query, memberExpression, endValue, nullableType, false, selector.Parameters);
					}
				}
				else
				{
					throw new InvalidOperationException("Unsupported property type.");
				}
			}
			else if (range is NumberRange numberRange)
			{
				if (numberRange.Start.HasValue)
				{
					query = ApplyComparisonFilter(query, memberExpression, numberRange.Start.Value, nullableType, true, selector.Parameters);
				}
				if (numberRange.End.HasValue)
				{
					query = ApplyComparisonFilter(query, memberExpression, numberRange.End.Value, nullableType, false, selector.Parameters);
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

		private static Expression RebindParameter(Expression expression, ParameterExpression source, ParameterExpression target)
		{
			return new RebindVisitor(source, target).Visit(expression)!;
		}

		private class RebindVisitor : ExpressionVisitor
		{
			private readonly ParameterExpression _source;
			private readonly ParameterExpression _target;

			public RebindVisitor(ParameterExpression source, ParameterExpression target)
			{
				_source = source;
				_target = target;
			}

			protected override Expression VisitParameter(ParameterExpression node)
			{
				return node == _source ? _target : base.VisitParameter(node);
			}
		}

		private static MemberExpression? GetMemberAccess(ParameterExpression parameter, Expression body)
		{
			if (body is MemberExpression memberExpr)
			{
				var members = new Stack<MemberInfo>();
				while (memberExpr != null)
				{
					members.Push(memberExpr.Member);
					memberExpr = memberExpr.Expression as MemberExpression;
				}

				Expression current = parameter;
				while (members.Count > 0)
				{
					current = Expression.PropertyOrField(current, members.Pop().Name);
				}

				return (MemberExpression)current;
			}

			if (body is UnaryExpression unary && unary.Operand is MemberExpression unaryMember)
			{
				return GetMemberAccess(parameter, unaryMember);
			}

			return null;
		}

		public static ResultList<TDto> ToResultList<TEntity, TDto>(this IQueryable<TEntity> query, Expression<Func<TEntity, TDto>> projection)
			where TEntity : IEntity
			where TDto : class
		{
			var result = query.Select(projection).ToArray();
			return new ResultList<TDto>(result);
		}

		public static async Task<ResultList<TDto>> ToResultListAsync<TEntity, TDto>(this IQueryable<TEntity> query, Expression<Func<TEntity, TDto>> projection, int? totalResults = null, CancellationToken cancellationToken = default)
		{
			var result = await query.Select(projection).ToArrayAsync(cancellationToken);
			return new ResultList<TDto>(result, totalResults ?? result.Length);
		}
	}
}
