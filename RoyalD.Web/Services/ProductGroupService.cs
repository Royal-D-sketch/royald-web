using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace RoyalD.Web.Services
{
    public class ProductGroupDefinition
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public class ProductGroupItemInfo
    {
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public string GroupCode { get; set; } = "20";
        public string GroupName { get; set; } = "กลุ่ม อิสระ";
    }

    public class ProductGroupBundle
    {
        public List<ProductGroupDefinition>? groups { get; set; } = new();
        public Dictionary<string, ProductGroupItemInfo>? products { get; set; } = new();
    }

    public class ProductGroupService
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<ProductGroupService> _logger;
        private readonly Dictionary<string, ProductGroupItemInfo> _productMap = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<ProductGroupDefinition> _groups = new();

        public ProductGroupService(IWebHostEnvironment env, ILogger<ProductGroupService> logger)
        {
            _env = env;
            _logger = logger;
            LoadGroupData();
        }

        private void LoadGroupData()
        {
            try
            {
                var jsonPath = Path.Combine(_env.ContentRootPath, "Data", "product_groups.json");
                if (File.Exists(jsonPath))
                {
                    var jsonStr = File.ReadAllText(jsonPath);
                    var bundle = JsonSerializer.Deserialize<ProductGroupBundle>(jsonStr, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (bundle != null)
                    {
                        if (bundle.groups != null) _groups.AddRange(bundle.groups);
                        if (bundle.products != null)
                        {
                            foreach (var kvp in bundle.products)
                            {
                                _productMap[kvp.Key.Trim()] = kvp.Value;
                            }
                        }
                    }
                }

                if (!_groups.Any())
                {
                    _groups.AddRange(GetDefaultGroups());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load product group master data");
                if (!_groups.Any()) _groups.AddRange(GetDefaultGroups());
            }
        }

        public List<ProductGroupDefinition> GetAllGroups()
        {
            return _groups;
        }

        public static bool IsTesterProduct(string? code, string? name)
        {
            if (!string.IsNullOrWhiteSpace(code) && code.IndexOf("tester", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            if (!string.IsNullOrWhiteSpace(name))
            {
                if (name.IndexOf("tester", StringComparison.OrdinalIgnoreCase) >= 0 || name.Contains("เทสเตอร์"))
                    return true;
            }
            return false;
        }

        public bool IsTester(string? productCode, string? productName = "")
        {
            if (IsTesterProduct(productCode, productName)) return true;

            var cleanCode = (productCode ?? "").Trim();
            if (!string.IsNullOrEmpty(cleanCode) && _productMap.TryGetValue(cleanCode, out var info))
            {
                if (IsTesterProduct(info.ProductCode, info.ProductName)) return true;
            }

            var matchPrefix = _productMap.Keys.FirstOrDefault(k => !string.IsNullOrEmpty(cleanCode) && (cleanCode.StartsWith(k) || k.StartsWith(cleanCode)));
            if (matchPrefix != null && _productMap.TryGetValue(matchPrefix, out var pInfo))
            {
                if (IsTesterProduct(pInfo.ProductCode, pInfo.ProductName)) return true;
            }

            return false;
        }

        public ProductGroupItemInfo GetProductGroup(string productCode, string fallbackName = "", string fallbackUnit = "")
        {
            if (string.IsNullOrWhiteSpace(productCode))
            {
                return new ProductGroupItemInfo
                {
                    ProductCode = "-",
                    ProductName = fallbackName,
                    Unit = fallbackUnit,
                    GroupCode = "20",
                    GroupName = "กลุ่ม อิสระ"
                };
            }

            var cleanCode = productCode.Trim();
            if (_productMap.TryGetValue(cleanCode, out var info))
            {
                return info;
            }

            var matchPrefix = _productMap.Keys.FirstOrDefault(k => cleanCode.StartsWith(k) || k.StartsWith(cleanCode));
            if (matchPrefix != null && _productMap.TryGetValue(matchPrefix, out var pInfo))
            {
                return pInfo;
            }

            return new ProductGroupItemInfo
            {
                ProductCode = cleanCode,
                ProductName = !string.IsNullOrEmpty(fallbackName) ? fallbackName : cleanCode,
                Unit = !string.IsNullOrEmpty(fallbackUnit) ? fallbackUnit : "ชิ้น",
                GroupCode = "20",
                GroupName = "กลุ่ม อิสระ"
            };
        }

        private List<ProductGroupDefinition> GetDefaultGroups()
        {
            return new List<ProductGroupDefinition>
            {
                new() { Code = "01", Name = "กลุ่มเกลือแร่" },
                new() { Code = "02", Name = "กลุ่มแคลเซี่ยม,แคลซีตี-โกลด์" },
                new() { Code = "03", Name = "กลุ่มแคร์คอฟฟี่,รอยเยลคอฟฟี่" },
                new() { Code = "04", Name = "กลุ่มคอลลาเจน , แคลร์ โทลาเจน" },
                new() { Code = "05", Name = "กลุ่ม Ready to drink" },
                new() { Code = "06", Name = "กลุ่มเนเจอร์ไวท์" },
                new() { Code = "07", Name = "กลุ่มอาราบิก้า , ไวท์คอฟฟี่" },
                new() { Code = "08", Name = "กลุ่มเวย์โปรตีน" },
                new() { Code = "09", Name = "กลุ่มโยเกิร์ต" },
                new() { Code = "10", Name = "กลุ่มถังเช่า" },
                new() { Code = "11", Name = "กลุ่ม รอยเยล-ดี เจล" },
                new() { Code = "12", Name = "กลุ่มกาแฟดริป" },
                new() { Code = "13", Name = "กลุ่มนมอัดเม็ด (ชนิด 12)" },
                new() { Code = "14", Name = "กลุ่มกาแฟอาราบิก้าถั่วบดละเอียด" },
                new() { Code = "15", Name = "กลุ่มวิตามินซี" },
                new() { Code = "16", Name = "กลุ่มเครื่องดื่มวิตามินซี" },
                new() { Code = "17", Name = "กลุ่ม เครื่องดื่มรสชิงผสมเลมอน" },
                new() { Code = "18", Name = "กลุ่ม โพรไบโอติก" },
                new() { Code = "19", Name = "กลุ่มนมอัดเม็ด (ชนิด 10 กรัม)" },
                new() { Code = "20", Name = "กลุ่ม อิสระ" }
            };
        }
    }
}
