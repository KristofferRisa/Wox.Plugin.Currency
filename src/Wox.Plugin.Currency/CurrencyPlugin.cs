using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Wox.Plugin.Currency.Models;
using System.Runtime.InteropServices;
using Wox.Plugin.Currency.ViewModels;
using System.Windows.Controls;
using Wox.Plugin.Currency.Views;
using Wox.Infrastructure.Storage;
using Newtonsoft.Json.Linq;

namespace Wox.Plugin.Currency
{
    public class CurrencyPlugin : IPlugin, ISavable, ISettingProvider
    {
        private PluginInitContext Context { get; set; }

        private static List<CacheItem> cache;
        
        private static Settings settings;

        private static SettingsViewModel viewModel;

        public CurrencyPlugin()
        {
            if (cache == null)
            {
                cache = new List<CacheItem>();
            }
        }

        public void Init(PluginInitContext context)
        {
            this.Context = context;            
            viewModel = new SettingsViewModel();
            settings = viewModel.Settings;
        }

        public List<Result> Query(Query query)
        {
            if (query.FirstSearch == "clear")
            {
                // Clears old entries 
                if (cache.Any(x => x.Created.Date < DateTime.Now.Date))
                {
                    cache.RemoveAll(x => x.Created.Date < DateTime.Now.Date);
                }
            }
            if (query == null || query.Search.Length < 5)
            {
                return new List<Result>();
            } 
            
            try
            {                
                if (IsOneWayConversion(query.RawQuery) 
                    || IsTwoWayConversion(query.RawQuery))
                {
                    var _money = Convert.ToDecimal(query.FirstSearch);
                    var _fromCurrency = query.RawQuery.Split(' ')[1].ToUpper();
                    var _toCurrency = settings.BaseCurrency.ToUpper();

                    if (IsTwoWayConversion(query.RawQuery))
                    {
                        _toCurrency = query.RawQuery.Split(' ')[3].ToUpper();   
                        
                        if(_fromCurrency == "SAT"
                            && _toCurrency == "BTC")
                        {
                            var convertedSat = _money / 100000000;
                            return new List<Result>()
                            {
                                new Result
                                {
                                    Title = $"{_money} {_fromCurrency} = {convertedSat} {_toCurrency}",
                                    IcoPath = "Images/Bitcoin.png",
                                    SubTitle = $"1 bitcoin = 100,000,000 satoshis",
                                    Score = 100,
                                    Action = c =>
                                    {
                                        try
                                        {
                                            Clipboard.SetText(convertedSat.ToString());
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
                    }

                    // exits if not to or from currency is in the supported enum list
                    if (!Enum.IsDefined(typeof(RateList),_toCurrency) 
                        || !Enum.IsDefined(typeof(RateList),_fromCurrency))
                        return new List<Result>();

                    var item = new CacheItem()
                    {
                        From = _fromCurrency,
                        To = _toCurrency,
                        Created = DateTime.Now.Date
                    };

                    // checking of query is in cache
                    if (cache.Count > 0
                        && cache.Any(i => i.From == item.From
                            && i.To == item.To
                            && i.Created.Date == item.Created.Date))
                    {
                        item.BtcRate = cache
                            .Where(i => i.From == item.From
                                && i.To == item.To
                                && i.Created.Date == item.Created.Date)
                            .Select(x => x.BtcRate)
                            .FirstOrDefault();
                        item.Rate = cache
                            .Where(i => i.From == item.From
                                && i.To == item.To
                                && i.Created.Date == item.Created.Date)
                            .Select(x => x.Rate)
                            .FirstOrDefault();
                    }
                    else
                    {                        
                        try
                        {
                            ServicePointManager.Expect100Continue = true;
                            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

                            if (item.From == "BTC" || item.To == "BTC")
                            {
                                using (WebClient client = new WebClient())
                                {
                                    string response = client.DownloadString("https://api.coindesk.com/v1/bpi/currentprice/USD.json");

                                    // Parse the response as JSON
                                    JObject json = JObject.Parse(response);
                                    item.BtcRate = (decimal)json["bpi"]["USD"]["rate_float"];
                                }
                            }
                            
                            if (settings.ActiveApiProvider == "frankfurter.app")
                            {                                
                                var url = $"https://api.frankfurter.app/latest?from={item.From}&to={item.To}";                                
                                // Handle BTC
                                if ((item.From == "BTC" 
                                    && item.To != "USD") 
                                    || (item.From != "USD"
                                        && item.To == "BTC")
                                    )
                                {
                                    if(item.From == "BTC")
                                        url = $"https://api.frankfurter.app/latest?from=USD&to={item.To}";
                                    if (item.To== "BTC")
                                        url = $"https://api.frankfurter.app/latest?from=USD&to={item.From}";
                                }
                                if((item.From == "BTC" 
                                    && item.To == "USD") 
                                    || (
                                        item.To == "USD" 
                                        && item.From == "BTC")
                                       )
                                {
                                    // use rates from coindesk 
                                }
                                else
                                {
                                    var request = (HttpWebRequest)WebRequest.Create(url);
                                    request.Method = "GET";
                                    var response = (HttpWebResponse)request.GetResponse();
                                    using (new StreamReader(response.GetResponseStream()))
                                    {
                                        var responsestring = new StreamReader(response.GetResponseStream()).ReadToEnd();
                                        var currency = JsonConvert.DeserializeObject<Models.Currency>(responsestring);
                                        if (item.To == "BTC")
                                            item.Rate = currency.GetRate(item.From);
                                        else
                                            item.Rate = currency.GetRate(item.To);
                                        item.Created = DateTime.Parse(currency.date);
                                    }
                                }
                                cache.Add(item);
                            }                            
                        }
                        catch (Exception e)
                        {
                            new Result
                            {
                                Title = $"Failed to load data from API, {e.Message}",
                                IcoPath = "Images/bank.png",
                                Score = 10
                            };
                        }
                        

                    }
                    var amount = ((double)(_money * item.Rate)).ToString("C2");

                    if (item.BtcRate == 0)
                    {
                        return new List<Result>()
                        {
                            new Result
                            {
                                Title = $"{_money} {_fromCurrency} = {amount} {_toCurrency}",
                                IcoPath = "Images/bank.png",
                                SubTitle = $"Enter to copy. Source: https://frankfurter.app (Updated {item.Created.ToString("yyyy-MM-dd")})",
                                Score = 100,
                                Action = c =>
                                {
                                    try
                                    {
                                        Clipboard.SetText(amount);
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
                    else
                    {
                        if(item.To == "USD" )
                        {
                            amount = ((double)(_money * item.BtcRate)).ToString("C2");
                        } 
                        else if (item.From == "USD") // 10 USD to BTC
                        {
                            amount = ((double)(_money / item.BtcRate)).ToString("C2");
                        }
                        else if (item.To == "BTC")
                        {
                            amount = ((double)((_money / item.Rate) / item.BtcRate)).ToString("C2");
                        }
                        else
                        {
                            amount = ((double)(_money * item.Rate * item.BtcRate)).ToString("C2");
                        }
                        
                        return new List<Result>()
                        {
                            new Result
                            {
                                Title = $"{_money} {_fromCurrency} = {amount} {_toCurrency}",
                                IcoPath = "Images/Bitcoin.png",
                                SubTitle = $"Enter to copy. Source: https://api.coindesk.com (Updated {item.Created.ToString("yyyy-MM-dd")})",
                                Score = 100,
                                Action = c =>
                                {
                                    try
                                    {
                                        Clipboard.SetText(amount);
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
                    
                }
            }
            catch
            {
                // Ignored
            }
            return new List<Result>();
        }

        public bool IsOneWayConversion(string query)
        {
            return (Regex.IsMatch(query, @"^(\d+(?:[.,]\d+)?)?\s([A-Za-z]{3})$")); //10,2 usd
        }

        public bool IsTwoWayConversion(string query)
        {
            return (
                Regex.IsMatch(query, @"^(\d+(?:[.,]\d+)?)?\s([A-Za-z]{3})\s([i][n])\s([A-Za-z]{3})") // 10,2 usd in nok 
                || Regex.IsMatch(query, @"^(\d+(?:[.,]\d+)?)?\s([A-Za-z]{3})\s([A-Za-z]{3})")); // 10,2 usd nok
        }

        public Control CreateSettingPanel()
        {
            return new CurrencySettings(viewModel);
        }

        public void Save()
        {
            viewModel.Save();
        }
    }
}
