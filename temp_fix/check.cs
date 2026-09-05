using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using RoyalD.Web.Models;

var opt = new DbContextOptionsBuilder<AppDbContext>();
opt.UseNpgsql(""Host=aws-0-ap-southeast-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.pssccxujypweaahkbvdw;Password=029030445Rd*;Pooling=true;Maximum Pool Size=100;SSL Mode=Require;Trust Server Certificate=true;"");
using var db = new AppDbContext(opt.Options);

var b1 = db.SalesBills.FirstOrDefault(b => b.BillNo == ""R153152"");
var b2 = db.SalesBills.FirstOrDefault(b => b.BillNo == ""R152858"");

Console.WriteLine($""R153152: Phone='{b1?.Phone}', Credit={b1?.Credit}"");
Console.WriteLine($""R152858: Phone='{b2?.Phone}', Credit={b2?.Credit}"");