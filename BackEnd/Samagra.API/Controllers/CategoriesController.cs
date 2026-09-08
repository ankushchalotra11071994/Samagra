using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Samagra.API.Controllers
{
    [Route("[controller]")]
    public class CategoriesController : Controller
    {
        private readonly ILogger<CategoriesController> _logger;

        public CategoriesController(ILogger<CategoriesController> logger)
        {
            _logger = logger;
        }
         

    }
}