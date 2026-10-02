using System;
using System.Collections.Generic;
using System.Linq;

namespace RoyalD.Web.Services
{
    public static class SalesRepHelper
    {
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
                    }

                    return false;
                }).ToList();

                if (fuzzyMatches.Count > 0)
                {
                    foreach (var m in fuzzyMatches) matched.Add(m);
                }
                else
                {
                    matched.Add(input);
                }
            }

            return matched.OrderBy(s => s).ToList();
        }

        /// <summary>
        /// Validates and resolves the effective sales rep(s) to query:
        /// - If user is restricted and has assigned reps:
        ///   - If selectedRep is specified and matches one of the assigned reps -> filters by that rep only.
        ///   - Otherwise (empty/null or non-matching) -> filters by ALL assigned reps.
        /// - If user is not restricted:
        ///   - If selectedRep is specified -> filters by selectedRep.
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

            if (isRestrictedWithAssigned)
            {
                if (!string.IsNullOrWhiteSpace(requestedSalesRep))
                {
                    var reqTrim = requestedSalesRep.Trim();
                    var matchedReq = assignedReps.FirstOrDefault(r => 
                        string.Equals(r, reqTrim, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(r.Replace(" ", ""), reqTrim.Replace(" ", ""), StringComparison.OrdinalIgnoreCase) ||
                        r.IndexOf(reqTrim, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        reqTrim.IndexOf(r, StringComparison.OrdinalIgnoreCase) >= 0);

                    if (matchedReq != null)
                    {
                        return (new List<string> { matchedReq }, matchedReq, hasMultipleAssigned);
                    }
                }

                // If no specific rep requested or not in allowed list: filter by all assigned reps
                return (assignedReps, "", hasMultipleAssigned);
            }
            else
            {
                // Not restricted or has no assigned reps
                if (!string.IsNullOrWhiteSpace(requestedSalesRep))
                {
                    return (new List<string> { requestedSalesRep.Trim() }, requestedSalesRep.Trim(), false);
                }
                return (null, "", false);
            }
        }
    }
}
