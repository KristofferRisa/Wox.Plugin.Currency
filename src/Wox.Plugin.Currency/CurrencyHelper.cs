using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Wox.Plugin.Currency
{
    public static class CurrencyHelper
    {
        private const string Endpoint = @"https://api.fixer.io/latest";

        private static readonly Dictionary<CurrencyPair, double> _cache;
        private static readonly WebClient _webClient; 


        static CurrencyHelper()
        {
            _cache = new Dictionary<CurrencyPair, double>();

            _webClient = new WebClient();
            _webClient.Headers.Add("content-type", "application/json");
        }

        public static double Convert(string from, string to, double value)
        {
            var pair = new CurrencyPair(from, to);

            if (!_cache.ContainsKey(pair))
            {
                _cache.Add(pair, GetRate(from, to));
            }

            var rate = _cache[pair];
            return rate * value;
        }

        private static double GetRate(string from, string to)
        {
            var endpoint = $"{Endpoint}?base={from.ToUpper()}&symbols={to.ToUpper()}";
            var result = _webClient.DownloadString(endpoint);
            var rate = JsonConvert.DeserializeObject<CurrencyRate>(result);

            return rate.Rates.FirstOrDefault().Value;
        }
    }

    #region Exchange currencies stuff
    internal struct CurrencyPair
    {
        public CurrencyPair(string from, string to)
        {
            From = from;
            To = to;
        }

        public string From { get; set; }
        public string To { get; set; }
    }

    internal class CurrencyRate
    {
        [JsonProperty(PropertyName = "rates")]
        public Dictionary<string, double> Rates { get; set; }
    }
    #endregion
}
