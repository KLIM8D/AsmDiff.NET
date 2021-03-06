﻿#region LICENSE
/*
The MIT License (MIT)
Copyright (c) 2015 Morten Klim Sørensen
See LICENSE.txt for more information
*/
#endregion
using AsmAnalyzer;
using AsmAnalyzer.Util;
using NDesk.Options;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.IO.Pipes;
using System.Text.RegularExpressions;

namespace AsmDiff.NET
{
    class Program
    {
        static OptionSet options;

        static void Main(string[] args)
        {
            #region OptionVariables
            string source  = "";
            string target  = "";
            string filter  = "";
            string pattern = "";
            string flags   = "";
            string theme   = "light";
            string title   = "AsmDiff.NET report";
            string filename = "";
            UInt16 maxdepth = 1;

            bool isHtml   = true;
            bool showHelp = false;
            #endregion

            options = new OptionSet
            {
                {"h|help", "show this message and exit", x => showHelp = true },
                {"s|source=", "this is the OLD version of the library. Either a path to a specific assembly or a folder which contains assemblies", src => source = src},
                {"t|target=", "this is the NEW version of the library. Either a path to a specific assembly or a folder which contains assemblies", tar => target = tar},
                {"f|filter=", "specify a filter which will exclude other classes which doesn't reference what's specified in the filter (eg. System.Reflection.Assembly) ", f => filter = f},
                {"p|pattern=", "specify a regex pattern which will exclude all files which doesn't match the regular expression ", p => pattern = p},
                {"o|output=", "specify output format, JSON or HTML. Default: HTML", h => isHtml = !String.Equals(h.ToUpperInvariant(), "JSON") },
                {"flags=",  "specify which kind of analysis you want the application to do." + Environment.NewLine +
                            "Options: (a = Addtions, c = Changes, d = Deletions)" + Environment.NewLine +
                            "Ex. `flags=ad` will only search for and include Additions and Deletions from the analysis of the assemblies." + Environment.NewLine +
                            "Default: `flags=cd`", fl => flags = fl },
                {"theme=", "specify either a filename within Assets\\Themes or a path to a CSS file. " + Environment.NewLine +
                            "Default options: light, dark" + Environment.NewLine +
                            "Default: `theme=light`", th => theme = th},
                {"title=", "the given title will be displayed at the top of the HTML report", t => { if(!String.IsNullOrEmpty(t)) title = t; } },
                {"maxdepth=", "descend at most levels (a non-negative integer) levels of directories below the current", m => { if(UInt16.TryParse(m, out maxdepth)); } },
                {"filename=", "the name the tool will use for naming the result file, excluding the file extension", f => { if(!String.IsNullOrEmpty(f)) filename = f; } }
            };


            try
            {
                options.Parse(args);

                if (showHelp)
                {
                    Help();
                    return;
                }

                #region HandleArguments
                // handle commandarguments
                bool sourceIsNull = false;
                if ((sourceIsNull = String.IsNullOrEmpty(source)) || String.IsNullOrEmpty(target))
                    throw new OptionException(String.Format("{0} cannot be empty. You have to specify a {0}", sourceIsNull ? "source" : "target"), sourceIsNull ? "source" : "target");

                // verify that the files or directory in source and target exist
                if ((sourceIsNull = !Exists(source)) || !Exists(target))
                    throw new OptionException(String.Format("the program were unable to find the path specified in {0} ({1})", sourceIsNull ? "source" : "target", sourceIsNull ? source : target),
                        sourceIsNull ? "source" : "target");

                source = Path.GetFullPath(source);
                target = Path.GetFullPath(target);

                Regex regex = null;
                if (IsValidRegex(pattern))
                    regex = new Regex(@pattern, RegexOptions.Compiled);
                else if(!String.IsNullOrEmpty(pattern))
                    throw new OptionException(String.Format("the pattern specified is not a valid regular expression {0}", Environment.NewLine + pattern), "pattern");

                var fl = !String.IsNullOrEmpty(flags) ? GetFlags(flags) : (Analyzer.AnalyzerFlags.Changes | Analyzer.AnalyzerFlags.Deletion);

                if(!Exists(theme) && !Exists(String.Format(@"{0}\Assets\Themes\{1}.css", Environment.CurrentDirectory, theme)))
                    throw new OptionException(String.Format("invalid path or file does not exists {0}", Environment.NewLine + theme), "theme");

                #endregion

                var metaData = new MetaData
                {
                    Pattern = pattern,
                    Filter = filter,
                    Flags = GetFlagsString(flags),
                    CommandArguments = String.Join(" ", args),
                    Source = new AssemblyMetaData { Path = source, AssemblyErrors = new List<string>(), AssemblySuccess = new List<string>() },
                    Target = new AssemblyMetaData { Path = target, AssemblyErrors = new List<string>(), AssemblySuccess = new List<string>() }
                };
                var analyzer = new Analyzer { Flags = fl, MaxDepth = maxdepth };
                var s = analyzer.Invoke(@source, @target, @filter, regex, metaData);

                #region RenderOutput
                // is the outputformat HTML or not
                if (isHtml)
                {
                    var htmlHelper = new HtmlHelper(theme, title);
                    var html = htmlHelper.RenderHTML(s, metaData);
                    string filepath = String.Format(@"{0}\{1}.html", Environment.CurrentDirectory, !String.IsNullOrEmpty(filename) ? filename : ("AssemblyScanReport-" + DateTime.Now.ToString("dd-MM-yyyy_HH-mm")));
                    using (var fileStream = File.Create(filepath))
                    {
                        html.Seek(0, SeekOrigin.Begin);
                        html.CopyTo(fileStream);
                    }

                    SendMessageAsync(filename, filepath);
                }
                else
                {
                    var json = JsonHelper.SerializeJson<ICollection<Result>>(s);
                    string filepath = String.Format(String.Format(@"{0}\{1}.json", Environment.CurrentDirectory, !String.IsNullOrEmpty(filename) ? filename : ("AssemblyScanReport-" + DateTime.Now.ToString("dd-MM-yyyy_HH-mm"))));
                    File.WriteAllText(filepath, json);

                    SendMessageAsync(filename, filepath);
                }
                #endregion
            }
            catch (OptionException e)
            {
                Console.Error.Write("AsmDiff.NET: ");
                Console.Error.WriteLine(e.Message);
                Console.Error.WriteLine("Try 'AsmDiffNET --help' for more information.");

#if DEBUG
                //Console.ReadLine();
#endif
                return;
            }
            catch (Exception e)
            {
                Console.Error.Write("AsmDiff.NET: An unknown error occurred. Please see the CrashDump file for further information.");
                string fileName = String.Format(String.Format(@"{0}\CrashDump-{1}.txt", Environment.CurrentDirectory, DateTime.Now.ToString("dd-MM-yyyy_HH-mm")));
                File.WriteAllText(fileName, e.ToString());
            }
        }

        static void Help()
        {
            Console.WriteLine("Usage: AsmDiffNET [OPTIONS]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            options.WriteOptionDescriptions(Console.Out);
        }

        static bool Exists(string name)
        {
            return (Directory.Exists(name) || File.Exists(name));
        }

        static bool IsValidRegex(string pattern)
        {
            if (String.IsNullOrEmpty(pattern)) 
                return false;

            try
            {
                Regex.Match("", pattern);
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }

            return true;
        }

        static Analyzer.AnalyzerFlags GetFlags(string flags)
        {
            flags = flags.ToUpperInvariant();
            Analyzer.AnalyzerFlags r = Analyzer.AnalyzerFlags.None;

            if (flags.Contains("A"))
                r = r | Analyzer.AnalyzerFlags.Addition;
            if (flags.Contains("C"))
                r = r | Analyzer.AnalyzerFlags.Changes;
            if (flags.Contains("D"))
                r = r | Analyzer.AnalyzerFlags.Deletion;

            return r;
        }

        static string GetFlagsString(string flags)
        {
            if (String.IsNullOrEmpty(flags))
                return "Changes Deletions";

            flags = flags.ToUpperInvariant();
            string r = "";

            if (flags.Contains("A"))
                r += "Additions ";
            if (flags.Contains("C"))
                r += "Changes ";
            if (flags.Contains("D"))
                r += "Deletions ";

            return r;
        }

        static async void SendMessageAsync(string pipeName, string message)
        {
            try
            {
                using (var pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.Out, PipeOptions.Asynchronous))
                using (var stream = new StreamWriter(pipe))
                {
                    pipe.Connect(2000);

                    // write the message to the pipe stream 
                    await stream.WriteAsync(message);
                }
            }
            catch (Exception)
            {
                Console.Error.WriteLine("Error connecting to the pipe: " + pipeName);
            }
        }
    }
}
