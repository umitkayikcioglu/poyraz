using System;
using System.Linq;
using System.Text.Json.Serialization;

namespace Poyraz.Helpers.Primitives
{
	public class Error : IEquatable<Error>
	{
		public static readonly Error None = new(string.Empty, string.Empty);
		public static readonly Error NullValues = new("Error.NullValue", "The specified result value is null");

		public Error(string code)
		{
			Code = code;
		}

		public Error(string code, params object[] values)
		{
			Code = code;

			if (values?.Length > 0)
				Values = values?.Select(x => x.ToString()).ToArray();
		}

		[JsonConstructor]
		public Error(string code, string[] values)
		{
			Code = code;
			Values = values;
		}
		public string Code { get; }

		public string[] Values { get; }

		public static implicit operator string(Error error) => error.Code;
		public static bool operator ==(Error? a, Error? b)
		{
			if (a is null && b is null)
				return true;

			if (a is null || b is null)
				return false;

			return a.Equals(b);
		}

		public static bool operator !=(Error? a, Error? b)
		{
			if (a is null && b is null)
				return false;

			if (a is null || b is null)
				return true;

			return !a.Equals(b);
		}

		public bool Equals(Error other)
		{
			return Code == other.Code;
		}
	}
}
