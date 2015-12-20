using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Wox.Plugin.Currency.Models
{
    class Response
    {
        public SearchParameters SearchParameters { get; set; }
        public Currency Currency { get; set; }
        public override bool Equals(object obj)
        {
            if (!(obj is Response))
            {
                return false;
            }
            Response response = obj as Response;
            return (response.SearchParameters == this.SearchParameters);
        }
        public override int GetHashCode()
        {
            return Int32.MaxValue;
        }
    }
}
