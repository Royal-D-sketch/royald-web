using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

var file = ""Services/ReportService.cs"";
var text = File.ReadAllText(file);
var matches = Regex.Matches(text, @""[ก-ฮ]"");
Console.WriteLine($""Total Thai chars: {matches.Count}"");
