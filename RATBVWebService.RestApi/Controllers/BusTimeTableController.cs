using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using RATBVData.Models.Models;
using RATBVWebService.Data.Interfaces;

namespace RATBVWebService.RestApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BusTimeTableController : Controller
    {
        #region Dependencies

        private IBusDataService _busDataService;

        #endregion

        #region Constructor

        public BusTimeTableController(IBusDataService busRepository)
        {
            _busDataService = busRepository;
        }

        #endregion

        #region GET API Methods

        // GET: api/values/someString
        [HttpGet("{scheduleLink}")]
        public async Task<IEnumerable<BusTimeTableModel>> Get(string scheduleLink)
        {
            return await _busDataService.GetBusTimeTableAsync(scheduleLink);
        }

        #endregion
    }
}