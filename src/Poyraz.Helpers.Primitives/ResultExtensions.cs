using System;

namespace Poyraz.Helpers.Primitives
{
	public static class ResultExtensions
	{
		public static Result<TData> Ensure<TData>(this Result<TData> result, Func<TData, bool> predicate, Error error)
		{
			if (result.IsFailure)
			{
				return result;
			}

			return predicate(result.Data) ?
				result :
				Result.Failure<TData>(error);
		}

		public static Result<TOut> Map<TIn, TOut>(this Result<TIn> result, Func<TIn, TOut> mappingFunc)
		{
			return result.IsSuccess ?
						Result.Success(mappingFunc(result.Data)) :
						Result.Failure<TOut>(result.Errors);
		}


		public static Result OnSuccess(this Result result, Func<Result> func)
		{
			if (result.IsFailure)
				return result;

			return func();
		}

		public static Result OnSuccess(this Result result, Action action)
		{
			if (result.IsFailure)
				return result;

			action();

			return Result.Success();
		}

		public static Result OnSuccess<T>(this Result<T> result, Action<T> action)
		{
			if (result.IsFailure)
				return result;

			action(result.Data);

			return Result.Success();
		}

		public static Result<T> OnSuccess<T>(this Result result, Func<T> func)
		{
			if (result.IsFailure)
				return Result.Failure<T>(result.Errors);

			return Result.Success(func());
		}

		public static Result OnSuccess<T>(this Result<T> result, Func<T, Result> func)
		{
			if (result.IsFailure)
				return Result.Failure<T>(result.Errors);

			return func(result.Data);
		}

		public static Result<T> OnSuccess<T>(this Result result, Func<Result<T>> func)
		{
			if (result.IsFailure)
				return Result.Failure<T>(result.Errors);

			return func();
		}

		public static Result<TOut> OnSuccess<TIn, TOut>(this Result<TIn> result, Func<Result<TIn>, Result<TOut>> func)
		{
			if (result.IsFailure)
				return Result.Failure<TOut>(result.Errors);

			return func(result.Data);
		}
		public static Result OnFailure(this Result result, Action action)
		{
			if (result.IsFailure)
			{
				action();
			}

			return result;
		}

		public static Result OnBoth(this Result result, Action<Result> action)
		{
			action(result);

			return result;
		}

		public static T OnBoth<T>(this Result result, Func<Result, T> func)
		{
			return func(result);
		}

		public static TOut OnBoth<TIn, TOut>(this Result<TIn> result, Func<Result<TIn>, TOut> func)
		{
			return func(result);
		}

		public static Result<TOut> OnBoth<TIn, TOut>(this Result<TIn> result, Func<Result<TIn>, Result<TOut>> func)
		{
			if (result.IsFailure)
				return Result.Failure<TOut>(result.Errors);

			return func(result);
		}

		public static Result GetResultWithoutData(this Result result)
		{
			return result.OnBoth<Result>(o => Result.Ensure(o, (o) => o.IsSuccess, o.Errors));
		}
	}
}
