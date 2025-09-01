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
        private static readonly Dictionary<int, IServiceInspector> InspectorServices = new ();
        private static readonly Dictionary<int, RegisteredService> RegisteredServices = new ();
        private static readonly Dictionary<int, HashSet<Type>> PushersToChannels = new ();
        private static readonly Dictionary<Type, HashSet<int>> ChannelsToPullers = new ();
        private static readonly object RegisterLock = new();
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

                var pullChannelTypes = GetGenericArgumentsFor(service.GetType(), typeof(IChannelPuller<>));
                var pushChannelTypes = GetGenericArgumentsFor(service.GetType(), typeof(IChannelPusher<>));

                var pullTypesArray = pullChannelTypes.Count == 0 ? null : pullChannelTypes.ToArray();
                var pushTypesArray = pushChannelTypes.Count == 0 ? null : pushChannelTypes.ToArray();

                var registeredService = new RegisteredService(handle.Id, service, pullTypesArray, pushTypesArray);

                RegisteredServices.Add(handle.Id, registeredService);
                
                if (service is IServiceInspector inspector)
                {
                    InspectorServices.Add(service.Handle.Id, inspector);
                    inspector.RegisteredServices = RegisteredServices;
                    inspector.PushersToChannels = PushersToChannels;
                    inspector.ChannelsToPullers = ChannelsToPullers;
                    inspector.ChannelsSubs = ChannelsSubs;
                    inspector.LockChannelsPullers = LockChannelsPullers;
                }

                if (pullChannelTypes.Count > 0)
                {
                    ChannelPullerRegistrar.RegisterAllPullers(service);

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
            
            RegisteredServices.Clear();
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

            foreach (var channelType in registeredService.PullChannels)
            {
                if (ChannelsToPullers.TryGetValue(channelType, out var pullers))
                {
                    pullers.Remove(registeredService.Id);
                    if (pullers.Count == 0) ChannelsToPullers.Remove(channelType);
                }
            }

            PushersToChannels.Remove(registeredService.Id);
            InspectorServices.Remove(registeredService.Id);

            foreach (var serviceInspector in InspectorServices.Values) serviceInspector.OnServiceUnregistered(registeredService);
            
            Debug.Log($"Microservices | Unregister service {registeredService.ServiceType.Name} with ID : {registeredService.Id}.");

            registeredService.Dispose();
        }

        internal static void RegisterPuller<TChannel>(object instance, Func<TChannel, UniTask> puller) where TChannel : IChannel
        {
            if (instance is not IService service) return;
            
            Debug.Log($"Microservices | RegisterPuller : {typeof(TChannel).Name} in {instance.GetType().Name}.");
            var registeredService = RegisteredServices[service.Handle.Id];
            
            AddToPool(registeredService, puller);
        }
        
        internal static async UniTask PushBroadcast<T>(int serviceId, T channel) where T : IChannel
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
            if (subObj is not Func<T, UniTask> subs) return;
            await subs(channel);
        }

        #endregion

        #region PRIVATE

        private static void AddToPool<T>(RegisteredService registeredService, Func<T, UniTask> pull) where T : IChannel
        {
            lock (LockChannelsPullers)
            {
                var type = typeof(T);
                Func<T, UniTask> subs;

                if (!ChannelsSubs.TryGetValue(type, out var subObject))
                {
                    subs = pull;
                    ChannelsSubs.Add(type, subs);
                }
                else
                {
                    subs = subObject as Func<T, UniTask>;
                    subs += pull;
                    ChannelsSubs[type] = subs;
                }

                registeredService.DisposeActions += () =>
                {
                    if (ChannelsSubs == null) return;
                    if (ChannelsSubs.Count == 0) return;
                    if (!ChannelsSubs.TryGetValue(type, out var subObj)) return;
                    if (subObj is not Func<T, UniTask> subscribes) return;
                    subscribes -= pull;
                    lock (LockChannelsPullers)
                    {
                        ChannelsSubs[type] = subscribes;
                    }
                };
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
