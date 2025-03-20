using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using TraceabilityEngine.Util.ObjectPooling;

namespace TraceabilityEngine.Util.Net
{
    public class HttpUtil
    {
        public static LimitedPool<HttpClient> ClientPool = new LimitedPool<HttpClient>(() =>
        {
            return new HttpClient();
        },
        (HttpClient client) =>
        {

        }, new TimeSpan(0, 0, 15, 0));
    }
}
