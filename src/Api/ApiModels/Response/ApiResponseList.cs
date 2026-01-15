using Product.Template.Kernel.Application.Data;
using Product.Template.Kernel.Domain.SeedWorks;

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
            ) : base(paginatedListOutput.Data)
        {
            Meta = new ApiResponseListMeta(
                paginatedListOutput.PageNumber,
                paginatedListOutput.PageNumber,
                paginatedListOutput.TotalCount);
        }
        public ApiResponseListMeta Meta { get; private set; }
    }
}

