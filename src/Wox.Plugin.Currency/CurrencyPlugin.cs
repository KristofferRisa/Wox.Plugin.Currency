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
        private PluginInitContext _context;
        private string LocalISOSymbol => RegionInfo.CurrentRegion.ISOCurrencySymbol;
        private readonly Dictionary<SearchParameters,Models.Currency> _cache;  
        private readonly string oneWaycheckPattern = @"^(\d+(\.\d{1,2})?)?\s([A-Za-z]{3})$"; //10 usd
        private readonly string twoWaycheckPattern = @"(\d+(\.\d{1,2})?)?\s([A-Za-z]{3})\s([i][n])\s([A-Za-z]{3})"; // 10 usd in nok
        private string _toCurrency = "";
        private string _fromCurrency = "";
        private decimal _money = new decimal();

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
                if (Regex.IsMatch(query.Search, oneWaycheckPattern))
                {
                    if (query.RawQuery != null && query.RawQuery.Split(' ').Length == 2) // 123 usd
                    {
                         _money = Convert.ToDecimal(query.FirstSearch);
                        _toCurrency = LocalISOSymbol;
                        _fromCurrency = query.SecondSearch.ToUpper();
                        //check ISO symbols
                        if (!Enum.IsDefined(typeof (RateList), query.SecondSearch.ToUpper())) return new List<Result>();
                        results = LoadCurrency(results);
                    }
                    
                }
                else if (Regex.IsMatch(query.Search, twoWaycheckPattern))
                {
                    if (query.RawQuery != null && query.RawQuery.Split(' ').Length == 4) //123 usd in nok
                    {
                        _toCurrency = query.RawQuery.Split(' ')[3].ToUpper();
                        _fromCurrency = query.SecondSearch.ToUpper();
                        _money = Convert.ToDecimal(query.FirstSearch);
                        //check ISO symbols
                        if (!Enum.IsDefined(typeof(RateList), _fromCurrency)
                            && !Enum.IsDefined(typeof(RateList), query.RawQuery.Split(' ')[3].ToUpper()))
                            return results;
                    results = LoadCurrency(results);
                    }
                }
                else if (query.Search == "currency")
                {
                    var ratelist = Enum.GetValues(typeof(RateList));
                    var rates = "";
                    foreach (var rate in ratelist)
                    {
                        if (rates.Length > 1)
                        {
                            rates += $"Avaliable rates are {rate} ";
                        }
                        else
                        {
                            rates += $"{rate} ,";
                        }
                    }
                    results.Add(new Result
                    {
                        Title = rates,
                        IcoPath = "Images/bank.png",
                        SubTitle = $"Source: fixer.io"
                    });
                }

                return results;
            }
            catch (Exception e)
            {
                //Add logging
                Console.WriteLine(e.Message);
                return new List<Result>();
            }

        }

        private List<Result> LoadCurrency(List<Result> results)
        {
            var currency = GetCurrency(new SearchParameters() {BaseIso = _fromCurrency, ToIso = _toCurrency});
            var rate = currency.GetRate(_toCurrency);
            results.Add(new Result
            {
                Title = $"{_money.ToString(".00")} {_fromCurrency} = {(_money*rate).ToString("C")} {_toCurrency}",
                IcoPath = "Images/bank.png",
                SubTitle = $"Source: https://frankfurter.app (Last updated {currency.date})"
            });
            return results;
        }

        public void Init(PluginInitContext context)
        {
            _context = context;
        }

        private Models.Currency GetCurrency(SearchParameters searchParameters)
        {
            var url = $"https://frankfurter.app/latest?base={searchParameters.BaseIso}&symbols={searchParameters.ToIso}";

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

        private bool CheckRegExOnQuery(string query)
        {
            return false;
        }
    }
}
