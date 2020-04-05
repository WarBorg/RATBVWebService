using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using RATBVData.Models.Models;
using RATBVWebService.Data.Interfaces;

namespace RATBVWebService.RestApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BusStationController : Controller
    {
        #region Dependencies

        private IBusDataService _busDataService;

        #endregion

        #region Constructor

        public BusStationController(IBusDataService busRepository)
        {
            _busDataService = busRepository;
        }

        #endregion

        #region GET API Methods

        // GET: api/values
        [HttpGet("{lineNumberLink}")]
        public async Task<IEnumerable<BusStationModel>> Get(string lineNumberLink)
        {
            return await _busDataService.GetBusStationsAsync(lineNumberLink);
        }

        #endregion
    }
}