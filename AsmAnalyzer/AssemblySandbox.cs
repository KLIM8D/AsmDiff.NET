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
using System.Security.Policy;

namespace AsmAnalyzer
{
    public class AssemblySandbox : IDisposable
    {
        public Dictionary<string, AppDomain> MapDomains { get; private set; }
        public Dictionary<string, AppDomain> LoadedAssemblies { get; private set; }
        public Dictionary<string, AssemblyReflectionProxy> Proxies { get; private set; }
        public Type Filter { get; set; }

        public AssemblySandbox()
        {
            MapDomains = MapDomains ?? new Dictionary<string, AppDomain>();
            LoadedAssemblies = LoadedAssemblies ?? new Dictionary<string, AppDomain>();
            Proxies = Proxies ?? new Dictionary<string, AssemblyReflectionProxy>();
        }

        /// <summary>
        /// Loads an assembly into a newly create temporary AppDomain.
        /// If the AppDomain exists in `MapDomains`, it will use the already existing one otherwise it will create a new one.
        /// </summary>
        /// <param name="assemblyPath">The system path to the assembly</param>
        /// <param name="domainName">The domainname which should be used for the temporary AppDomain</param>
        /// <param name="filter">The filter which will exlucde all other classes, than what's specified in the filter</param>
        public void LoadAssembly(string assemblyPath, string domainName, string filter)
        {
            // if the assembly file does not exist then fail
            if (!File.Exists(assemblyPath))
                throw new FileNotFoundException("Unable to locate the assembly in the given path");

            // if the assembly was already loaded then fail
            if (LoadedAssemblies.ContainsKey(assemblyPath))
                throw new TypeLoadException("Unable to load the assembly, because it's already loaded");

            // check if the appdomain exists, and if not create a new one
            AppDomain appDomain = null;
            if (MapDomains.ContainsKey(domainName))
            {
                appDomain = MapDomains[domainName];
            }
            else
            {
                appDomain = CreateChildDomain(AppDomain.CurrentDomain, domainName);
                MapDomains[domainName] = appDomain;
            }

            // load the assembly in the specified app domain
            Type proxyType = typeof(AssemblyReflectionProxy);
            if (proxyType.Assembly != null)
            {
                var proxy = (AssemblyReflectionProxy)appDomain.CreateInstanceFrom(proxyType.Assembly.Location, proxyType.FullName).Unwrap();

                proxy.LoadAssembly(assemblyPath, filter);

                LoadedAssemblies[assemblyPath] = appDomain;
                Proxies[assemblyPath] = proxy;
            }
            else
                throw new TypeLoadException("Unable to load the assembly. The proxys field `Assembly` was null");
        }

        /// <summary>
        /// Unloads an assembly from the temporary AppDomain.
        /// </summary>
        /// <param name="assemblyPath">The system path to the assembly</param>
        /// <returns>A boolean whenever the assembly was unloaded or not</returns>
        public bool UnloadAssembly(string assemblyPath)
        {
            if (!File.Exists(assemblyPath))
                return false;

            // check if the assembly is found in the internal dictionaries
            if (LoadedAssemblies.ContainsKey(assemblyPath) && Proxies.ContainsKey(assemblyPath))
            {
                // check if there are more assemblies loaded in the same app domain; in this case fail
                AppDomain appDomain = LoadedAssemblies[assemblyPath];
                int count = LoadedAssemblies.Values.Count(a => a == appDomain);
                if (count != 1)
                    return false;

                try
                {
                    // remove the appdomain from the dictionary and unload it from the process
                    MapDomains.Remove(appDomain.FriendlyName);
                    AppDomain.Unload(appDomain);

                    // remove the assembly from the dictionaries
                    LoadedAssemblies.Remove(assemblyPath);
                    Proxies.Remove(assemblyPath);

                    return true;
                }
                catch
                {
                }
            }

            return false;
        }

        /// <summary>
        /// Removes the temporary AppDomain from `MapDomains` and unloads every assembly which belongs to this domain
        /// </summary>
        /// <param name="domainName">The name of the domain which should be removed</param>
        /// <returns>A boolean whenever the AppDomain was unloaded or not</returns>
        public bool UnloadDomain(string domainName)
        {
            // check the appdomain name is valid and we have an instance of the domain
            if (string.IsNullOrEmpty(domainName) && !MapDomains.ContainsKey(domainName))
                return false;

            try
            {
                var appDomain = MapDomains[domainName];

                // check the assemblies that are loaded in this app domain
                var assemblies = new List<string>();
                foreach (var kvp in LoadedAssemblies)
                {
                    if (kvp.Value == appDomain)
                    {
                        // kvp.Key == AssemblyName
                        LoadedAssemblies.Remove(kvp.Key);
                        Proxies.Remove(kvp.Key);
                    }
                }

                // remove the appdomain from the dictionary
                MapDomains.Remove(domainName);

                // unload the appdomain
                AppDomain.Unload(appDomain);

                return true;
            }
            catch
            {
            }

            return false;
        }

        /// <summary>
        /// From within the temporary AppDomain, use reflection, so the types are known to the anonymous function
        /// </summary>
        /// <param name="assemblies">A list containing the system path for all the assemblies in the domain</param>
        /// <param name="func">The anonymous function</param>
        /// <returns>A DataStore object which holds the information return by `func`</returns>
        public DataStore Reflect(List<string> assemblies, AssemblyMetaData metaData, Func<Assembly, Type, DataStore> func)
        {
            var r = new DataStore { Data = new Dictionary<string, Util.CommonObject>() };
            foreach (var assemblyPath in assemblies)
            {
                // check if the assembly is found in the internal dictionaries
                if (LoadedAssemblies.ContainsKey(assemblyPath) && Proxies.ContainsKey(assemblyPath))
                {
                    try
                    {
                        var result = Proxies[assemblyPath].Reflect(func);
                        foreach (var item in result.Data)
                        {
                            if (!r.Data.ContainsKey(item.Key))
                                r.Data.Add(item.Key, item.Value);
                        }
                        metaData.AssemblySuccess.Add(assemblyPath);
                    }
                    catch (FileNotFoundException)
                    {
                        //errors here usually occurs when the application is unable to locate an assemblys dependencies or an dependency, dependencies
                        metaData.AssemblyErrors.Add(assemblyPath);
                    }
                }
            }

            return r;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var appDomain in MapDomains.Values)
                    AppDomain.Unload(appDomain);

                LoadedAssemblies.Clear();
                Proxies.Clear();
                MapDomains.Clear();
            }
        }

        private AppDomain CreateChildDomain(AppDomain parentDomain, string domainName)
        {
            Evidence evidence = new Evidence(parentDomain.Evidence);
            AppDomainSetup setup = parentDomain.SetupInformation;
            return AppDomain.CreateDomain(domainName, evidence, setup);
        }
    }
}
