namespace Poyraz.Helpers.Primitives
{
	public class ResultList<TItem>
	{
		public ResultList(TItem[] items, int totalResults)
		{
			Items = items;
			ResultsPerPage = items.Length;
			TotalResults = totalResults;
		}

		public ResultList(TItem[] items) : this(items, items.Length)
		{
		}

		public TItem[] Items { get; }

		public int TotalResults { get; set; }
		public int ResultsPerPage { get; set; }

		public static implicit operator ResultList<TItem>(TItem[] data) => new ResultList<TItem>(data, data.Length);
	}
}
