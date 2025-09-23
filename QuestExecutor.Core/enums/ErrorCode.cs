using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuestExecutor.Core.enums
{
    public enum ErrorCode
    {
        None = 0,              
        InvalidSchema,         
        Unauthorized,          
        Forbidden,             
        NotFound,              
        Timeout,               
        RateLimited,           
        Upstream5xx,           
        TargetUnavailable,     
        CommandNotAllowlisted, 
        Unknown                
    }
}
