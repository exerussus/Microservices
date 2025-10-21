using System;
using UnityEngine;
using System.Linq;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using Exerussus._1Extensions.MicroserviceFeature;
using Exerussus.Microservices.Runtime.Registration;
using Exerussus.Microservices.Runtime.Exerussus._1Extensions.LoopFeature;

// ReSharper disable once CheckNamespace
namespace Exerussus.Microservices.Runtime
{
    public static class MicroservicesApi
    {
        private static readonly Dictionary<Type, object> ChannelsSubs = new ();
        private static readonly Dictionary<Type, object> CommandSubs = new ();
        private static readonly Dictionary<Type, object> AsyncChannelsSubs = new ();
        private static readonly Dictionary<Type, object> AsyncCommandSubs = new ();
        private static readonly Dictionary<int, IServiceInspector> InspectorServices = new ();
        private static readonly Dictionary<int, RegisteredService> RegisteredServices = new ();
        private static readonly Dictionary<int, HashSet<Type>> AsyncPushersToChannels = new ();
        private static readonly Dictionary<Type, HashSet<int>> AsyncChannelsToPullers = new ();
        private static readonly Dictionary<int, HashSet<Type>> PushersToChannels = new ();
        private static readonly Dictionary<Type, HashSet<int>> ChannelsToPullers = new ();
        private static readonly object RegisterLock = new();
        private static readonly object LockAsyncChannelsPullers = new();
        private static readonly object LockChannelsPullers = new();
        private static int _freeId;

        #region PUBLIC

        public static ServiceHandle RegisterService(IService service)
        {
            ServiceHandle handle;
            
            lock (RegisterLock)
            {

                if (service == null)
                {
                    Debug.LogError("Microservices | Can't register null ref.");
                    return default;
                }

                _freeId++;

                handle = new ServiceHandle(_freeId);
                service.Handle = handle;
                
                Debug.Log($"Microservices | Registered service {service.GetType().Name} with ID : {service.Handle.Id}.");

                var pullAsyncChannelTypes = GetGenericArgumentsFor(service.GetType(), typeof(IChannelPullerAsync<>));
                var pushAsyncChannelTypes = GetGenericArgumentsFor(service.GetType(), typeof(IChannelPusherAsync<>));
                var pullAsyncCommandTypes = GetGenericArgumentsFor(service.GetType(), typeof(ICommandPullerAsync<,>));
                var pushAsyncCommandTypes = GetGenericArgumentsFor(service.GetType(), typeof(ICommandPusherAsync<,>));

                var pullChannelTypes = GetGenericArgumentsFor(service.GetType(), typeof(IChannelPuller<>));
                var pushChannelTypes = GetGenericArgumentsFor(service.GetType(), typeof(IChannelPusher<>));
                var pullCommandTypes = GetGenericArgumentsFor(service.GetType(), typeof(ICommandPuller<,>));
                var pushCommandTypes = GetGenericArgumentsFor(service.GetType(), typeof(ICommandPusher<,>));

                var pullAsyncTypesArray = pullAsyncChannelTypes.Count == 0 ? null : pullAsyncChannelTypes.ToArray();
                var pushAsyncTypesArray = pushAsyncChannelTypes.Count == 0 ? null : pushAsyncChannelTypes.ToArray();
                var pullAsyncCommandTypesArray = pullAsyncCommandTypes.Count == 0 ? null : pullAsyncCommandTypes.ToArray();
                var pushAsyncCommandTypesArray = pushAsyncCommandTypes.Count == 0 ? null : pushAsyncCommandTypes.ToArray();

                var pullTypesArray = pullChannelTypes.Count == 0 ? null : pullChannelTypes.ToArray();
                var pushTypesArray = pushChannelTypes.Count == 0 ? null : pushChannelTypes.ToArray();
                var pullCommandTypesArray = pullCommandTypes.Count == 0 ? null : pullCommandTypes.ToArray();
                var pushCommandTypesArray = pushCommandTypes.Count == 0 ? null : pushCommandTypes.ToArray();

                var registeredService = new RegisteredService(handle.Id, service, 
                    pullAsyncTypesArray, 
                    pushAsyncTypesArray, 
                    pullAsyncCommandTypesArray, 
                    pushAsyncCommandTypesArray,
                    pullTypesArray, 
                    pushTypesArray, 
                    pullCommandTypesArray, 
                    pushCommandTypesArray);

                RegisteredServices.Add(handle.Id, registeredService);
                
                if (service is IServiceInspector inspector)
                {
                    InspectorServices.Add(service.Handle.Id, inspector);
                    
                    ReflectionUtils.SetPropertyValue(inspector, nameof(inspector.RegisteredServices), RegisteredServices);
                    ReflectionUtils.SetPropertyValue(inspector, nameof(inspector.AsyncPushersToChannels), AsyncPushersToChannels);
                    ReflectionUtils.SetPropertyValue(inspector, nameof(inspector.AsyncChannelsToPullers), AsyncChannelsToPullers);
                    ReflectionUtils.SetPropertyValue(inspector, nameof(inspector.AsyncChannelsSubs), AsyncChannelsSubs);
                    ReflectionUtils.SetPropertyValue(inspector, nameof(inspector.PushersToChannels), PushersToChannels);
                    ReflectionUtils.SetPropertyValue(inspector, nameof(inspector.ChannelsToPullers), ChannelsToPullers);
                    ReflectionUtils.SetPropertyValue(inspector, nameof(inspector.ChannelsSubs), ChannelsSubs);
                }

                if (pullAsyncChannelTypes.Count > 0)
                {
                    PullerRegistrar.RegisterAllAsyncChannelPullers(service);
                    PullerRegistrar.RegisterAllAsyncCommandPullers(service);

                    foreach (var channelType in pullAsyncChannelTypes)
                    {
                        if (!AsyncChannelsToPullers.TryGetValue(channelType, out var pullers))
                        {
                            pullers = new HashSet<int>();
                            AsyncChannelsToPullers.Add(channelType, pullers);
                        }

                        pullers.Add(registeredService.Id);
                    }
                }

                if (pullChannelTypes.Count > 0)
                {
                    PullerRegistrar.RegisterAllChannelPullers(service);
                    PullerRegistrar.RegisterAllCommandPullers(service);

                    foreach (var channelType in pullChannelTypes)
                    {
                        if (!ChannelsToPullers.TryGetValue(channelType, out var pullers))
                        {
                            pullers = new HashSet<int>();
                            ChannelsToPullers.Add(channelType, pullers);
                        }

                        pullers.Add(registeredService.Id);
                    }
                }

                if (pushAsyncChannelTypes.Count > 0)
                {
                    if (!AsyncPushersToChannels.TryGetValue(registeredService.Id, out var channels))
                    {
                        channels = new HashSet<Type>();
                        AsyncPushersToChannels.Add(registeredService.Id, channels);
                    }

                    foreach (var channelType in pushAsyncChannelTypes) channels.Add(channelType);
                }

                if (pushChannelTypes.Count > 0)
                {
                    if (!PushersToChannels.TryGetValue(registeredService.Id, out var channels))
                    {
                        channels = new HashSet<Type>();
                        PushersToChannels.Add(registeredService.Id, channels);
                    }

                    foreach (var channelType in pushChannelTypes) channels.Add(channelType);
                }

                TryRegisterUpdating(registeredService);

                foreach (var (id, serviceInspector) in InspectorServices)
                {
                    if (registeredService.Id == id) continue;
                    serviceInspector.OnServiceRegistered(registeredService);
                }
            }

            return handle;
        }

        public static void UnregisterAll()
        {
            foreach (var serviceId in RegisteredServices.Keys.ToArray()) UnregisterService(serviceId);

            _freeId = 0;
            RegisteredServices.Clear();
            AsyncChannelsSubs.Clear();
            ChannelsSubs.Clear();
        }

        #endregion

        #region INTERNAL

        internal static bool GetIsValid(int id)
        {
            return RegisteredServices.ContainsKey(id);
        }

        internal static void UnregisterService(int id)
        {
            if (!RegisteredServices.Remove(id, out var registeredService)) return;

            foreach (var channelType in registeredService.PullAsyncChannels)
            {
                if (AsyncChannelsToPullers.TryGetValue(channelType, out var pullers))
                {
                    pullers.Remove(registeredService.Id);
                    if (pullers.Count == 0) AsyncChannelsToPullers.Remove(channelType);
                }
            }

            AsyncPushersToChannels.Remove(registeredService.Id);
            InspectorServices.Remove(registeredService.Id);

            foreach (var serviceInspector in InspectorServices.Values) serviceInspector.OnServiceUnregistered(registeredService);
            
            Debug.Log($"Microservices | Unregister service {registeredService.ServiceType.Name} with ID : {registeredService.Id}.");

            registeredService.Dispose();
        }

        internal static void RegisterChannelPuller<TChannel>(object instance, Action<TChannel> puller) where TChannel : IChannel
        {
            if (instance is not IService service) return;
            
            Debug.Log($"Microservices | RegisterPuller : {typeof(TChannel).Name} in {instance.GetType().Name}.");
            var registeredService = RegisteredServices[service.Handle.Id];
            
            AddChannelToPool(registeredService, puller);
        }

        internal static void RegisterCommandPuller<TChannel, TResponse>(object instance, Func<TChannel, TResponse> puller) where TChannel : ICommand<TResponse>
        {
            if (instance is not IService service) return;
            
            Debug.Log($"Microservices | RegisterPuller : {typeof(TChannel).Name} in {instance.GetType().Name}.");
            var registeredService = RegisteredServices[service.Handle.Id];
            
            AddCommandToPool(registeredService, puller);
        }

        internal static void RegisterChannelPullerAsync<TChannel>(object instance, Func<TChannel, UniTask> puller) where TChannel : IChannel
        {
            if (instance is not IService service) return;
            
            Debug.Log($"Microservices | RegisterPuller : {typeof(TChannel).Name} in {instance.GetType().Name}.");
            var registeredService = RegisteredServices[service.Handle.Id];
            
            AddAsyncChannelToPool(registeredService, puller);
        }

        internal static void RegisterCommandPullerAsync<TChannel, TResponse>(object instance, Func<TChannel, UniTask<TResponse>> puller) where TChannel : ICommand<TResponse>
        {
            if (instance is not IService service) return;
            
            Debug.Log($"Microservices | RegisterPuller : {typeof(TChannel).Name} in {instance.GetType().Name}.");
            var registeredService = RegisteredServices[service.Handle.Id];
            
            AddAsyncCommandToPool(registeredService, puller);
        }
        
        internal static async UniTask PushBroadcastAsync<T>(int serviceId, T channel) where T : IChannel
        {
            var type = typeof(T);

            if (serviceId == 0)
            {
                Debug.LogError($"Microservices | Can't push async broadcast {type.Name} from broken handle.");
                return;
            }
            
            if (!RegisteredServices.TryGetValue(serviceId, out var registeredService))
            {
                Debug.LogError($"Microservices | Can't push async broadcast {type.Name} (id:{serviceId}) from unregistered service.");
                return;
            }

            if (!registeredService.PushAsyncChannels.Contains(type))
            {
                Debug.LogError($"Microservices | Can't push async broadcast {type.Name} (id:{serviceId}) from service {registeredService.ServiceType.Name} without Push registration.");
                return;
            }
            
            if (!AsyncChannelsSubs.TryGetValue(type, out var subObj)) return;
            if (subObj is not Func<T, UniTask> subs) return;
            await subs(channel);
        }
        
        internal static void PushBroadcast<T>(int serviceId, T channel) where T : IChannel
        {
            var type = typeof(T);

            if (serviceId == 0)
            {
                Debug.LogError($"Microservices | Can't push broadcast {type.Name} from broken handle.");
                return;
            }
            
            if (!RegisteredServices.TryGetValue(serviceId, out var registeredService))
            {
                Debug.LogError($"Microservices | Can't push broadcast {type.Name} (id:{serviceId}) from unregistered service.");
                return;
            }

            if (!registeredService.PushChannels.Contains(type))
            {
                Debug.LogError($"Microservices | Can't push broadcast {type.Name} (id:{serviceId}) from service {registeredService.ServiceType.Name} without Push registration.");
                return;
            }
            
            if (!ChannelsSubs.TryGetValue(type, out var subObj)) return;
            if (subObj is not Action<T> subs) return;
            subs(channel);
        }
        
        internal static async UniTask<(bool success, TResponse response)> PushCommandAsync<TRequest, TResponse>(int serviceId, TRequest channel) where TRequest : ICommand<TResponse>
        {
            var type = typeof(TRequest);

            if (serviceId == 0)
            {
                Debug.LogError($"Microservices | Can't push async broadcast {type.Name} from broken handle.");
                return (false, default);
            }
            
            if (!RegisteredServices.TryGetValue(serviceId, out var registeredService))
            {
                Debug.LogError($"Microservices | Can't push async broadcast {type.Name} (id:{serviceId}) from unregistered service.");
                return (false, default);
            }

            if (!registeredService.PushAsyncCommands.Contains(type))
            {
                Debug.LogError($"Microservices | Can't push async broadcast {type.Name} (id:{serviceId}) from service {registeredService.ServiceType.Name} without Push registration.");
                return (false, default);
            }
            
            if (!AsyncCommandSubs.TryGetValue(type, out var subObj)) return (false, default);
            if (subObj is not Func<TRequest, UniTask<TResponse>> subs) return (false, default);
            return (true, await subs(channel));
        }
        
        internal static (bool success, TResponse response) PushCommand<TRequest, TResponse>(int serviceId, TRequest channel) where TRequest : ICommand<TResponse>
        {
            var type = typeof(TRequest);

            if (serviceId == 0)
            {
                Debug.LogError($"Microservices | Can't push broadcast {type.Name} from broken handle.");
                return (false, default);
            }
            
            if (!RegisteredServices.TryGetValue(serviceId, out var registeredService))
            {
                Debug.LogError($"Microservices | Can't push broadcast {type.Name} (id:{serviceId}) from unregistered service.");
                return (false, default);
            }

            if (!registeredService.PushCommands.Contains(type))
            {
                Debug.LogError($"Microservices | Can't push broadcast {type.Name} (id:{serviceId}) from service {registeredService.ServiceType.Name} without Push registration.");
                return (false, default);
            }
            
            if (!CommandSubs.TryGetValue(type, out var subObj)) return (false, default);
            if (subObj is not Func<TRequest, TResponse> subs) return (false, default);
            return (true, subs(channel));
        }

        #endregion

        #region PRIVATE

        private static void AddAsyncChannelToPool<T>(RegisteredService registeredService, Func<T, UniTask> pull) where T : IChannel
        {
            lock (LockAsyncChannelsPullers)
            {
                var type = typeof(T);
                Func<T, UniTask> subs;

                if (!AsyncChannelsSubs.TryGetValue(type, out var subObject))
                {
                    subs = pull;
                    AsyncChannelsSubs.Add(type, subs);
                }
                else
                {
                    subs = subObject as Func<T, UniTask>;
                    subs += pull;
                    AsyncChannelsSubs[type] = subs;
                }

                registeredService.DisposeActions += () =>
                {
                    if (AsyncChannelsSubs == null) return;
                    if (AsyncChannelsSubs.Count == 0) return;
                    if (!AsyncChannelsSubs.TryGetValue(type, out var subObj)) return;
                    if (subObj is not Func<T, UniTask> subscribes) return;
                    subscribes -= pull;
                    lock (LockAsyncChannelsPullers)
                    {
                        AsyncChannelsSubs[type] = subscribes;
                    }
                };
            }
        }
        
        private static void AddChannelToPool<T>(RegisteredService registeredService, Action<T> pull) where T : IChannel
        {
            lock (LockChannelsPullers)
            {
                var type = typeof(T);
                Action<T> subs;

                if (!ChannelsSubs.TryGetValue(type, out var subObject))
                {
                    subs = pull;
                    ChannelsSubs.Add(type, subs);
                }
                else
                {
                    subs = subObject as Action<T>;
                    subs += pull;
                    ChannelsSubs[type] = subs;
                }

                registeredService.DisposeActions += () =>
                {
                    if (ChannelsSubs == null) return;
                    if (ChannelsSubs.Count == 0) return;
                    if (!ChannelsSubs.TryGetValue(type, out var subObj)) return;
                    if (subObj is not Action<T> subscribes) return;
                    subscribes -= pull;
                    lock (LockChannelsPullers)
                    {
                        ChannelsSubs[type] = subscribes;
                    }
                };
            }
        }
        
        private static void AddCommandToPool<TRequest, TResponse>(RegisteredService registeredService, Func<TRequest, TResponse> pull) where TRequest : ICommand<TResponse>
        {
            lock (LockChannelsPullers)
            {
                var type = typeof(TRequest);

                if (CommandSubs.TryAdd(type, pull))
                {
                    registeredService.DisposeActions += () =>
                    {
                        if (CommandSubs == null) return;
                        if (CommandSubs.Count == 0) return;
                        lock (LockChannelsPullers) CommandSubs.Remove(type);
                    };
                }
                else
                {
                    Debug.LogError($"Microservices | Command {type.Name} already registered. Can't register more than one puller for command.");
                }
            }
        }
        
        private static void AddAsyncCommandToPool<TRequest, TResponse>(RegisteredService registeredService, Func<TRequest, UniTask<TResponse>> pull) where TRequest : ICommand<TResponse>
        {
            lock (LockAsyncChannelsPullers)
            {
                var type = typeof(TRequest);

                if (AsyncCommandSubs.TryAdd(type, pull))
                {
                    registeredService.DisposeActions += () =>
                    {
                        if (AsyncCommandSubs == null) return;
                        if (AsyncCommandSubs.Count == 0) return;
                        lock (LockAsyncChannelsPullers) AsyncCommandSubs.Remove(type);
                    };
                }
                else
                {
                    Debug.LogError($"Microservices | Command {type.Name} already registered. Can't register more than one puller for command.");
                }
            }
        }

        private static void TryRegisterUpdating(RegisteredService registeredService)
        {
            // ReSharper disable once SuspiciousTypeConversion.Global
            if (registeredService.Service is IServiceRun updatableService)
            {
                MicroserviceLoopHelper.OnUpdate -= updatableService.Run;
                MicroserviceLoopHelper.OnUpdate += updatableService.Run;
                
                registeredService.DisposeActions += () => MicroserviceLoopHelper.OnUpdate -= updatableService.Run;
            }
        }
        
        private static List<Type> GetGenericArgumentsFor(Type inspectedType, Type openInterface)
        {
            if (inspectedType == null || openInterface == null) return new List<Type>();

            var result = new List<Type>();

            var interfaces = inspectedType.GetInterfaces();
            for (var i = interfaces.Length - 1; i >= 0; i--)
            {
                var itf = interfaces[i];
                if (!itf.IsGenericType) continue;

                if (itf.GetGenericTypeDefinition() == openInterface)
                {
                    result.AddRange(itf.GetGenericArguments());
                }
            }

            return result;
        }

        #endregion
    }
}
