using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Exerussus._1Extensions.MicroserviceFeature;
using Exerussus.Microservices.Runtime.Registration;
using UnityEngine.Scripting;

namespace Exerussus.Microservices.Runtime
{
    public interface IService
    {
        public ServiceHandle Handle { get; set; }
    }

    public interface IServiceRun
    {
        public void Run();
    }
    
    public interface IChannelPuller<in TChannel>
        where TChannel : IChannel
    {
        public UniTask PullBroadcast(TChannel channel);
        
        [Preserve]
        internal void RegisterPuller()
        {
            Func<TChannel, UniTask> pull = PullBroadcast;
            MicroservicesApi.RegisterPuller(this, pull);
        }
    }
    
    public interface IChannelPusher<in TChannel>
        where TChannel : IChannel
    {
        
    }
    
    public interface IServiceInspector
    {
        public Dictionary<Type, object> ChannelsSubs {get; set; }
        public Dictionary<int, RegisteredService> RegisteredServices {get; set; }
        public Dictionary<int, HashSet<Type>> PushersToChannels {get; set; }
        public Dictionary<Type, HashSet<int>> ChannelsToPullers {get; set; }

        public virtual void OnServiceRegistered(RegisteredService registeredService) {}
        public virtual void OnServiceUnregistered(RegisteredService registeredService) {}
    }
}