using System;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Wox.Plugin.Currency
{
    internal static class GuardQueriesExtensions
    {
        public static Query IsCurrencyConversion(this Query query)
        {
            if(query.IsOneWayConversion() 
                || query.IsTwoWayConversion())
            {
                return query;
            }
            return null;
        }

        public static Query IsNotNull(this Query query)
        {
            if (!string.IsNullOrEmpty(query.RawQuery))
                return query;
            throw new Exception("Query is null");
        }

        public static bool IsOneWayConversion(this Query query)
        {
            return (Regex.IsMatch(query.Search, @"^(\d+(\.\d{1,2})?)?\s([A-Za-z]{3})$"));//10 usd
        }

        public static bool IsTwoWayConversion(this Query query)
        {
            return (Regex.IsMatch(query.Search, @"(\d+(\.\d{1,2})?)?\s([A-Za-z]{3})\s([i][n])\s([A-Za-z]{3})")); // 10 usd in nok
        }

    public static bool VerifyQueryInputs(this Query query, int numbersOfInput)
        {
            return (query.RawQuery.Split(' ').Length == numbersOfInput);
        }
    }
}