using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.Api
{
    [ApiController]
    [Route("data")]
    public class RessourceController : BaseController
    {
        private readonly IDataService _dataService;

        public RessourceController(IDataService dataService)
        {
            _dataService = dataService;
        }

        [HttpGet]
        public DataTable Get()
        {
            return _dataService.GetData();
        }

    }
}
