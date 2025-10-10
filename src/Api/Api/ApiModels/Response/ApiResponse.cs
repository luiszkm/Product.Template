namespace Product.Template.Api.ApiModels.Response
{
    public class ApiResponse<TData>
    {
        public ApiResponse(TData data)
            => Data = data;
        public TData Data { get; private set; }
        public string Error { get; set; } = string.Empty;
        public int StatusCode { get; set; }
        public bool Success => string.IsNullOrEmpty(Error);
    }
}

