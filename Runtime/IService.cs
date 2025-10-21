﻿using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Exerussus._1Extensions.MicroserviceFeature;
using Exerussus.Microservices.Runtime.Registration;

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
    
    public interface IChannelPuller<in TChannel> where TChannel : IChannel
    {
        void PullBroadcast(TChannel channel);
    }
    
    public interface ICommandPuller<in TCommand, TResponse> where TCommand : ICommand<TResponse>
    {
        TResponse PullBroadcast(TCommand channel);
    }
    
    public interface IChannelPullerAsync<in TChannel> where TChannel : IChannel
    {
        UniTask PullBroadcastAsync(TChannel channel);
    }
    
    public interface ICommandPullerAsync<in TCommand, TResponse> where TCommand : ICommand<TResponse>
    {
        UniTask<TResponse> PullBroadcastAsync(TCommand channel);
    }
    
    public interface IChannelPusher<in TChannel>
        where TChannel : IChannel
    {
        
    }
    
    public interface ICommandPusher<in TCommand, TResponse>
        where TCommand : ICommand<TResponse>
    {
        
    }
    
    public interface IChannelPusherAsync<in TChannel>
        where TChannel : IChannel
    {
        
    }
    
    public interface ICommandPusherAsync<in TCommand, TResponse>
        where TCommand : ICommand<TResponse>
    {
        
    }
    
    public interface IServiceInspector : IService
    {
        public Dictionary<Type, object> AsyncChannelsSubs {get; }
        public Dictionary<int, RegisteredService> RegisteredServices {get; }
        public Dictionary<int, HashSet<Type>> AsyncPushersToChannels {get; }
        public Dictionary<Type, HashSet<int>> AsyncChannelsToPullers {get; }
        Dictionary<int, HashSet<Type>> PushersToChannels { get; }
        Dictionary<Type, HashSet<int>> ChannelsToPullers { get; }
        Dictionary<Type, object> ChannelsSubs { get; }

        public virtual void OnServiceRegistered(RegisteredService registeredService) {}
        public virtual void OnServiceUnregistered(RegisteredService registeredService) {}
    }
}