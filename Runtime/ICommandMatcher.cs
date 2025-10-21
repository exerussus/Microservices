using Cysharp.Threading.Tasks;
using Exerussus._1Extensions.MicroserviceFeature;

namespace Exerussus.Microservices.Runtime
{
    public class CommandMatcher<TRequest, TResponse> where TRequest : ICommand<TResponse>
    {
        public CommandMatcher(ServiceHandle serviceHandle)
        {
            ServiceHandle = serviceHandle;
        }

        internal ServiceHandle ServiceHandle { get; set; }
        
        public async UniTask<(bool success, TResponse response)> PushCommandAsync(TRequest broadcast = default)
        {
            return await MicroservicesApi.PushCommandAsync<TRequest, TResponse>(ServiceHandle.Id, broadcast);
        }
        
        public (bool success, TResponse response) PushCommand(TRequest broadcast = default)
        {
            return MicroservicesApi.PushCommand<TRequest, TResponse>(ServiceHandle.Id, broadcast);
        }
    }
}