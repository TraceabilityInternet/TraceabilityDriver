﻿using TraceabilityEngine.Interfaces.Models.Events;
using System;
using System.Collections.Generic;
using System.Text;
using TraceabilityEngine.Interfaces.Models;

namespace TraceabilityEngine.Interfaces.Mappers
{
    public interface ITEEPCISMapper
    {
        string WriteEPCISData(ITEEPCISDocument data);
        ITEEPCISDocument ReadEPCISData(string value);
    }
}