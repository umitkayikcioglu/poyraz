using Poyraz.EntityFramework.Abstractions;
using System;
using System.Linq.Expressions;

namespace Poyraz.EntityFramework.Specifications
{
	public class ByRowGuidSpecification<T> : Specification<T> where T : IEntityWithExternalId
	{
		public ByRowGuidSpecification(Guid rowGuid) : base(x => x.RowGuid == rowGuid)
		{
		}
		public ByRowGuidSpecification(Guid rowGuid, params Expression<Func<T, object>>[] includes) : this(rowGuid)
		{
			Includes.AddRange(includes);
		}
	}
}
