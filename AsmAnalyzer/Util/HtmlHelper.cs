#region LICENSE
/*
The MIT License (MIT)
Copyright (c) 2015 Morten Klim Sørensen
See LICENSE.txt for more information
*/
#endregion
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using WebMarkupMin.Yui.Minifiers;

namespace AsmAnalyzer.Util
{
    public class HtmlHelper
    {
        public HtmlDocument BaseTemplate { get; set; }
        public HtmlDocument TableTemplate { get; set; }
        public HtmlDocument MetaDataTemplate { get; set; }

        public HtmlHelper(string theme, string title)
        {
            //Read theme file and minify the CSS
            var ycm = new YuiCssMinifier();
            var themeFile = File.ReadAllLines(String.Format(@"{0}\Assets\Themes\{1}.css", Environment.CurrentDirectory, theme));
            var minified = ycm.Minify(String.Join("", themeFile), false);
            string themeCss;
            if (minified.Errors.Count == 0)
                themeCss = minified.MinifiedContent;
            else
                throw new FormatException("Unable to minify the provided CSS" + Environment.NewLine + String.Join("", minified.Errors));

            //Read the main.js file and minify the javascript
            var jsm = new YuiJsMinifier();
            var jsFile = File.ReadAllLines(String.Format(@"{0}\Assets\main.js", Environment.CurrentDirectory));
            var minJs = jsm.Minify(String.Join("", jsFile), false);
            string mainJs = "";
            if (minJs.Errors.Count == 0)
                mainJs = minJs.MinifiedContent;


            BaseTemplate = LoadHtml(@"Assets\base.html", new TupleList<string, string>
            {
                {"{{CSS}}", themeCss},
                {"{{JAVASCRIPT}}", mainJs},
                {"{{TITLE}}", title},
                {"{{DATE}}", DateTime.Now.ToString("dd-MM-yyyy HH:mm")}
            });

            TableTemplate = LoadHtml(@"Assets\table.html");
            MetaDataTemplate  = LoadHtml(@"Assets\metadata.html");
        }

        private HtmlDocument LoadHtml(string path, TupleList<string, string> replace = null)
        {
            var template = new HtmlDocument();
            var htmlFile = String.Join("", File.ReadAllLines(String.Format(@"{0}\{1}", Environment.CurrentDirectory, path)));
            if(replace != null)
            {
                foreach (var item in replace)
                {
                    htmlFile = htmlFile.Replace(item.Item1, item.Item2);
                }
            }
            template.LoadHtml(String.Join("", htmlFile));

            return template;
        }

        public Stream RenderHTML(ICollection<Result> results, MetaData metaData)
        {
            RenderMetaData(metaData);
            var body = BaseTemplate.DocumentNode.SelectSingleNode("//body");
            body.ChildNodes.Add(MetaDataTemplate.DocumentNode);

            int i = 0;
            foreach (var r in results)
            {
                var div = HtmlNode.CreateNode("<div></div>");
                div.CopyFrom(TableTemplate.DocumentNode.SelectSingleNode("//div"), false);
                div.InnerHtml = div.InnerHtml.Replace("{{CLASS_NAME}}", r.ClassName);
                foreach (var ri in r.Items)
                {
                    var beforeCol = ri.Before.Split(' ');
                    string before = beforeCol.Length > 1 ? String.Format("<span class=\"datatype\"> {0} </span>{1}", beforeCol[0], beforeCol[1]) : ri.Before;

                    var afterCol = ri.After.Split(' ');
                    string after = afterCol.Length > 1 ? String.Format("<span class=\"datatype\"> {0} </span>{1}", afterCol[0], afterCol[1]) : ri.After;

                    string type = String.Format("<span class=\"{0}\">{0}</span", ri.Type);
                    var node = HtmlNode.CreateNode(String.Format("<tr class=\"{0} child\"><td>{1}</td><td>{2}</td><td>{3}</td></tr>", i % 2 == 0 ? "even" : "odd", type, before, after));
                    div.SelectSingleNode("//table").ChildNodes.Add(node);
                    i++;
                }
                i = 0;
                body.ChildNodes.Add(div);
            }

            var stream = new MemoryStream();
            BaseTemplate.Save(stream);

            return stream;
        }

        public void RenderMetaData(MetaData meta)
        {
            MetaDataTemplate = LoadHtml(@"Assets\metadata.html", new TupleList<string, string>
            {
                {"{{FILTER}}", meta.Filter},
                {"{{PATTERN}}", meta.Pattern},
                {"{{FLAGS}}", meta.Flags},
                {"{{SOURCE}}", meta.Source.Path},
                {"{{TARGET}}", meta.Target.Path},
                {"{{CMDARGS}}", meta.CommandArguments}
            });

            int i = 0;
            FileInfo fileInfo;
            foreach (var srcAsm in meta.Source.AssemblySuccess)
            {
                fileInfo = new FileInfo(srcAsm);
                var node = HtmlNode.CreateNode(String.Format("<tr class=\"{0} child\"><td>{1}</td></tr>", i % 2 == 0 ? "even" : "odd", fileInfo.Name));
                MetaDataTemplate.GetElementbyId("source-assemblies-success").ChildNodes.Add(node);
                i++;
            }

            i = 1;
            foreach (var srcAsm in meta.Source.AssemblyErrors)
            {
                fileInfo = new FileInfo(srcAsm);
                var node = HtmlNode.CreateNode(String.Format("<tr class=\"{0} child\"><td>{1}</td></tr>", i % 2 == 0 ? "even" : "odd", fileInfo.Name));
                MetaDataTemplate.GetElementbyId("source-assemblies-errors").ChildNodes.Add(node);
                i++;
            }


            i = 0;
            foreach (var tarAsm in meta.Target.AssemblySuccess)
            {
                fileInfo = new FileInfo(tarAsm);
                var node = HtmlNode.CreateNode(String.Format("<tr class=\"{0} child\"><td>{1}</td></tr>", i % 2 == 0 ? "even" : "odd", fileInfo.Name));
                MetaDataTemplate.GetElementbyId("target-assemblies-success").ChildNodes.Add(node);
                i++;
            }

            i = 1;
            foreach (var tarAsm in meta.Target.AssemblyErrors)
            {
                fileInfo = new FileInfo(tarAsm);
                var node = HtmlNode.CreateNode(String.Format("<tr class=\"{0} child\"><td>{1}</td></tr>", i % 2 == 0 ? "even" : "odd", fileInfo.Name));
                MetaDataTemplate.GetElementbyId("target-assemblies-errors").ChildNodes.Add(node);
                i++;
            }
        }
    }
}
