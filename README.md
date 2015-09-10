# AsmDiff.NET

This tool is for comparing .NET assemblies between different versions of the assemblies. It was developed to generate a report of what have changed between two releases of a software product.
This can be useful if you have external components depending on your library, and you want to catch the breaking changes before disaster strikes.

AsmDiff.NET is able to scan for

- Additions
  * New properties added
  * New classes added
- Changes
  * Renamed properties
  * Changed datatypes
 - Deletions
   * Removed classes
   * Removed properties

## Install
The application is a single binary file and a folder containing the HTML template.
\
You can find the latest release under the release tab above.

## Build dependencies
- xxHashSharp (https://github.com/noricube/xxHashSharp)
- ILMerge (http://www.microsoft.com/en-us/download/details.aspx?id=17630)

## Reports
The outputformat of the report are either in HTML or JSON, which you are able to specify from one of the command arguments.
Be default there are 2 color themes, a dark and a light one.
If neither of the default ones satisfies your needs, you are able to create your own theme.

Examples of the HTML reports

[DARK](https://github.com/KLIM8D/AsmDiff.NET/example-report-dark.gif)

[LIGHT](https://github.com/KLIM8D/AsmDiff.NET/example-report-light.gif)


## Usage
```
Usage: AsmDiffNET [OPTIONS]
 Options:
  -h, --help                 show this message and exit
  
  -s, --source=VALUE         this is the OLD version of the library. Either a path to a specific assembly or 
                             a folder which contains assemblies
                             
  -t, --target=VALUE         this is the NEW version of the library. Either a path to a specific assembly or 
                             a folder which contains assemblies
                             
  -f, --filter=VALUE         specify a filter which will exclude other classes which doesn't reference
                             what's specified in the filter (eg. System.Reflection.Assembly)
                             
  -p, --pattern=VALUE        specify a regex pattern which will exclude all files which 
                             doesn't match the regular expression
                             
  -o, --output=VALUE         specify output format, JSON or HTML. Default: HTML
  
      --flags=VALUE          specify which kind of analysis you want the application to do.
                             Options: (a = Addtions, c = Changes, d = Deletions)
                             Ex. `flags=ad` will only search for and include
                             Additions and Deletions from the analysis of the assemblies.
                             Default: `flags=cd`
                             
      --theme=VALUE          specify either a filename within Assets\Themes
                             or a path to a CSS file.
                             Default options: light, dark
                             Default: `theme=light`
```

### Development

Want to contribute? Great!

Feel free to submit issues and pull requests.

### License
AsmDiff.NET is licensed under the MIT license. See [LICENSE](https://github.com/KLIM8D/AsmDiff.NET/blob/master/LICENSE.txt) for the full license text.