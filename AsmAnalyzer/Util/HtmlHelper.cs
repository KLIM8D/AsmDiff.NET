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
        public HtmlDocument HtmlFile { get; set; }
        public HtmlDocument TableTemplate { get; set; }

        public HtmlHelper(string theme)
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

            var baseFile = File.ReadAllLines(String.Format(@"{0}\Assets\base.html", Environment.CurrentDirectory));
            string baseHtml = String.Join("", baseFile).Replace("{{DATE}}", DateTime.Now.ToString("dd-MM-yyyy HH:mm"));
            baseHtml = baseHtml.Replace("{{CSS}}", themeCss);

            TableTemplate = new HtmlDocument();
            var tableFile = File.ReadAllLines(String.Format(@"{0}\Assets\table.html", Environment.CurrentDirectory));
            TableTemplate.LoadHtml(String.Join("", tableFile));

            HtmlFile = new HtmlDocument();
            HtmlFile.LoadHtml(baseHtml);
        }

        public Stream RenderHTML(ICollection<Result> results)
        {
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
                HtmlFile.DocumentNode.SelectSingleNode("//body").ChildNodes.Add(div);
            }

            var stream = new MemoryStream();
            HtmlFile.Save(stream);

            return stream;
        }
    }
}
