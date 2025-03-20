using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace TraceabilityEngine.Util
{
    public static class TESequentialGUID
    {
        [DllImport("rpcrt4.dll", SetLastError = true)]
        static extern int UuidCreateSequential(out Guid guid);
        static object _statLocker = new object();
        public static Guid NextValue()
        {
            lock (_statLocker)
            {
                const int RPC_S_OK = 0;
                Guid g;
                int hr = UuidCreateSequential(out g);
                if (hr != RPC_S_OK)
                {
                    throw new ApplicationException("UuidCreateSequential failed: " + hr);
                }
                return g;
            }
        }
    }
}
