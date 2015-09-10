#region LICENSE
/*
The MIT License (MIT)
Copyright (c) 2015 Morten Klim Sørensen
See LICENSE.txt for more information
*/
#endregion
using AsmAnalyzer.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using xxHashSharp;

namespace AsmAnalyzer
{
    public class Result
    {
        public string ClassName { get; set; }
        public ICollection<ResultItem> Items { get; set; }
    }

    public class ResultItem
    {
        public string Before { get; set; }
        public string After { get; set; }
        public string Type { get; set; }
    }

    /// <summary>
    /// Base class for the analyzer library. Contains method for analyzing the assemblies, retrieving their types, propertynames etc. and comparing them.
    /// 
    /// xxHash algorithm chosen based upon https://github.com/Cyan4973/xxHash (https://github.com/noricube/xxHashSharp)
    ///                                and http://fastcompression.blogspot.dk/2012/04/selecting-checksum-algorithm.html
    /// 
    /// Changes:
    /// 
    /// </summary>
    /// <version>1.0</version>
    public sealed class Analyzer
    {
        [Flags]
        public enum AnalyzerFlags
        {
            Addition = 1 << 0,
            Changes  = 1 << 1, 
            Deletion = 1 << 2, 
            None     = 1 << 3
        }

        public AnalyzerFlags Flags { get; set; }

        enum ResultType
        {
            Unchanged,
            Addition,
            Change,
            Deletion,
            Unknown
        }

        /// <summary>
        /// Starts the analyzer
        /// </summary>
        /// <param name="source">This is the OLD version of the library. Either a path to a specific assembly or a folder which contains assemblies</param>
        /// <param name="target">This is the NEW version of the library. Either a path to a specific assembly or a folder which contains assemblies</param>
        /// <param name="filter">Specify a filter, which will exclude all other classes, than the one specified in the filter. Must be the class name including the namespace (eg. System.Reflection.Assembly)</param>
        /// <returns>The difference between the assemblies in `source` and `target`</returns>
        public ICollection<Result> Invoke(string source, string target, string filter, Regex pattern, MetaData metaData)
        {
            var sourceSandbox = new AssemblySandbox();
            var sourceAssemblies = Setup(sourceSandbox, source, filter, "SourceSandbox", pattern);

            var sourceDataStore = sourceSandbox.Reflect(sourceAssemblies, (asm, f) =>
            {
                var ds = new DataStore { Data = new Dictionary<string, CommonObject>() };
                var a = new Analyzer();
                a.GetAssemblyInfo(f, ds, asm);
                return ds;
            });


            var targetSandbox = new AssemblySandbox();
            var targetAssemblies = Setup(targetSandbox, target, filter, "TargetSandbox", pattern);

            var targetDataStore = targetSandbox.Reflect(targetAssemblies, (asm, f) =>
            {
                var ds = new DataStore { Data = new Dictionary<string, CommonObject>() };
                var a = new Analyzer();
                a.GetAssemblyInfo(f, ds, asm);
                return ds;
            });

            return Analyze(sourceDataStore, targetDataStore);
        }

        /// <summary>
        /// Loads one or more assemblies file from disk based on the path given
        /// They are loaded into the tempoary AppDomain found in the AssemblySandbox
        /// </summary>
        /// <param name="sandbox">The tempoary AppDomain which the assemblies should belong to</param>
        /// <param name="path">The given path is the FULL system path to either a specific assembly or a folder containing more assemblies</param>
        /// <param name="filter">Must be the class name including the namespace (eg. System.Reflection.Assembly)</param>
        /// <param name="domainName">The desired name of the tempoary AppDomain</param>
        /// <returns>A list which holds the fullpaths for every assembly loaded</returns>
        private List<string> Setup(AssemblySandbox sandbox, string path, string filter, string domainName, Regex regex)
        {
            var asmPaths = new List<string>();
            if (path.Contains(".dll"))
            {
                sandbox.LoadAssembly(path, domainName, filter);
                asmPaths.Add(path);
            }
            else
            {
                if (regex != null)
                {
                    foreach (var dll in Directory.GetFiles(path, "*.dll").Where(x => regex.IsMatch(x)))
                    {
                        sandbox.LoadAssembly(dll, domainName, filter);
                        asmPaths.Add(dll);
                    }
                }
                else
                {
                    foreach (var dll in Directory.GetFiles(path, "*.dll"))
                    {
                        sandbox.LoadAssembly(dll, domainName, filter);
                        asmPaths.Add(dll);
                    }
                }
            }

            return asmPaths;
        }

        /// <summary>
        /// Retrieves information about the types and properties from within the given assembly
        /// </summary>
        /// <param name="filter">Specify a filter, which will exclude all other classes, than the one specified in the filter</param>
        /// <param name="ds">The DataStore where the information should be saved</param>
        /// <param name="asm">The assembly</param>
        private void GetAssemblyInfo(Type filterType, DataStore ds, Assembly asm)
        {
            // get the types from the assembly
            var types = asm.GetTypes();

            foreach (Type item in types)
            {
                if (!ApplyFilter(item, filterType))
                    continue;

                var t = Tuple.Create<Type, PropertyInfo[]>(item, item.GetProperties());
                //T1: Full namespace path
                //T2: All information about the given property, including type, namespace, class info etc.
                ds.Data.Add(item.FullName, t.Serialize());
            }
        }

        /// <summary>
        /// Used to apply a filter to the scanned assembly and here by limit the output
        /// </summary>
        /// <param name="type">The type which the filter is performend upon</param>
        /// <param name="filter">The filter which should be applied for this scan</param>
        /// <returns>Returns a boolean whenever the type satifies the filter or not</returns>
        private bool ApplyFilter(Type type, Type filter)
        {
            if (filter == null)
                return true;

            //var properties = type.GetProperties();
            var f = filter.Name.ToUpperInvariant();
            if (type.Name.ToUpperInvariant().Contains(f) || type.FullName.ToUpperInvariant().Contains(f))
                return true;
            else if (filter.IsAssignableFrom(type))
                return true;
            //else if (properties.Any(x => x.ToString().ToUpperInvariant().Contains(f)))
            //    return true;

            return false;
        }

        /// <summary>
        /// Analyze and compare the properties of the two DLL (assemblies)
        /// </summary>
        /// <param name="source">The old version of the DLL</param>
        /// <param name="target">The new version of the DLL which should be compared to the old</param>
        /// <returns>A collection of properties which either have been (added, changed datatype, renamed or removed)</returns>
        private ICollection<Result> Analyze(DataStore source, DataStore target)
        {
            bool inclChanges   = ((Flags & AnalyzerFlags.Changes)  == AnalyzerFlags.Changes);
            bool inclAdditons  = ((Flags & AnalyzerFlags.Addition) == AnalyzerFlags.Addition);
            bool inclDeletions = ((Flags & AnalyzerFlags.Deletion) == AnalyzerFlags.Deletion);

            var returnVal = new List<Result>();
            foreach (var sourceItem in source.Data)
            {
                string srcChkSum = GetChecksum(sourceItem.Value);
                CommonObject targetItem;
                if (target.Data.TryGetValue(sourceItem.Key, out targetItem))
                {
                    string tarChkSum = GetChecksum(targetItem);

                    if (!CompareChecksum(srcChkSum, tarChkSum))
                    {
                        var result = new Result
                        {
                            ClassName = sourceItem.Key,
                            Items = new List<ResultItem>()
                        };

                        // these 2 dictionaries are used to keep track of already processed items
                        // and items which couldn't be resolved during the first loop
                        var srcUnknownItems = new Dictionary<string, CommonProperty>();
                        var tarKnownItems = new Dictionary<string, CommonProperty>();

                        // go through each item in `source` and check if there are any differences between source and targets properties
                        foreach (var srcProp in sourceItem.Value.Properties)
                        {
                            var tarProp = targetItem.Properties.FirstOrDefault(x => x.Name.Equals(srcProp.Name));
                            ResultType rt = ResultType.Unchanged;
                            if (tarProp == null)
                            {
                                // renamed or deleted
                                srcUnknownItems.Add(srcProp.ToString(), srcProp);
                            }
                            else if (!srcProp.Name.Equals(tarProp.Name))
                            {
                                rt = ResultType.Change;
                            }
                            else if (!srcProp.PropertyType.Equals(tarProp.PropertyType))
                            {
                                rt = ResultType.Change;
                            }

                            if (rt != ResultType.Unchanged && inclChanges)
                            {
                                var r = NewResultItem(srcProp, tarProp, rt);
                                result.Items.Add(r);
                            }

                            if (tarProp != null && !tarKnownItems.ContainsKey(tarProp.ToString()))
                                tarKnownItems.Add(tarProp.ToString(), tarProp);
                        }

                        // get all the items which either have been renamed or removed, and therefore couldn't be resolved in the first loop
                        var unresolvedTarItems = targetItem.Properties.Where(x => { CommonProperty y; return !tarKnownItems.TryGetValue(x.ToString(), out y); }).ToList();
                        if (inclDeletions)
                        {
                            foreach (var srcProp in srcUnknownItems.Values)
                            {
                                // look up the item by it's datatype
                                //var tarProp = unresolvedTarItems.FirstOrDefault(x => { CommonProperty y; return x.PropertyType.Equals(srcProp.PropertyType)
                                //                                                       && !tarKnownItems.TryGetValue(x.ToString(), out y); });
                                //if (tarProp != null)
                                //{
                                //    if (inclChanges)
                                //    {
                                //        var r = NewResultItem(srcProp, tarProp, ResultType.Change);
                                //        result.Items.Add(r);
                                //    }

                                //    tarKnownItems.Add(tarProp.ToString(), tarProp);
                                //}
                                //else if (inclDeletions)
                                //{
                                    var r = NewResultItem(srcProp, null, ResultType.Deletion);
                                    result.Items.Add(r);
                                //}
                            }
                        }

                        if (inclAdditons)
                        {
                            // all the items which exists in the collection `unresolvedTarItems`, but doesn't exists in the
                            // source DLL and in the collection of already processed items, must be new to the model
                            var addedItems = unresolvedTarItems.Where(x => !sourceItem.Value.Properties.Contains(x) && !tarKnownItems.Values.Contains(x)).ToList();

                            foreach (var addedItem in addedItems)
                            {
                                var r = NewResultItem(null, addedItem, ResultType.Addition);
                                result.Items.Add(r);
                            }
                        }

                        returnVal.Add(result);
                    }
                }
                else if (inclDeletions)
                {
                    // if this frame is reached, the whole class has been removed in the new version of the assembly
                    var result = new Result
                    {
                        ClassName = sourceItem.Key,
                        Items = new List<ResultItem>()
                    };

                    foreach (var removedItem in sourceItem.Value.Properties)
                    {
                        var r = NewResultItem(removedItem, null, ResultType.Deletion);
                        result.Items.Add(r);
                    }

                    returnVal.Add(result);
                }
            }

            if (inclAdditons)
            {
                // include the classes which is new in this version of the assembly
                var addedClasses = target.Data.Where(x => !source.Data.Keys.Contains(x.Key));
                foreach (var addedClass in addedClasses)
                {
                    var result = new Result
                    {
                        ClassName = addedClass.Key,
                        Items = new List<ResultItem>()
                    };

                    foreach (var addedProp in addedClass.Value.Properties)
                    {
                        var r = NewResultItem(null, addedProp, ResultType.Addition);
                        result.Items.Add(r);
                    }

                    returnVal.Add(result);
                }
            }

            return returnVal.Where(x => x.Items.Count != 0).ToList();
        }

        private static ResultItem NewResultItem(CommonProperty before, CommonProperty after, ResultType type)
        {
            var r = new ResultItem
            {
                Type = Enum.Format(typeof(ResultType), type, "g"),
                Before = before != null ? before.ToString() : "None",
                After = after != null ? after.ToString() : "Removed"
            };
            return r;
        }

        /// <summary>
        /// Get the checksum for an object, using reflection and the xxHash algorithm.
        /// 
        /// </summary>
        /// <param name="input">The object which the checksum should be calculated from</param>
        /// <returns>The checksum for the object</returns>
        private string GetChecksum(CommonObject input)
        {
            var sb = new StringBuilder();

            sb.Append(input.TypeFullName);

            foreach (Util.CommonProperty prop in input.Properties)
            {
                sb.Append(prop.ToString());
            }

            string content = sb.ToString();

            // convert the input string to a byte array and compute the hash.
            byte[] data = Encoding.Default.GetBytes(content);
            uint hash = xxHash.CalculateHash(data);

            // return the hexadecimal string.
            return String.Format("{0:X}", hash);
        }

        private bool CompareChecksum(string source, string target)
        {
            // create a StringComparer an compare the hashes.
            StringComparer comparer = StringComparer.Ordinal;

            return (0 == comparer.Compare(source, target)) ? true : false;
        }
    }
}
