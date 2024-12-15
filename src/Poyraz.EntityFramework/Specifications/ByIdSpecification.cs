using Poyraz.EntityFramework.Abstractions;

namespace Poyraz.EntityFramework.Specifications
{
	public class ByIdSpecification<T> : Specification<T> where T : IEntity
	{
		public ByIdSpecification(long id) : base(x => x.Id == id)
		{
		}
	}
}
