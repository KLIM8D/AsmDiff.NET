#region LICENSE
/*
The MIT License (MIT)
Copyright (c) 2015 Morten Klim Sørensen
See LICENSE.txt for more information
*/
#endregion
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace AsmAnalyzer
{
    public class AssemblyReflectionProxy : MarshalByRefObject
    {
        public string AssemblyPath { get; private set; }
        public string Filter { get; set; }

        public void LoadAssembly(string path, string filter)
        {
            try
            {
                AssemblyPath = path;
                Filter = filter;
                Assembly.ReflectionOnlyLoadFrom(AssemblyPath);
            }
            catch (FileNotFoundException)
            {
                // continue loading assemblies even if an assembly can not be loaded in the new AppDomain.
            }
        }

        /// <summary>
        /// Use reflection from within the temporary AppDomain
        /// The method will automaticly resolve an assemblys dependencies by using the eventhandler `ReflectionOnlyAssemblyResolve`
        /// </summary>
        /// <typeparam name="T">The type which is returned by `func`</typeparam>
        /// <param name="func">The anonymous function which is executed inside the AppDomain</param>
        /// <returns>The result from `func`</returns>
        public T Reflect<T>(Func<Assembly, Type, T> func)
        {
            var directory = new FileInfo(AssemblyPath).Directory;
            ResolveEventHandler resolveEventHandler = (s, e) =>
            {
                return OnReflectionOnlyResolve(e, directory);
            };

            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += resolveEventHandler;

            var assembly = AppDomain.CurrentDomain.ReflectionOnlyGetAssemblies().FirstOrDefault(a => a.Location.CompareTo(AssemblyPath) == 0);

            var t = GetFilter(AppDomain.CurrentDomain.ReflectionOnlyGetAssemblies());
            var result = func(assembly, t);

            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve -= resolveEventHandler;

            return result;
        }

        public Type GetFilter(ICollection<Assembly> assemblies)
        {
            if (String.IsNullOrEmpty(Filter))
                return null;

            foreach (var item in assemblies)
            {
                var t = item.GetType(Filter);
                if (t != null)
                    return t;
            }

            return null;
        }

        /// <summary>
        /// Used to resolve an assemblys dependencies
        /// </summary>
        /// <param name="args">Contains the name of the assembly</param>
        /// <param name="directory">The directory which contains the assembly</param>
        /// <returns>The assembly loaded into the current AppDomain</returns>
        private Assembly OnReflectionOnlyResolve(ResolveEventArgs args, DirectoryInfo directory)
        {
            Assembly loadedAssembly = AppDomain.CurrentDomain.ReflectionOnlyGetAssemblies()
                    .FirstOrDefault(asm => String.Equals(asm.FullName, args.Name, StringComparison.InvariantCultureIgnoreCase));

            if (loadedAssembly != null)
                return loadedAssembly;

            var assemblyName = new AssemblyName(args.Name);
            string dependentAssemblyFilename = Path.Combine(directory.FullName, assemblyName.Name + ".dll");

            Assembly r;
            if (File.Exists(dependentAssemblyFilename))
                r = Assembly.ReflectionOnlyLoadFrom(dependentAssemblyFilename);
            else
                r = Assembly.ReflectionOnlyLoad(args.Name);

            var dependencies = r.GetReferencedAssemblies().Where(x => 
                !AppDomain.CurrentDomain.GetAssemblies().Any(y => y.FullName == x.FullName) && 
                !AppDomain.CurrentDomain.ReflectionOnlyGetAssemblies().Any(y => y.FullName == x.FullName)).ToList();

            foreach (var dep in dependencies)
            {
                var p = Path.Combine(directory.FullName, dep.Name + ".dll");
                if (File.Exists(p))
                    Assembly.ReflectionOnlyLoadFrom(dependentAssemblyFilename);
                else
                    Assembly.ReflectionOnlyLoad(dep.FullName);
            }

            return r;
        }
    }
}
