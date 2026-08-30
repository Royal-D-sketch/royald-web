using Microsoft.EntityFrameworkCore;
using RoyalD.Web.Models;
using System.Linq;

var db = new AppDbContext();
var d = db.OutstandingDebts.First(x => x.CustomerCode == "110439");
System.Console.WriteLine($"Name: {d.CustomerName}, District: {d.District}, Province: {d.Province}");
