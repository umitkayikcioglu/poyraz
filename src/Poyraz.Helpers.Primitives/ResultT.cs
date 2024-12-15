using System;

namespace Poyraz.Helpers.Primitives
{
	public interface IResultWithData
	{
		object GetData();
	}

	public class Result<TData> : Result, IResultWithData
	{
		private TData _data;

		protected internal Result(TData data, bool isSuccess, Error error)
			: base(isSuccess, error)
		{
			_data = data;
		}

		protected internal Result(TData data, bool isSuccess, Error[] errors)
			: base(isSuccess, errors)
		{
			_data = data;
		}

		public TData Data => IsSuccess
			? _data!
			: throw new InvalidOperationException("The value of a failure result can not be accessed.");

		public object GetData()
		{
			return _data;
		}

		public static implicit operator Result<TData>(TData? data) => Create(data);

	}
}
