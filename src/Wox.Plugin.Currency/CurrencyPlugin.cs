using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Wox.Plugin.Currency
{
    public class CurrencyPlugin : IPlugin
    {
        private const string Pattern = @"^(?<amount>\d+(?:\.\d+)?)\s(?<from>[A-Za-z]{3})\s(?:to|in)\s(?<to>[A-Za-z]{3})$";
        
        public void Init(PluginInitContext context)
        {
        }

        public List<Result> Query(Query query)
        {
            var match = Regex.Match(query.Search, Pattern);
            if (match.Success)
            {
                var from = match.Groups["from"].Value.ToUpper();
                var to = match.Groups["to"].Value.ToUpper();
                var amount = double.Parse(match.Groups["amount"].Value);
                var value = CurrencyHelper.Convert(from, to, amount);

                return new List<Result>
                {
                    new Result
                    {
                        Title = $"{amount} {from} = {value} {to}",
                        IcoPath = "Images/bank.png",
                        SubTitle = $"Source: fixer.io."
                    }
                };
            }

            return new List<Result>();
        }       
    }
}
