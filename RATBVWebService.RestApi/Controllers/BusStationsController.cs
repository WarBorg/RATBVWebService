﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using RATBVData.Models.Models;
using RATBVWebService.Data.Interfaces;

namespace RATBVWebService.RestApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BusStationsController : Controller
    {
        #region Dependencies

        private readonly IBusDataService _busDataService;

        #endregion

        #region Constructor

        public BusStationsController(IBusDataService busRepository)
        {
            _busDataService = busRepository;
        }

        #endregion

        #region GET API Methods

        // GET: api/busstations/valid_line_number_link
        [HttpGet("{lineNumberLink}")]
        public async Task<ActionResult<IEnumerable<BusStationModel>>> Get(string lineNumberLink)
        {
            try
            {
                return await _busDataService.GetBusStationsAsync(lineNumberLink);
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

        #endregion
    }
}