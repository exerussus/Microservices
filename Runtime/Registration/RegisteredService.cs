using System;
using System.Collections.Generic;

namespace Exerussus.Microservices.Runtime.Registration
{
    public class RegisteredService : IDisposable
    {
        public RegisteredService(int id, IService service, 
            Type[] pullAsyncChannels, 
            Type[] pushAsyncChannels, 
            Type[] pullAsyncCommands, 
            Type[] pushAsyncCommands, 
            Type[] pullChannels, 
            Type[] pushChannels, 
            Type[] pullCommands, 
            Type[] pushCommands)
        {
            Id = id;
            Service = service;
            ServiceType = service.GetType();
            PullAsyncChannels = pullAsyncChannels ?? Array.Empty<Type>();
            PullAsyncCommands = pullAsyncCommands ?? Array.Empty<Type>();
            PushAsyncChannels = pushAsyncChannels == null ? new HashSet<Type>() : new HashSet<Type>(pushAsyncChannels);
            PushAsyncCommands = pushAsyncCommands == null ? new HashSet<Type>() : new HashSet<Type>(pushAsyncCommands);
            PullChannels = pullChannels ?? Array.Empty<Type>();
            PullCommands = pullCommands ?? Array.Empty<Type>();
            PushChannels = pushChannels == null ? new HashSet<Type>() : new HashSet<Type>(pushChannels);
            PushCommands = pushCommands == null ? new HashSet<Type>() : new HashSet<Type>(pushCommands);
        }

        public readonly int Id;
        public readonly Type ServiceType;
        public readonly IService Service;
        public readonly Type[] PullAsyncChannels;
        public readonly HashSet<Type> PushAsyncChannels;
        public readonly Type[] PullAsyncCommands;
        public readonly HashSet<Type> PushAsyncCommands;
        public readonly Type[] PullChannels;
        public readonly HashSet<Type> PushChannels;
        public readonly Type[] PullCommands;
        public readonly HashSet<Type> PushCommands;

        public event Action DisposeActions;
        
        public void Dispose()
        {
            DisposeActions?.Invoke();
        }
    }
}