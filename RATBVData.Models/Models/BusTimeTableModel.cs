using Newtonsoft.Json;
using SQLite;
using SQLiteNetExtensions.Attributes;

namespace RATBVData.Models.Models
{
    public class BusTimeTableModel
    {
        [JsonProperty("id")]
        [PrimaryKey, AutoIncrement]
        public int? Id { get; set; } // without nullable on Id, InsertOrReplace will not autoincrement the Id

        [JsonProperty("busStationId")]
        [ForeignKey(typeof(BusStationModel))]
        public int BusStationId { get; set; }

        [JsonProperty("hour")]
        public string Hour { get; set; }

        [JsonProperty("minutes")]
        public string Minutes { get; set; }

        [JsonProperty("timeOfWeek")]
        public string TimeOfWeek { get; set; }

        [JsonProperty("lastUpdateDate")]
        public string LastUpdateDate { get; set; }
    }
}