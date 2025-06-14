namespace EMSApp.Api;

using EMSApp.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

public class ApiExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is DomainException dex)
        {
            var problem = new { message = dex.Message };
            context.Result = new BadRequestObjectResult(problem);
            context.ExceptionHandled = true;
        }

        else if (context.Exception is RepositoryException rex)
        {
            var problem = new { message = rex.Message };
            context.Result = new BadRequestObjectResult(problem);
            context.ExceptionHandled = true;
        }
    }
}
