using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuestExecutor.Observability.Logging
{
    public static class LogEvents
    {
        public static readonly string RequestStart = nameof(RequestStart);
        public static readonly string RequestInvalid = nameof(RequestInvalid);
        public static readonly string ExecutorResolved = nameof(ExecutorResolved);
        public static readonly string AttemptStart = nameof(AttemptStart);
        public static readonly string AttemptResult = nameof(AttemptResult);
        public static readonly string RequestSuccess = nameof(RequestSuccess);
        public static readonly string RequestFailure = nameof(RequestFailure);
        public static readonly string HttpOutbound = nameof(HttpOutbound);
        public static readonly string PsInvoke = nameof(PsInvoke);
    }
}
