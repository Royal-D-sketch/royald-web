using System;
using System.IO;
using System.Text;

class Program {
    static void Main() {
        string path = ""RoyalD.Web/Views/SalesBill/Detail.cshtml"";
        string content = File.ReadAllText(path, Encoding.UTF8);
        int start = content.IndexOf(""@if (isInstallment)"");
        int end = content.IndexOf(""</div>"", start);
        string oldBlock = content.Substring(start, end - start);
        string newBlock = ""@if (debt != null) { <span class=\""badge bg-primary ms-2 px-3 py-1 fs-6 shadow-sm\""><i class=\""bi bi-info-circle-fill me-1\""></i> ʶҹ�: @(debt.Status.ToThaiString())</span> }\r\n        "";
        content = content.Replace(oldBlock, newBlock);
        File.WriteAllText(path, content, new UTF8Encoding(true));
    }
}
