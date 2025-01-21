using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Poyraz.EntityFramework.Abstractions
{
	public interface ISpecification<T>
	{
		bool IsSplitQuery { get; }
		Expression<Func<T, bool>> Criteria { get; }
		Dictionary<string, string> SearchFields { get; }
		List<Expression<Func<T, object>>> Includes { get; }
		List<string> IncludeStrings { get; }
		Expression<Func<T, object>> OrderBy { get; }
		Expression<Func<T, object>> OrderByDescending { get; }
		string OrderByWithQueryString { get; }
		Expression<Func<T, object>> GroupBy { get; }

		int Take { get; }
		int Skip { get; }
		bool IsPagingEnabled { get; }

		void UndoPaging();
	}
}
