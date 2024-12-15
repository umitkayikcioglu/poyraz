using Poyraz.EntityFramework.Abstractions;
using Poyraz.EntityFramework.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Poyraz.EntityFramework.Specifications
{
	public abstract class Specification<TEntity> : ISpecification<TEntity> where TEntity : IEntity
	{
		protected Specification(Expression<Func<TEntity, bool>>? criteria)
		{
			Criteria = criteria;
		}

		public bool IsSplitQuery { get; protected set; }
		public Expression<Func<TEntity, bool>> Criteria { get; }
		public List<Expression<Func<TEntity, object>>> Includes { get; } = new List<Expression<Func<TEntity, object>>>();
		public List<string> IncludeStrings { get; } = new List<string>();
		public Expression<Func<TEntity, object>> OrderBy { get; private set; }
		public Expression<Func<TEntity, object>> OrderByDescending { get; private set; }
		public string OrderByWithQueryString { get; private set; }
		public Expression<Func<TEntity, object>> GroupBy { get; private set; }

		public int Take { get; private set; }
		public int Skip { get; private set; }
		public bool IsPagingEnabled { get; private set; } = false;

		protected virtual void AddInclude(Expression<Func<TEntity, object>> includeExpression)
		{
			Includes.Add(includeExpression);
		}

		protected virtual void AddInclude(string includeString)
		{
			IncludeStrings.Add(includeString);
		}

		protected virtual void ApplyPaging(int skip, int take)
		{
			Skip = skip;
			Take = take;
			IsPagingEnabled = true;
		}

		protected virtual void ApplyOrderBy(Expression<Func<TEntity, object>> orderByExpression)
		{
			OrderBy = orderByExpression;
		}

		protected virtual void ApplyOrderByDescending(Expression<Func<TEntity, object>> orderByDescendingExpression)
		{
			OrderByDescending = orderByDescendingExpression;
		}

		public virtual void ApplyQueryStringParameters<TDto>(QueryStringParameters queryStringParameters) where TDto : class
		{
			OrderByWithQueryString = queryStringParameters.OrderBy;
			if (!string.IsNullOrEmpty(OrderByWithQueryString))
			{
				if (OrderByWithQueryString.IndexOf('.') > -1)
					throw new InvalidCastException();
				else
				{
					var props = typeof(TDto).GetProperties();
					var attr = props
										   .Select(s => new
										   {
											   s.Name,
											   Attr = s.GetCustomAttribute<SortEntityFieldAttribute>()
										   })
										   .Where(w => w.Attr != null);
					if (attr.Any())
					{
						var orderParams = OrderByWithQueryString.Trim().Split(',');
						for (int i = 0; i < orderParams.Length; i++)
						{
							var propertyFromQueryName = orderParams[i].Trim().Split(" ")[0];
							var entityField = attr.FirstOrDefault(a => propertyFromQueryName.Equals(a.Name, StringComparison.InvariantCultureIgnoreCase));
							if (entityField != null)
							{
								if (entityField.Attr.EntityName == typeof(TEntity).Name)
									orderParams[i] = entityField.Attr.PropertyName + " " + orderParams[i].Trim().Split(" ")[1];
								else
									orderParams[i] = entityField.Attr.SortName + " " + orderParams[i].Trim().Split(" ")[1];
							}
						}

						OrderByWithQueryString = string.Join(",", orderParams);
					}
				}
			}

			ApplyPaging((queryStringParameters.PageNumber - 1) * queryStringParameters.PageSize, queryStringParameters.PageSize);
		}

		public virtual void UndoPaging()
		{
			IsPagingEnabled = false;
		}

		protected virtual void ApplyGroupBy(Expression<Func<TEntity, object>> groupByExpression)
		{
			GroupBy = groupByExpression;
		}

	}
}
