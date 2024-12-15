using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Poyraz.Helpers.Primitives
{
	public class Result
	{
		protected internal Result(bool isSuccess, Error error)
		{
			if (isSuccess && error != Error.None)
			{
				throw new InvalidOperationException();
			}

			if (!isSuccess && error == Error.None)
			{
				throw new InvalidOperationException();
			}

			IsSuccess = isSuccess;
			Errors = new Error[] { error };
		}

		protected internal Result(bool isSuccess, Error[] errors)
		{
			IsSuccess = isSuccess;
			Errors = errors;
		}

		public bool IsSuccess { get; }
		public bool IsFailure => !IsSuccess;
		public Error[] Errors { get; }

		public static Result Success() => new Result(true, Error.None);
		public static Result<TData> Success<TData>(TData data) => new Result<TData>(data, true, Error.None);

		public static Result Failure(Error error) => new Result(false, error);
		public static Result Failure(Error[] errors) => new Result(false, errors);

		public static Result<TData> Failure<TData>(Error error) => new Result<TData>(default, false, error);
		public static Result<TData> Failure<TData>(Error[] errors) => new Result<TData>(default, false, errors);

		public static Result<TData> Create<TData>(TData? data) => data is not null ? Success(data) : Failure<TData>(Error.NullValues);

		public static Result<T> Ensure<T>(T value, Func<T, bool> predicate, params Error[] errors)
		{
			return predicate(value) ? Success(value) : Failure<T>(errors);
		}
		public static Result<T> Ensure<T>(T value, params (Func<T, bool> predicate, Error error)[] functions)
		{
			var results = new List<Result<T>>();
			foreach ((Func<T, bool> predicate, Error error) in functions)
			{
				results.Add(Ensure(value, predicate, error));
			}
			return Combine(results.ToArray());
		}

		public static Result<T> Combine<T>(params Result<T>[] results)
		{
			if (results.Any(r => r.IsFailure))
			{
				return Failure<T>(results.SelectMany(r => r.Errors).Where(w => w != Error.None).Distinct().ToArray());
			}

			return Success(results[0].Data);
		}

		public string ErrorSerialize()
		{
			return JsonSerializer.Serialize(Errors);
		}

		public static Error[] ErrorDeserialize(string errorSerialized)
		{
			try
			{
				if (errorSerialized.StartsWith('['))
					return JsonSerializer.Deserialize<Error[]>(errorSerialized, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
				else
					return new Error[] { new Error(errorSerialized) };
			}
			catch (Exception)
			{
				return new Error[] { new Error(errorSerialized) };
			}
		}
	}
}
