using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceabilityEngine.Interfaces.Driver;

namespace TraceabilityEngine.Models.Driver
{
    public static class TEDriverFactory
    {
        public static ITEDriverAccount CreateAccount(string jsonStr)
        {
            TEDriverAccount account = null;
            if (!string.IsNullOrWhiteSpace(jsonStr))
            {
                account = new TEDriverAccount();
                account.FromJson(jsonStr);
            }
            return account;
        }

        public static ITEDriverTradingPartner CreateTradingPartner(string jsonStr)
        {
            TEDriverTradingPartner tp = null;
            if (!string.IsNullOrWhiteSpace(jsonStr))
            {
                tp = new TEDriverTradingPartner();
                tp.FromJson(jsonStr);
            }
            return tp;
        }
    }
}
