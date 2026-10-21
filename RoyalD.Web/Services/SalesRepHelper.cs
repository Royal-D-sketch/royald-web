using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using RoyalD.Web.Models;

namespace RoyalD.Web.Services
{
    public static class SalesRepHelper
    {
        /// <summary>
        /// Code-to-names mapping for sales rep codes (e.g. 121.1 -> สัญญา, สัณห์, etc.)
        /// </summary>
        public static readonly Dictionary<string, List<string>> CodeToNamesMap = new(StringComparer.OrdinalIgnoreCase)
        {
            ["121.1"] = new() { "สัญญา", "สัณห์", "สัญญา LN", "สัญญา N", "สิริญญา", "ชาติวุธ N" },
            ["124.0"] = new() { "วีรนุช", "วีรนุช 8", "วีรนุช T" },
            ["115.0"] = new() { "จันทิมา" },
            ["155.0"] = new() { "ธัญญ์ลักษณ์" },
            ["ธัญลักษณ์"] = new() { "ธัญญ์ลักษณ์" },
            ["ธัญญลักษณ์"] = new() { "ธัญญ์ลักษณ์" },
            ["ธัญญ์ลัษณ์"] = new() { "ธัญญ์ลักษณ์" },
            ["158.0"] = new() { "วีระศักดิ์", "วีรนุช T" },
            ["150.0"] = new() { "อภิญญา" },
            ["146.0"] = new() { "ธนะศักดิ์", "ธนาศักดิ์" },
            ["145.0"] = new() { "ทินภัทร", "ปานชลี" },
            ["129.2"] = new() { "ชุลีวรรณ W", "ชุลีวรรณ", "สัญญา T" },
            ["125.1"] = new() { "น้ำเพชร", "ณัฐนันท์", "จิตติศักดิ์", "บุญเสริม", "วัชรี" },
            ["114.1"] = new() { "ณัฐนันท์" },
            ["132.1"] = new() { "วัชรี" },
            ["125.2"] = new() { "บุญเสริม", "บุญเสริม T" },
            ["985.1"] = new() { "จิตติศักดิ์" }
        };

        public static bool IsCode(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return false;
            var t = s.Trim();
            return System.Text.RegularExpressions.Regex.IsMatch(t, @"^[\d\.]+$");
        }

        /// <summary>
        /// Retrieves all unique sales reps across SalesBills, OutstandingDebts, and configured AppUsers.
        /// Results are filtered from junk items, cached for 10 minutes, and ordered.
        /// </summary>
        public static async Task<List<string>> GetAllKnownSalesRepsAsync(AppDbContext db, IMemoryCache cache)
        {
            return await cache.GetOrCreateAsync("all_known_sales_reps_v1", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);

                var billReps = await db.SalesBills.AsNoTracking()
                    .Where(b => !string.IsNullOrEmpty(b.SalesRep))
                    .Select(b => b.SalesRep!)
                    .Distinct()
                    .ToListAsync();

                var debtReps = await db.OutstandingDebts.AsNoTracking()
                    .Where(d => !string.IsNullOrEmpty(d.SalesRep))
                    .Select(d => d.SalesRep!)
                    .Distinct()
                    .ToListAsync();

                var userReps = await db.Users.AsNoTracking()
                    .Where(u => !string.IsNullOrEmpty(u.SalesRepCode))
                    .Select(u => u.SalesRepCode!)
                    .ToListAsync();

                var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                void AddIfValid(string? rep)
                {
                    if (string.IsNullOrWhiteSpace(rep)) return;
                    var s = rep.Trim();
                    if (s.Contains("5%") ||
                        s.Contains("page", StringComparison.OrdinalIgnoreCase) ||
                        s.Contains("หน้า", StringComparison.OrdinalIgnoreCase) ||
                        s.Contains("สัญญาลักษณ์") ||
                        IsCode(s))
                    {
                        return;
                    }
                    if (s == "ธัญลักษณ์" || s == "ธัญญลักษณ์" || s == "ธัญญ์ลัษณ์")
                    {
                        s = "ธัญญ์ลักษณ์";
                    }
                    set.Add(s);
                }

                foreach (var r in billReps) AddIfValid(r);
                foreach (var r in debtReps) AddIfValid(r);
                foreach (var rawCode in userReps)
                {
                    if (string.IsNullOrWhiteSpace(rawCode)) continue;
                    var parts = rawCode.Split(new[] { ',', ';', '/', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var p in parts) AddIfValid(p);
                }

                return set.OrderBy(s => s).ToList();
            }) ?? new List<string>();
        }
        /// <summary>
        /// Cleans Thai string for comparison (removes titles, garun, whitespace, lowercase)
        /// </summary>
        public static string CleanThai(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            var res = s.Replace("คุณ", "").Replace("นาย", "").Replace("นางสาว", "").Replace("นาง", "").Replace("น.ส.", "");
            res = res.Replace("์", ""); // remove garun
            res = res.Replace(" ", "").Trim().ToLower();
            return res;
        }

        /// <summary>
        /// Parses assigned sales rep codes/names from user configuration (comma/semicolon/slash separated)
        /// and matches them against the distinct DB sales reps.
        /// </summary>
        public static List<string> GetAssignedSalesReps(string? salesRepCode, List<string>? allDbReps = null)
        {
            if (string.IsNullOrWhiteSpace(salesRepCode))
                return new List<string>();

            var rawInputs = salesRepCode
                .Split(new[] { ',', ';', '/', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (rawInputs.Count == 0)
                return new List<string>();

            if (allDbReps == null || allDbReps.Count == 0)
                return rawInputs;

            var matched = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var input in rawInputs)
            {
                if (CodeToNamesMap.TryGetValue(input, out var mappedFromCode))
                {
                    foreach (var m in mappedFromCode) matched.Add(m);
                    continue;
                }

                if (IsCode(input))
                {
                    // Numeric code without mapping: do NOT add code directly since SalesBills store names!
                    continue;
                }

                var exactMatches = allDbReps.Where(dbRep => 
                    string.Equals(dbRep, input, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(dbRep.Replace(" ", ""), input.Replace(" ", ""), StringComparison.OrdinalIgnoreCase)
                ).ToList();

                if (exactMatches.Count > 0)
                {
                    foreach (var m in exactMatches) matched.Add(m);
                    continue;
                }

                var inputClean = CleanThai(input);
                var fuzzyMatches = allDbReps.Where(dbRep =>
                {
                    if (dbRep.IndexOf(input, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        input.IndexOf(dbRep, StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;

                    var dbClean = CleanThai(dbRep);
                    if (!string.IsNullOrEmpty(inputClean) && !string.IsNullOrEmpty(dbClean))
                    {
                        if (dbClean == inputClean || dbClean.StartsWith(inputClean) || inputClean.StartsWith(dbClean))
                            return true;

                        // Phonetic tolerance: ธนศักดิ์ vs ธนาศักดิ์
                        if ((inputClean.Contains("ธนศักดิ") || inputClean.Contains("ธนาศักดิ")) &&
                            (dbClean.Contains("ธนศักดิ") || dbClean.Contains("ธนาศักดิ")))
                            return true;

                        // Phonetic tolerance for ธัญญ์ลักษณ์
                        if (inputClean.StartsWith("ธัญ") && dbClean.StartsWith("ธัญ"))
                            return true;
                    }

                    return false;
                }).ToList();

                if (fuzzyMatches.Count > 0)
                {
                    foreach (var m in fuzzyMatches) matched.Add(m);
                }
                else
                {
                    if (!IsCode(input)) matched.Add(input);
                }
            }

            matched.RemoveWhere(s => s.Contains("สัญญาลักษณ์"));
            return matched.OrderBy(s => s).ToList();
        }

        /// <summary>
        /// Validates and resolves the effective sales rep(s) to query:
        /// - If user is restricted and has assigned reps:
        ///   - If selectedRep is specified and matches one of the assigned reps -> filters by that rep only.
        ///   - If selectedRep is a code (e.g. 121.1) -> maps to real rep names and matches assigned reps.
        ///   - Otherwise (empty/null or non-matching) -> filters by ALL assigned reps.
        /// - If user is not restricted:
        ///   - If selectedRep is specified -> filters by selectedRep (or mapped names if it's a code).
        ///   - If empty -> no sales rep filter (null).
        /// Returns (FilterReps, SelectedRepForUi, HasMultipleAssigned).
        /// </summary>
        public static (List<string>? FilterReps, string? SelectedRepForUi, bool HasMultipleAssigned) ResolveFilter(
            bool isRestricted, 
            string? userSalesRepCode, 
            string? requestedSalesRep, 
            List<string> allDbReps)
        {
            var assignedReps = isRestricted && !string.IsNullOrWhiteSpace(userSalesRepCode)
                ? GetAssignedSalesReps(userSalesRepCode, allDbReps)
                : new List<string>();

            bool isRestrictedWithAssigned = isRestricted && assignedReps.Count > 0;
            bool hasMultipleAssigned = isRestrictedWithAssigned && assignedReps.Count > 1;

            if (!string.IsNullOrWhiteSpace(requestedSalesRep))
            {
                var reqTrim = requestedSalesRep.Trim();
                if (reqTrim == "ธัญลักษณ์" || reqTrim == "ธัญญลักษณ์" || reqTrim == "ธัญญ์ลัษณ์")
                {
                    reqTrim = "ธัญญ์ลักษณ์";
                }

                // If user selected or entered a code (e.g. 121.1), map to actual rep names
                List<string>? targetNames = null;
                if (CodeToNamesMap.TryGetValue(reqTrim, out var codeMapped))
                {
                    targetNames = codeMapped;
                }
                else if (!IsCode(reqTrim))
                {
                    targetNames = new List<string> { reqTrim };
                }

                if (isRestrictedWithAssigned)
                {
                    if (targetNames != null)
                    {
                        var matchedFromAssigned = assignedReps.Where(r => 
                            targetNames.Any(tn => 
                                string.Equals(r, tn, StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(r.Replace(" ", ""), tn.Replace(" ", ""), StringComparison.OrdinalIgnoreCase) ||
                                r.IndexOf(tn, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                tn.IndexOf(r, StringComparison.OrdinalIgnoreCase) >= 0)).ToList();

                        if (matchedFromAssigned.Count > 0)
                        {
                            return (matchedFromAssigned, matchedFromAssigned.First(), hasMultipleAssigned);
                        }
                    }

                    // Fallback to all assigned reps
                    return (assignedReps, "", hasMultipleAssigned);
                }
                else
                {
                    // Not restricted or has no assigned reps
                    if (targetNames != null && targetNames.Count > 0)
                    {
                        return (targetNames, targetNames.First(), false);
                    }
                    return (null, "", false);
                }
            }
            else
            {
                if (isRestrictedWithAssigned)
                {
                    return (assignedReps, "", hasMultipleAssigned);
                }
                return (null, "", false);
            }
        }
    }
}
