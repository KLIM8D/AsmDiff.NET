using AsmAnalyzer.Util;
using System;
using System.Collections.Generic;

namespace AsmAnalyzer
{
    [Serializable]
    public class DataStore
    {
        public Dictionary<string, CommonObject> Data { get; set; }
    }
}
