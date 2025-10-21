using Cysharp.Threading.Tasks;
using System.Runtime.CompilerServices;
using Exerussus._1Extensions.MicroserviceFeature;
using NUnit.Framework.Internal.Commands;

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

        public async UniTask PushAsync<T>(T broadcast) where T : IChannel
        {
            await MicroservicesApi.PushBroadcastAsync(Id, broadcast);
        }
        
        public async UniTask<(bool success, TResponse response)> PushCommandAsync<TRequest, TResponse>(TRequest broadcast = default) where TRequest : ICommand<TResponse>
        {
            return await MicroservicesApi.PushCommandAsync<TRequest, TResponse>(Id, broadcast);
        }
        
        public async UniTask PushAsync<T>() where T : struct, IChannel
        {
            await MicroservicesApi.PushBroadcastAsync(Id, new T());
        }

        public void Push<T>(T broadcast) where T : IChannel
        {
            MicroservicesApi.PushBroadcast(Id, broadcast);
        }
        
        public (bool success, TResponse response) PushCommand<TRequest, TResponse>(TRequest broadcast = default) where TRequest : ICommand<TResponse>
        {
            return MicroservicesApi.PushCommand<TRequest, TResponse>(Id, broadcast);
        }
        
        public void Push<T>() where T : IChannel, new()
        {
            MicroservicesApi.PushBroadcast(Id, new T());
        }

        public CommandMatcher<TCommand, TResponse> Match<TCommand, TResponse>() where TCommand : ICommand<TResponse>
        {
            return new CommandMatcher<TCommand, TResponse>(this);
        }

        public void Match<TCommand, TResponse>(ref CommandMatcher<TCommand, TResponse> matcher) where TCommand : ICommand<TResponse>
        {
            matcher = new CommandMatcher<TCommand, TResponse>(this);
        }
        
        public override string ToString()
        {
            return $"Service Handle {Id} id";
        }
    }
}