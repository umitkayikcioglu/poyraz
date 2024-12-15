namespace Poyraz.EntityFramework.Specifications
{
	public class QueryStringParameters
	{
		private int _pageSize = SpecificationConstants.DefaultPageSize;

		public int PageNumber { get; set; } = 1;
		public int PageSize
		{
			get => _pageSize;
			set => _pageSize = (value > SpecificationConstants.MaxPageSize) ? SpecificationConstants.MaxPageSize : value;
		}
		public string OrderBy { get; set; }
		public string Search { get; set; }
	}
}
