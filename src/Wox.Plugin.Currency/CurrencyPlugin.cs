using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Wox.Plugin.Currency.Models;

namespace Wox.Plugin.Currency
{
    public class CurrencyPlugin : IPlugin
    {
        #region private fields

        private PluginInitContext _context;
        string _localIsoSymbol = RegionInfo.CurrentRegion.ISOCurrencySymbol;
        private static readonly HttpClient client = new HttpClient();
        private static List<string> GetGetregExs()
        {
            return new List<string>(){
                @"(\d+(\.\d{1,2})?)?\s([A-Za-z]{3})\s([Ii][nn])\s([A-Za-z]{3})", // 10 usd in nok
                @"(\d+(\.\d{1,2})?)?\s([A-Za-z]{3})\s([tt][Oo])\s([A-Za-z]{3})", // 10 usd to nok
                @"(\d+(\.\d{1,2})?)?\s([A-Za-z]{3})\s([Aa][Ss])\s([A-Za-z]{3})", // 10 usd as nok    
                @"^(\d+(\.\d{1,2})?)?\s([A-Za-z]{3})\s([A-Za-z]{3})$", //10 usd nok
                @"^(\d+(\.\d{1,2})?)?\s([A-Za-z]{3})$" // 10 usd
            };
        }

        #endregion

        public CurrencyPlugin()
        {

        }

        public List<Result> Query(Query query)
        {
            var results = new List<Result>();
            try
            {
                //From and to converstion
                foreach (var regex in GetGetregExs())
                {
                    if (Regex.IsMatch(query.RawQuery, regex))
                    {
                        var input_split = query.RawQuery.Split(' ');
                        var from = input_split[1].ToUpper();
                        var to = _localIsoSymbol;
                        if (input_split.Length > 3)
                        {
                            to = input_split[3].ToUpper();
                        }
                        else if (input_split.Length > 2)
                        {
                            to = input_split[2].ToUpper();
                        }
                        var amount = System.Convert.ToDecimal(input_split[0]);
                        //var convertedAmount = GetCurrency(from, to, amount.ToString());

                        var url = $"https://convertexpressapi.azurewebsites.net/api/ConvertCurrency?convert={amount} {from} {to}";

                         var convertedAmount = client.GetStringAsync(url).Result;

                        results.Add(new Result
                        {
                            Title = $"{amount.ToString(".00")} {from} = {convertedAmount} {to} ",
                            IcoPath = "Images/bank.png",
                            SubTitle = $"Source: https://frankfurter.app (Last updated )"
                        });
                    }
                }

                return results;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return new List<Result>();
            }

        }
        
        public void Init(PluginInitContext context)
        {
            _context = context;
        }
    
        private bool CheckRegExOnQuery(string query)
        {
            return false;
        }
    }
}
