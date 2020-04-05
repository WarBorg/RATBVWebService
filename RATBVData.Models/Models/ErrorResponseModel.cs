using Newtonsoft.Json;

namespace RATBVData.Models.Models
{
    public class ErrorResponseModel
    {
        [JsonProperty("errorType")]
        public string ErrorType { get; set; }

        [JsonProperty("errorMessage")]
        public string ErrorMessage { get; set; }
    }
}