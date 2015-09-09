using Newtonsoft.Json;
using System.IO;
using System.Net;

namespace AsmAnalyzer.Util
{
    public static class JsonHelper
    {
        public static string SerializeJson<T>(T obj)
        {
            var settings = new JsonSerializerSettings() { NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore, DateFormatHandling = DateFormatHandling.MicrosoftDateFormat };
            return JsonConvert.SerializeObject(obj, settings);
        }
    }
}
