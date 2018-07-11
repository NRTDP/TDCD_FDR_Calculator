# TDCD_FDR_Calculator
The TDCD_FDR_CALCULATOR provides a context-dependent FDR calculation that can be applied post-search to enhance the quality of results in top-down proteomics from any search engine.

## Dependencies
This tool is provided as a [.NET Core](http://www.dot.net) project and, as such, 
supports Windows, macOS, and most Linux distributions. You must install the .NET 
SDK that works with your system:

[Windows](https://www.microsoft.com/net/learn/get-started/windows)

[macOS](https://www.microsoft.com/net/learn/get-started/macos)

[Linux](https://www.microsoft.com/net/learn/get-started/linux)

## Example Usage on Windows
After [cloning the github repository locally](https://help.github.com/articles/cloning-a-repository/), 
you should run the following commands from the TDCD_FDR_CALCULATOR project directory 
(contains the .csproj file).
```bash
dotnet build
dotnet .\bin\Debug\netcoreapp2.0\TDCD_FDR_Calculator.dll
```
This will display usage information for the tool. To run the example files, use the
following command:
```bash
dotnet .\bin\Debug\netcoreapp2.0\TDCD_FDR_Calculator.dll ..\..\examples\target.csv ..\..\examples\decoy.csv ..\..\examples\output.csv
```
## File Format
The input files must be CSV files where the first column is a generic text tag 
and the second column is a score (where larger is better, sorted ascending). 
The output CSV will be the same as the forward input file, 
but with two additional columns: non-parametric q-value and Enhanced q-value.