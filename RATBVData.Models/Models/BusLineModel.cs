using System.Collections.Generic;
using Newtonsoft.Json;
using SQLite;
using SQLiteNetExtensions.Attributes;

namespace RATBVData.Models.Models
{
    public class BusLineModel
    {
        [JsonProperty("id")]
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("route")]
        public string Route { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        // TODO check this property if we still need it
        [JsonProperty("color")]
        public string Color { get; set; }

        [JsonProperty("linkNormalWay")]
        public string LinkNormalWay { get; set; }

        [JsonProperty("linkReverseWay")]
        public string LinkReverseWay { get; set; }

        [JsonIgnore]
        public string LastUpdateDate { get; set; }

        [JsonIgnore]
        [OneToMany(CascadeOperations = CascadeOperation.All)]
        public List<BusStationModel> BusStations { get; set; }
    }
}