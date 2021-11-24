using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LiveHome.Server.Models;

namespace LiveHome.Server
{
    public static class DoubleTupleExtension
    {
        public static EnvironmentInfo AsEnvironmentInfoStruct(this (double, double) val)
        {
            return new EnvironmentInfo() { Temperature = val.Item1, RelativeHumidity = val.Item2 };
        }
    }
}
