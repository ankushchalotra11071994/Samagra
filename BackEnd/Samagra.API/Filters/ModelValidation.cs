using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace samagra.API.Filters
{
    internal class ModelValidation : IActionFilter 
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
             if (!context.ModelState.IsValid)
        {
            context.Result = new BadRequestObjectResult(
                context.ModelState);
        }
            
        }

        // Action के बाद कुछ नहीं करना — सिर्फ़ पहले validation
        public void OnActionExecuted(ActionExecutedContext context) { }

        
    }
}