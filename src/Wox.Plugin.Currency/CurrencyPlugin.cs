using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Wox.Plugin.Currency.Models;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Wox.Plugin.Currency
{
    public class CurrencyPlugin : IPlugin
    {
        private PluginInitContext _context;

        private string LocalISOSymbol => RegionInfo.CurrentRegion.ISOCurrencySymbol;

        private readonly Dictionary<SearchParameters, Models.Currency> _cache;
        
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

        public void Init(PluginInitContext context)
        {
            _context = context;
        }

        public List<Result> Query(Query query)
        {
            try
            {
                query
                    .IsCurrencyConversion()
                    .IsNotNull();

                _money = Convert.ToDecimal(query.FirstSearch);
                _fromCurrency = query.SecondSearch.ToUpper();
                _toCurrency = query.RawQuery.Split(' ')[3].ToUpper() ?? LocalISOSymbol;

                if (!Enum.IsDefined(
                        typeof(RateList), 
                        query.RawQuery.Split(' ')[3].ToUpper() 
                            ?? query.SecondSearch.ToUpper()))
                    return new List<Result>();

                var searchParameters = new SearchParameters()
                {
                    BaseIso = _fromCurrency,
                    ToIso = _toCurrency
                };
                var currency = GetCurrencyFromApi(searchParameters);
                var rate = currency.GetRate(_toCurrency);
                var convertedValue = ((double)(_money * rate)).ToString();
                return new List<Result>()
                {   
                    new Result
                    {
                        Title = $"{_money} {_fromCurrency} = {convertedValue} {_toCurrency}",
                        IcoPath = "Images/bank.png",
                        SubTitle = $"Enter to copy. Source: https://frankfurter.app (Updated {currency.date})",
                        Action = c =>
                        {
                            try
                            {
                                Clipboard.SetText(convertedValue);
                                return true;
                            }
                            catch (ExternalException)
                            {
                                MessageBox.Show("Copy failed, please try later");
                                return false;
                            }
                        }
                    }
                };

            }
            catch 
            {
                return new List<Result>();
            }

        }
             
        private Models.Currency GetCurrencyFromApi(SearchParameters searchParameters)
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

    }
}
