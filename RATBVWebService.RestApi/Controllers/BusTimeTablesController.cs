using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using RATBVData.Models.Models;
using RATBVWebService.Data.Interfaces;

namespace RATBVWebService.RestApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BusTimeTablesController : Controller
    {
        #region Dependencies

        private readonly IBusDataService _busDataService;

        #endregion

        #region Constructor

        public BusTimeTablesController(IBusDataService busRepository)
        {
            _busDataService = busRepository;
        }

        #endregion

        #region GET API Methods

        // GET: api/bustimetables/valid_shedule_link
        [HttpGet("{scheduleLink}")]
        public async Task<IEnumerable<BusTimeTableModel>> Get(string scheduleLink)
        {
            return await _busDataService.GetBusTimeTableAsync(scheduleLink);
        }

        #endregion
    }
}