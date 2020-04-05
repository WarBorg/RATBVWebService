using System.Collections.Generic;
using System.Threading.Tasks;
using RATBVData.Models.Models;

namespace RATBVWebService.Data.Interfaces
{
    public interface IBusDataService
    {
        Task<List<BusLineModel>> GetBusLinesAsync();
        Task<List<BusStationModel>> GetBusStationsAsync(string lineNumberLink);
        Task<List<BusTimeTableModel>> GetBusTimeTableAsync(string schedualLink);
    }
}