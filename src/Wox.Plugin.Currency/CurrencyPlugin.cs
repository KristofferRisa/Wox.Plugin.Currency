using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using Newtonsoft.Json;
using Wox.Plugin.Currency.Models;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using Wox.Infrastructure.Logger;
using NLog;

namespace Wox.Plugin.Currency
{
    public class CurrencyPlugin : IPlugin
    {
        private PluginInitContext _context;

        private static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();
        private string LocalISOSymbol => RegionInfo.CurrentRegion.CurrencyEnglishName;

        private readonly Dictionary<SearchParameters, Models.Currency> _cache;
        
        private string _toCurrency = "";

        private string _fromCurrency = "";

        private decimal _money = new decimal();

        public CurrencyPlugin()
        {
            Logger.WoxInfo("Currency plugin loaded.");
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
                Logger.WoxInfo($"Converting {_money} from {_fromCurrency} to {_toCurrency}");

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
                Logger.WoxInfo($"Converted {_money} {_fromCurrency} to {convertedValue} {_toCurrency} with rate {rate}");
                return new List<Result>()
                {   
                    new Result
                    {
                        Title = $"{_money} {_fromCurrency} = {convertedValue} {_toCurrency}",
                        IcoPath = "Images/bank.png",
                        SubTitle = $" https://frankfurter.app (Last updated {currency.date})",
                        Action = c =>
                        {
                            try
                            {
                                Clipboard.SetText(convertedValue);
                                return true;
                            }
                            catch (ExternalException e)
                            {
                                Logger.WoxInfo($"Copy failed, please try later: {e.Message}");
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
            Logger.WoxInfo("Loading rates...");
            var url = $"https://frankfurter.app/latest?base={searchParameters.BaseIso}&symbols={searchParameters.ToIso}";

            if (_cache.ContainsKey(searchParameters))
            {
                if (Convert.ToDateTime(_cache[searchParameters].date) == DateTime.Today)
                {
                    Logger.WoxInfo("Loading rates from cache");
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
                Logger.WoxInfo("Reading rates from API");
                var responsestring = new StreamReader(response.GetResponseStream()).ReadToEnd();

                var currency =  JsonConvert.DeserializeObject<Models.Currency>(responsestring);

                _cache.Add(searchParameters,currency);
                return currency;
            }
        }

    }
}
