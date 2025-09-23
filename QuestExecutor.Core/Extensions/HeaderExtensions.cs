using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuestExecutor.Core.Extensions
{
    public static class HeaderDictionaryExtensions
    {
        public static string GetHeaderValue(this IHeaderDictionary headers, string headerName)
        {
            if (headers == null)
            {
                return string.Empty;
            }

            if (headers.TryGetValue(headerName, out var value))
            {
                return value.ToString();
            }

            return string.Empty;
        }
    }
}
