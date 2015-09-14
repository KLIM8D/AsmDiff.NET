using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsmAnalyzer.Util
{
    #region HtmlHelper
    public class MetaData
    {
        public AssemblyMetaData Source { get; set; }
        public AssemblyMetaData Target { get; set; }
        public string Filter { get; set; }
        public string Pattern { get; set; }
        public string Flags { get; set; }
        public string CommandArguments { get; set; }
    }

    public class AssemblyMetaData
    {
        public string Path { get; set; }
        public ICollection<string> AssemblySuccess { get; set; }
        public ICollection<string> AssemblyErrors { get; set; }
    }
    #endregion

    #region ObjectSerializer
    public class TupleList<T1, T2> : List<Tuple<T1, T2>>
    {
        public void Add(T1 item, T2 item2)
        {
            Add(new Tuple<T1, T2>(item, item2));
        }
    }

    public class TupleList<T1, T2, T3> : List<Tuple<T1, T2, T3>>
    {
        public void Add(T1 item, T2 item2, T3 item3)
        {
            Add(new Tuple<T1, T2, T3>(item, item2, item3));
        }
    }

    [Serializable]
    public class CommonObject
    {
        public string TypeFullName { get; set; }
        public ICollection<Util.CommonProperty> Properties { get; set; }
    }

    [Serializable]
    public class CommonProperty
    {
        public string Name { get; set; }
        public string PropertyType { get; set; }

        public override string ToString()
        {
            return String.Format("{0} {1}", PropertyType, Name);
        }
    }
    #endregion

    #region Analyzer
    [Serializable]
    public class DataStore
    {
        public Dictionary<string, CommonObject> Data { get; set; }
    }
    #endregion
}
