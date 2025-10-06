using System;
using System.Collections.Generic;

namespace Exerussus.Microservices.Runtime.Registration
{
    public class RegisteredService : IDisposable
    {
        public RegisteredService(int id, IService service, Type[] pullChannels, Type[] pushChannels, Type[] pullCommands, Type[] pushCommands)
        {
            Id = id;
            Service = service;
            PullChannels = pullChannels ?? Array.Empty<Type>();
            PullCommands = pullCommands ?? Array.Empty<Type>();
            PushChannels = pushChannels == null ? new HashSet<Type>() : new HashSet<Type>(pushChannels);
            PushCommands = pushCommands == null ? new HashSet<Type>() : new HashSet<Type>(pushCommands);
            ServiceType = service.GetType();
        }

        public readonly int Id;
        public readonly Type ServiceType;
        public readonly IService Service;
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