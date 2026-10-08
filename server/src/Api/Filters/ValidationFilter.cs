using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Api.Filters;

public class ValidationFilter : IAsyncActionFilter
{
    private readonly IServiceProvider _serviceProvider;

    public ValidationFilter(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // iterates through all action arguments and based validation on the request type
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument == null) continue;
            
            // gets the request type of incoming object
            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            // checks if the incoming object has a validator
            if (_serviceProvider.GetService(validatorType) is IValidator validator)
            {   
                // wraps the incoming object in a ValidationContext before validating
                var validationContext = new ValidationContext<object>(argument);
                // validates the incoming object
                var validationResult = await validator.ValidateAsync(validationContext, context.HttpContext.RequestAborted);
                // if the validation fails, return a 400 Bad Request response
                if (!validationResult.IsValid)
                {
                    var firstErrorMessage = validationResult.Errors.FirstOrDefault()?.ErrorMessage ?? "Validation failed.";

                    context.Result = new BadRequestObjectResult(new
                    {
                        statusCode = StatusCodes.Status400BadRequest,
                        message = firstErrorMessage,
                        timestamp = DateTime.UtcNow
                    });

                    return;
                }
            }
        }

        await next();
    }
}

