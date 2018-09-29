using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Wox.Core.Plugin;


namespace Wox.Plugin.Currency.Test
{
    [TestClass]
    public class CurrencyPluginTest
    {
        [TestMethod]
        public void TestPlugin()
        {
            var plugin = new CurrencyPlugin();
            var q = PluginManager.QueryInit("10 usd in nok");

            //var result = plugin.Query(q);
        }
    }
}
