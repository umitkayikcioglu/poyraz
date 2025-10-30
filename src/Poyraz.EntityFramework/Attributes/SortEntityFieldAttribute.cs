using Poyraz.EntityFramework.Abstractions;
using System;

namespace Poyraz.EntityFramework.Attributes
{
	[AttributeUsage(AttributeTargets.Property)]
	public abstract class SortEntityFieldAttribute : Attribute
	{
		public string PropertyName { get; set; }
		public string EntityName { get; set; }
		public string SortName => $"{EntityName}.{PropertyName}";
		public SortEntityFieldAttribute(string entityName, string propertyName)
		{
			PropertyName = propertyName;
			EntityName = entityName;
		}
	}

	public sealed class SortEntityFieldAttribute<TEntity> : SortEntityFieldAttribute
		where TEntity : IEntity
	{
		public SortEntityFieldAttribute(string propertyName) : base(typeof(TEntity).Name, propertyName)
		{
		}
	}

	[AttributeUsage(AttributeTargets.Property)]
	public sealed class NonSearchAttribute : Attribute
	{

	}
}
