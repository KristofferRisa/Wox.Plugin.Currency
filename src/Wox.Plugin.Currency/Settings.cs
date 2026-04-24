using System;
using System.Collections.Generic;
using System.Linq;
using Wox.Plugin.Currency.Models;

namespace Wox.Plugin.Currency
{
    public class Settings
    {
        public Settings()
        {
            BaseCurrency = "USD";
            Providers = new List<string>()
            {
                "frankfurter.app"
            };
            ActiveApiProvider = "frankfurter.app";
            Rates = Enum.GetNames(typeof(RateList)).ToList();
        }
        public string BaseCurrency { get; set; }
        public string ActiveApiProvider { get; set; }
        public List<string> Providers { get; set; }

        public List<string> Rates { get; set; }

    }
}
