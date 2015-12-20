using System;
using Newtonsoft.Json;

namespace Wox.Plugin.Currency.Models
{
    public class Currency
    {
        [JsonProperty]
        public string @base { get; set; }
        [JsonProperty]
        public string date { get; set; }
        [JsonProperty]
        public Rates rates { get; set; }

        public decimal GetRate(string currency)
        {
            return Convert.ToDecimal(this.rates.GetType().GetProperty(currency).GetValue(rates, null));
        }
    }
    
}
