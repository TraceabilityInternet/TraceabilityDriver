using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.Util
{
    static public class TERandom
    {
        static Random m_Random;

        static TERandom()
        {
            m_Random = new Random(Guid.NewGuid().GetHashCode());
        }
        public static double Next(double min, double max)
        {
            double nextCDF = m_Random.NextDouble();
            double next = min + (max - min) * nextCDF;
            return (next);
        }

        public static int Next(int min, int max)
        {
            double nextCDF = m_Random.NextDouble();
            double range = max - min;
            double interval = range * nextCDF;
            int next = min + (int)interval;
            return (next);
        }
    }
}
