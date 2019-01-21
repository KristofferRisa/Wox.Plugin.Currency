using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wox.Plugin.Currency.Models
{
    public class Currency
    {
        [JsonProperty("amount")]
        public float Amount { get; set; }
        [JsonProperty("_base")]
        public string Base { get; set; }
        [JsonProperty("date")]
        public string Date { get; set; }
        [JsonProperty("rates")]
        public string Rate { get; set; }
    }

    public class Rates
    {
        public float NOK { get; set; }
    }

}
