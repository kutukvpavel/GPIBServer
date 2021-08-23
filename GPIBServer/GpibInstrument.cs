using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace GPIBServer
{
    public enum GpibInstrumentSearch
    {
        Address,
        Id
    }

    public class GpibInstrument
    {
        public int Address { get; }
        public string Id { get; }
        public GpibInstrumentSearch SearchMode { get; }
        public GpibCommand[] CommandSet { get; }
    }
}
