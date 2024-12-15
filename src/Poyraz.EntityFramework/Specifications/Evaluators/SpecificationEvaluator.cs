using Microsoft.EntityFrameworkCore;
using Poyraz.EntityFramework.Abstractions;
using System.Linq;

namespace Poyraz.EntityFramework.Specifications.Evaluators
{
	internal static class SpecificationEvaluator<TEntity> where TEntity : class, IEntity
	{
		internal static IQueryable<TEntity> GetQuery(IQueryable<TEntity> inputQuery, ISpecification<TEntity> specification)
		{
			var query = inputQuery;

			// modify the IQueryable using the specification's criteria expression
			if (specification.Criteria != null)
			{
				query = query.Where(specification.Criteria);
			}

			// Includes all expression-based includes
			query = specification.Includes.Aggregate(query,
									(current, include) => current.Include(include));

			query = specification.IncludeStrings.Aggregate(query,
									(current, include) => current.Include(include));

			// Apply ordering if expressions are set
			if (specification.OrderBy != null)
			{
				query = query.OrderBy(specification.OrderBy);
			}
			else if (specification.OrderByDescending != null)
			{
				query = query.OrderByDescending(specification.OrderByDescending);
			}

			if (!string.IsNullOrEmpty(specification.OrderByWithQueryString))
			{
				query = query.ApplySort(specification.OrderByWithQueryString);
			}

			if (specification.GroupBy != null)
			{
				query = query.GroupBy(specification.GroupBy).SelectMany(x => x);
			}

			// Apply paging if enabled
			if (specification.IsPagingEnabled)
			{
				query = query.Skip(specification.Skip)
							 .Take(specification.Take);
			}

			if (specification.IsSplitQuery)
			{
				query = query.AsSplitQuery();
			}

			return query;
		}
	}
}
