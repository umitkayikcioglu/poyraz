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

			int totalCount = await query.CountAsync();

			if (result.HasValue)
				query = SpecificationOrderEvaluator.ApplySort(query, result.Value.OrderQuery);

			if (queryStringParameters.PageNumber > 0)
			{
				int skip = (queryStringParameters.PageNumber - 1) * queryStringParameters.PageSize;
				query = query.Skip(skip);
			}

			query = query.Take(queryStringParameters.PageSize);

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

	}
}
