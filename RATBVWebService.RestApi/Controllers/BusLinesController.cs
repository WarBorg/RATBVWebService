using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using RATBVData.Models.Models;
using RATBVWebService.Data.Interfaces;

namespace RATBVWebService.RestApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BusLinesController : Controller
    {
        #region Dependencies

        private readonly IBusDataService _busDataService;

        #endregion

        #region Constructor

        public BusLinesController(IBusDataService busRepository)
        {
            _busDataService = busRepository;
        }

        #endregion

        #region GET API Methods

        // GET: api/buslines
        [HttpGet]
        public async Task<IEnumerable<BusLineModel>> Get()
        {
            return await _busDataService.GetBusLinesAsync();
        }

        // GET api/buslines/valid_line_number
        [HttpGet("{number}")]
        public async Task<ActionResult<BusLineModel>> Get(string number)
        {
            var busLines = await _busDataService.GetBusLinesAsync();
            var busLine = busLines.FirstOrDefault(b => b.Name == $"Linia {number}");
            
            if (busLine == null)
            {
                var error = new ErrorResponseModel
                {
                    ErrorType = "Not Found",
                    ErrorMessage = $"Line {number} not available"
                };

                return NotFound(error);
            }

            return busLine;
        }

        #endregion
    }
}