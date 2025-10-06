using Cysharp.Threading.Tasks;
using System.Runtime.CompilerServices;
using Exerussus._1Extensions.MicroserviceFeature;

namespace Exerussus.Microservices.Runtime
{
    public readonly struct ServiceHandle
    {
        internal ServiceHandle(int id)
        {
            Id = id;
        }

        internal readonly int Id;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool GetIsValid() { return MicroservicesApi.GetIsValid(Id);}
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Unregister() => MicroservicesApi.UnregisterService(Id);

        public async UniTask Push<T>(T broadcast) where T : IChannel
        {
            await MicroservicesApi.PushBroadcast(Id, broadcast);
        }
        
        public async UniTask<(bool success, TResponse response)> PushCommand<TRequest, TResponse>(TRequest broadcast = default) where TRequest : ICommand<TResponse>
        {
            return await MicroservicesApi.PushCommand<TRequest, TResponse>(Id, broadcast);
        }
        
        public async UniTask Push<T>() where T : struct, IChannel
        {
            await MicroservicesApi.PushBroadcast(Id, new T());
        }

        public override string ToString()
        {
            return $"Service Handle {Id} id";
        }
    }
}