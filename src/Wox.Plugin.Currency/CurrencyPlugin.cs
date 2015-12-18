using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Wox.Plugin.Currency.Models;

namespace Wox.Plugin.Currency
{
    public class CurrencyPlugin : IPlugin, IPluginI18n
    {
        #region private fields
        private PluginInitContext _context;
        private Models.Currency _currency;
        private string localISOSymbol => RegionInfo.CurrentRegion.ISOCurrencySymbol;
        #endregion

        public List<Result> Query(Query query)
        {
            var results = new List<Result>();
            try
            {
                var toCurrency = "";
                decimal result = 0;
                if (query.RawQuery != null && query.RawQuery.Split(' ').Length == 2) // 123 usd
                {
                    var pattern = @"(\d+(\.\d{1,2})?)?\s([A-Za-z]{3})$";
                    if (Regex.IsMatch(query.RawQuery, pattern))
                    {
                        //inputs
                        var money = Convert.ToDecimal(query.FirstSearch);
                        toCurrency = query.SecondSearch.ToUpper();
                        if (Enum.IsDefined(typeof(RateList),query.SecondSearch.ToUpper()))
                        {
                            //Checks if inputs exists in enum
                            _currency = GetCurrency(toCurrency);
                            //Extra info in the subtitle field
                            var usd = _currency.GetRate(RateList.USD.ToString());
                            var eur = _currency.GetRate(RateList.EUR.ToString());
                            var gbp = _currency.GetRate(RateList.GBP.ToString());
                            var jpy = _currency.GetRate(RateList.JPY.ToString());

                            var rate = _currency.GetRate(localISOSymbol);
                            result = money * rate;
                            results.Add(new Result
                            {
                                Title = $"{money.ToString(".00")} {toCurrency} = {result.ToString("C")} {localISOSymbol}",
                                IcoPath = "Images/bank.png",
                                SubTitle = $"More currencies: 100 {localISOSymbol} = {usd * 100} {RateList.USD} " +
                                           $"\n100 {localISOSymbol} = {eur * 100} {RateList.EUR} " +
                                           $" # 100 {localISOSymbol} = {gbp * 100} {RateList.GBP} " +
                                           $" # 100 {localISOSymbol} = {jpy * 100} {RateList.JPY}"
                            });
                        }
                    }
                }

                else if (query.RawQuery != null
                    && query.RawQuery.Split(' ').Length == 4) //100 usd in nok
                {
                    var pattern = @"(\d+(\.\d{1,2})?)?\s([A-Za-z]{3})\s([i][n])\s([A-Za-z]{3})";
                    if (Regex.IsMatch(query.RawQuery, pattern))
                    {
                        //Inputs
                        var fromCurrency = query.SecondSearch.ToUpper();
                        toCurrency = query.RawQuery.Split(' ')[3].ToUpper();
                        var money = Convert.ToDecimal(query.FirstSearch);
                        
                        if (Enum.IsDefined(typeof(RateList), fromCurrency)
                            && Enum.IsDefined(typeof(RateList), query.RawQuery.Split(' ')[3].ToUpper()))
                        {
                            //Checks if the input currencies exists in enum
                            _currency = GetCurrency(fromCurrency);
                            var rate = _currency.GetRate(toCurrency);
                            result = money * rate;
                            results.Add(new Result
                            {
                                Title = $"{money.ToString(".00")} {fromCurrency} = {result.ToString("C")} {toCurrency}",
                                IcoPath = "Images/bank.png"
                            });
                        }
                    }
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
        public string GetLanguagesFolder()
        {
            return Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Languages");
        }
        public string GetTranslatedPluginTitle()
        {
            return _context.API.GetTranslation("wox_plugin_currency");
        }
        public string GetTranslatedPluginDescription()
        {
            return _context.API.GetTranslation("wox_plugin_currency_plugin_description");
        }

        #region helpers
        private Models.Currency GetCurrency(string from)
        {
            var url = $"http://api.fixer.io/latest?base={from}";
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            var response = (HttpWebResponse)request.GetResponse();
            using (new StreamReader(response.GetResponseStream()))
            {
                var responsestring = new StreamReader(response.GetResponseStream()).ReadToEnd();
                return JsonConvert.DeserializeObject<Models.Currency>(responsestring);
            }
        }
        #endregion
    }
}
