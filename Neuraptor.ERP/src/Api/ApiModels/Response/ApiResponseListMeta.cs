namespace Neuraptor.ERP.Api.ApiModels.Response
{
    public class ApiResponseListMeta
    {
        public ApiResponseListMeta(
            int currentPage,
            int perPage,
            int totalCount)
        {
            TotalCount = totalCount;
            PerPage = perPage;
            CurrentPage = currentPage;
        }

        public int TotalCount { get; set; }
        public int PerPage { get; set; }
        public int CurrentPage { get; set; }

    }
}

