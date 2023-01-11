using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wox.Plugin.Currency.Models
{
    internal class CacheItem
    {
        public DateTime Created { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public decimal Rate { get; set; }
    }
}
