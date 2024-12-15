using System;

namespace Poyraz.EntityFramework.Abstractions
{
	public interface IEntity
	{
		long Id { get; }
	}

	public interface IEntityWithExternalId : IEntity
	{
		Guid RowGuid { get; }
	}

	public interface ISimpleAuditable
	{
		DateTime CreatedAt { get; }
	}
}
