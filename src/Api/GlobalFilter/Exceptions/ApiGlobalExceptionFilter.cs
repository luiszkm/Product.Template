using FluentValidation;
using Product.Template.Kernel.Application.Exceptions;
using Product.Template.Kernel.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Product.Template.Api.GlobalFilter.Exceptions
{
    public class ApiGlobalExceptionFilter : IExceptionFilter
    {
        private readonly IHostEnvironment _env;

        public ApiGlobalExceptionFilter(IHostEnvironment env)
        {
            _env = env;
        }

        public void OnException(ExceptionContext context)
        {
            var details = new ProblemDetails();
            var exception = context.Exception;

            if (_env.IsDevelopment())
            {
                details.Extensions.Add("StackTrace", exception.StackTrace);
            }

            if (exception is NotFoundException)
            {
                details.Title = "Not Found";
                details.Status = StatusCodes.Status404NotFound;
                details.Type = "Not Found";
                details.Detail = exception!.Message;

            }
            else if (exception is DomainException)
            {
                details.Title = "One or more validation errors occurred.";
                details.Status = StatusCodes.Status422UnprocessableEntity;
                details.Type = "UnProcessableEntity";
                details.Detail = exception!.Message;

            }
            else if (exception is BusinessRuleException)
            {
                details.Title = "Business rule violation";
                details.Status = StatusCodes.Status400BadRequest;
                details.Type = "BusinessRuleViolation";
                details.Detail = exception.Message;
            }
            else if (exception is ValidationException validationException)
            {
                details.Title = "One or more validation errors occurred.";
                details.Status = StatusCodes.Status400BadRequest;
                details.Type = "ValidationError";
                details.Detail = "One or more validation errors occurred.";
                details.Extensions["errors"] = validationException.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray());
            }
            else if (exception is UnauthorizedAccessException)
            {
                details.Title = "Unauthorized";
                details.Status = StatusCodes.Status401Unauthorized;
                details.Type = "Unauthorized";
                details.Detail = exception.Message;
            }
            else
            {
                details.Title = "An unexpected error occurred";
                details.Status = StatusCodes.Status500InternalServerError;
                details.Type = "UnexpectedError";
                details.Detail = _env.IsDevelopment()
                    ? exception.Message
                    : "An unexpected error occurred.";
            }

            context.HttpContext.Response.StatusCode = (int)details.Status;
            context.Result = new ObjectResult(details);
        }
    }
}

