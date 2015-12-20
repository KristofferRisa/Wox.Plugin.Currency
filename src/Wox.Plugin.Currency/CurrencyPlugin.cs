using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Wox.Plugin.Currency.Models;

namespace Wox.Plugin.Currency
{
    public class CurrencyPlugin : IPlugin
    {
        #region private fields
        private PluginInitContext _context;
        private string localISOSymbol => RegionInfo.CurrentRegion.ISOCurrencySymbol;
        private readonly Dictionary<SearchParameters,Models.Currency> _cache;        
        #endregion

        public CurrencyPlugin()
        {
            if (_cache == null)
            {
                _cache = new Dictionary<SearchParameters, Models.Currency>();
            }
        }

        public List<Result> Query(Query query)
        {
            var results = new List<Result>();
            try
            {
                var toCurrency = "";
                var fromCurrency = "";
                var money = new decimal();
                var pattern = "";
                if (query.RawQuery != null && query.RawQuery.Split(' ').Length == 2) // 123 usd
                {
                    //inputs
                    pattern = @"(\d+(\.\d{1,2})?)?\s([A-Za-z]{3})$";
                    money = Convert.ToDecimal(query.FirstSearch);
                    toCurrency = localISOSymbol;
                    fromCurrency = query.SecondSearch.ToUpper(); 
                    //check ISO symbols
                    if (!Enum.IsDefined(typeof(RateList), query.SecondSearch.ToUpper())) return new List<Result>();
                }
                else if (query.RawQuery != null && query.RawQuery.Split(' ').Length == 4) //123 usd in nok
                {
                    //inputs
                    toCurrency = query.RawQuery.Split(' ')[3].ToUpper();
                    pattern = @"(\d+(\.\d{1,2})?)?\s([A-Za-z]{3})\s([i][n])\s([A-Za-z]{3})";
                    fromCurrency = query.SecondSearch.ToUpper();
                    money = Convert.ToDecimal(query.FirstSearch);
                    //check ISO symbols
                    if (!Enum.IsDefined(typeof(RateList), fromCurrency)
                            && !Enum.IsDefined(typeof(RateList), query.RawQuery.Split(' ')[3].ToUpper())) return results;
                }
                else
                {
                    return new List<Result>();   
                }
                if (Regex.IsMatch(query.RawQuery, pattern))
                {
                    var currency = GetCurrency(new SearchParameters() {BaseIso = fromCurrency, ToIso = toCurrency});
                    var rate = currency.GetRate(toCurrency);
                    results.Add(new Result
                    {
                        Title = $"{money.ToString(".00")} {fromCurrency} = {(money * rate).ToString("C")} {toCurrency}",
                        IcoPath = "Images/bank.png",
                        SubTitle = $"Source: fixer.io (Last updated {currency.date})"
                    });
                }
                return results;
            }
            catch (Exception)
            {
                return new List<Result>();
            }
        }
        public void Init(PluginInitContext context)
        {
            _context = context;
        }
        #region helpers
        private Models.Currency GetCurrency(SearchParameters searchParameters)
        {
            var url = $"http://api.fixer.io/latest?base={searchParameters.BaseIso}&symbols={searchParameters.ToIso}";

            if (_cache.ContainsKey(searchParameters))
            {
                if (Convert.ToDateTime(_cache[searchParameters].date) == DateTime.Today)
                {
                    return _cache[searchParameters];
                }
            }

            if (searchParameters.OptionalIsos != null && searchParameters.OptionalIsos.Any())
            {
                url = searchParameters.OptionalIsos.Aggregate(url, (current, optionalIso) => current + $",{optionalIso}");
            }

            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            var response = (HttpWebResponse)request.GetResponse();
            using (new StreamReader(response.GetResponseStream()))
            {
                var responsestring = new StreamReader(response.GetResponseStream()).ReadToEnd();

                var currency =  JsonConvert.DeserializeObject<Models.Currency>(responsestring);

                _cache.Add(searchParameters,currency);
                return currency;
            }
        }
        #endregion
    }
}
