using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Wox.Plugin.Currency.Models
{
    class SearchParameters
    {
        public string BaseIso { get; set; }
        public string ToIso { get; set; }
        public List<string> OptionalIsos { get; set; }
    }
}
