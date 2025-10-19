using Product.Template.Kernel.Application.Data;

namespace Product.Template.Api.ApiModels.Response
{

    public class ApiResponseList<TItemData> : ApiResponse<IReadOnlyList<TItemData>>
    {
        public ApiResponseList(
            int currentPage,
            int perPage,
            int total,
            IReadOnlyList<TItemData> data) : base(data)
        {
            Meta = new(
                currentPage,
                perPage,
                total);
        }
        public ApiResponseList(
            PaginatedListOutput<TItemData> paginatedListOutput
            ) : base(paginatedListOutput.Items)
        {
            Meta = new ApiResponseListMeta(
                paginatedListOutput.Page,
                paginatedListOutput.PerPage,
                paginatedListOutput.TotalCount);
        }
        public ApiResponseListMeta Meta { get; private set; }
    }
}

