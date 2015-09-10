#region LICENSE
/*
The MIT License (MIT)
Copyright (c) 2015 Morten Klim Sørensen
See LICENSE.txt for more information
*/
#endregion
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AsmAnalyzer.Util
{
    public static class ObjectSerializer
    {
        //T1: The regex containing the string which should be replaced
        //T2: What it should be replaced with
        //T3: Is suffix replacement i.e. add the replacement to the end of the remaining string or in front
        static TupleList<Regex, string, bool> pretifyPatterns = new TupleList<Regex, string, bool>
        {
            { new Regex(@"\b(System\.Nullable`1\[\[)\b", RegexOptions.Compiled), "?", true }
        };

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
                    PropertyType = pi.PropertyType.FullName != null ? pi.PropertyType.FullName.GetTypeInfo().Pretify() : "UnknownType"
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

        public static string Pretify(this string t)
        {
            foreach (var pattern in pretifyPatterns)
            {
                if (pattern.Item3 && pattern.Item1.IsMatch(t))
                {
                    t = pattern.Item1.Replace(t, "");
                    t += pattern.Item2;
                }
                else
                {
                    t = pattern.Item1.Replace(t, pattern.Item2);
                }
            }

            return t;
        }
    }
}
