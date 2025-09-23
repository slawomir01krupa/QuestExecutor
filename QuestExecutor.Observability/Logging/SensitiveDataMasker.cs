using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace QuestExecutor.Observability.Logging
{
    public static class SensitiveDataMasker
    {
        private static readonly Regex Bearer = new(@"Bearer\s+[A-Za-z0-9\-\._~\+\/]+=*", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex ApiKey = new(@"(?i)(x\-api\-key|apikey|api\-key)\s*[:=]\s*([A-Za-z0-9\-\._~]+)", RegexOptions.Compiled);

        public static string Mask(string? s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            var m = Bearer.Replace(s, "Bearer ****");
            m = ApiKey.Replace(m, "$1: ****");
            return m;
        }

        public static IDictionary<string, string> MaskHeaders(IDictionary<string, string>? headers)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (headers == null) return result;
            foreach (var (k, v) in headers)
            {
                if (string.Equals(k, "Authorization", StringComparison.OrdinalIgnoreCase) ||
                    k.Contains("Api-Key", StringComparison.OrdinalIgnoreCase))
                    result[k] = "****";
                else
                    result[k] = v ?? "";
            }
            return result;
        }
    }
}
