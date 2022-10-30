using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using RATBVData.Models.Models;
using RATBVWebService.Data.Interfaces;
using System;

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
        public async Task<ActionResult<IEnumerable<BusLineModel>>> Get()
        {
            try
            {
                return await _busDataService.GetBusLinesAsync();
            }
            catch (Exception ex)
            {
                var error = new ErrorResponseModel
                    (
                        errorType: "Not Found",
                        errorMessage: ex.Message
                    );

                return StatusCode(500, error);
            }
        }

        // GET api/buslines/valid_line_number
        [HttpGet("{number}")]
        public async Task<ActionResult<BusLineModel>> Get(string number)
        {
            try
            {
                var busLines = await _busDataService.GetBusLinesAsync();
                var busLine = busLines.FirstOrDefault(b => b.Name == $"Linia {number}");

                if (busLine is null)
                {
                    var error = new ErrorResponseModel
                        (
                            errorType: "Not Found",
                            errorMessage: $"Line {number} not available"
                        );

                    return NotFound(error);
                }

                return busLine;
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        #endregion
    }
}