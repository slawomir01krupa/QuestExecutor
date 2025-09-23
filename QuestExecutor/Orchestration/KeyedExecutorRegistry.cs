using QuestExecutor.Core.Abstractions;

namespace QuestExecutor.Api.Orchestration
{
    public class KeyedExecutorRegistry : IExecutorRegistry
    {
        private readonly IServiceProvider _sp;
        public KeyedExecutorRegistry(IServiceProvider sp) => _sp = sp;

        public IExecutor? Resolve(string executorType)
            => _sp.GetKeyedService<IExecutor>(executorType);
    }
}
