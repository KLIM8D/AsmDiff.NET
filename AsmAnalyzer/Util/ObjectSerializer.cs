#region LICENSE
/*
The MIT License (MIT)
Copyright (c) 2015 Morten Klim Sørensen
See LICENSE.txt for more information
*/
#endregion
using System;
using System.Collections.Generic;

namespace AsmAnalyzer.Util
{
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

    public static class ObjectSerializer
    {
        /// <summary>
        /// Serializes the Tuple<T1,T2> into a object which doensn't include the types from the external assemblies
        /// and is hereby known by the applications AppDomain
        /// </summary>
        /// <param name="obj">The object which holds the type information</param>
        /// <returns>A `CommonObject` where information from `obj` is retrieved</returns>
        public static CommonObject Serialize(this Tuple<Type, System.Reflection.PropertyInfo[]> obj)
        {
            var returnVal = new CommonObject 
            { 
                TypeFullName = obj.Item1.FullName,
                Properties = new List<Util.CommonProperty>()
            };

            foreach (var pi in obj.Item2)
            {
                var prop = new Util.CommonProperty
                {
                    Name = pi.Name,
                    PropertyType = pi.PropertyType.FullName != null ? pi.PropertyType.FullName.GetTypeInfo() : "UnknownType"
                };
                returnVal.Properties.Add(prop);
            }

            return returnVal;
        }

        public static string GetTypeInfo(this string t)
        {
            var name = t.Split(',');
            if (name.Length == 1)
                return t;

            return name[0];
        }
    }
}
