using System;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using Exerussus._1Extensions.MicroserviceFeature;

namespace Exerussus.Microservices.Runtime
{
    public static class PullerRegistrar
    {
        internal static void RegisterAllChannelPullers(object instance)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));

            var type = instance.GetType();
            var pullerInterfaces = type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IChannelPuller<>))
                .ToArray();

            foreach (var itf in pullerInterfaces)
            {
                var channelType = itf.GetGenericArguments()[0];
                var interfaceMethod = itf.GetMethod(nameof(IChannelPuller<IChannel>.PullBroadcast));
                if (interfaceMethod == null) throw new InvalidOperationException($"Интерфейс {itf} не содержит PullBroadcast");
                var map = type.GetInterfaceMap(itf);
                var index = Array.IndexOf(map.InterfaceMethods, interfaceMethod);
                if (index < 0) throw new InvalidOperationException($"Не найдена реализация PullBroadcast для {itf}");
                var implMethod = map.TargetMethods[index];
                var pullerDelegate = implMethod.CreateDelegate(typeof(Action<>).MakeGenericType(channelType), instance);
                var registerMethod = typeof(MicroservicesApi).GetMethod(nameof(MicroservicesApi.RegisterChannelPuller), BindingFlags.NonPublic | BindingFlags.Static);
                var genericRegister = registerMethod.MakeGenericMethod(channelType);
                genericRegister.Invoke(null, new object[] { instance, pullerDelegate });
            }
        }
        
        internal static void RegisterAllCommandPullers(object instance)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));

            var type = instance.GetType();
            var pullerInterfaces = type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommandPuller<,>))
                .ToArray();

            foreach (var itf in pullerInterfaces)
            {
                var genericArgs = itf.GetGenericArguments();
                var commandType = genericArgs[0];
                var responseType = genericArgs[1];

                var interfaceMethod = itf.GetMethod(nameof(ICommandPuller<ICommand<object>, object>.PullBroadcast));
                if (interfaceMethod == null)
                    throw new InvalidOperationException($"Интерфейс {itf} не содержит PullBroadcast");

                var map = type.GetInterfaceMap(itf);
                var index = Array.IndexOf(map.InterfaceMethods, interfaceMethod);
                if (index < 0)
                    throw new InvalidOperationException($"Не найдена реализация PullBroadcast для {itf}");

                var implMethod = map.TargetMethods[index];

                // Создаем делегат Func<TCommand, UniTask<TResponse>>
                var delegateType = typeof(Func<,>).MakeGenericType(commandType, responseType);
                var pullerDelegate = implMethod.CreateDelegate(delegateType, instance);

                // Получаем метод RegisterCommandPuller и делаем его generic
                var registerMethod = typeof(MicroservicesApi).GetMethod(nameof(MicroservicesApi.RegisterCommandPuller), BindingFlags.NonPublic | BindingFlags.Static);
                var genericRegister = registerMethod.MakeGenericMethod(commandType, responseType);

                genericRegister.Invoke(null, new object[] { instance, pullerDelegate });
            }
        }
        
        internal static void RegisterAllAsyncChannelPullers(object instance)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));

            var type = instance.GetType();
            var pullerInterfaces = type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IChannelPullerAsync<>))
                .ToArray();

            foreach (var itf in pullerInterfaces)
            {
                var channelType = itf.GetGenericArguments()[0];
                var interfaceMethod = itf.GetMethod(nameof(IChannelPullerAsync<IChannel>.PullBroadcastAsync));
                if (interfaceMethod == null) throw new InvalidOperationException($"Интерфейс {itf} не содержит PullBroadcast");
                var map = type.GetInterfaceMap(itf);
                var index = Array.IndexOf(map.InterfaceMethods, interfaceMethod);
                if (index < 0) throw new InvalidOperationException($"Не найдена реализация PullBroadcast для {itf}");
                var implMethod = map.TargetMethods[index];
                var pullerDelegate = implMethod.CreateDelegate(typeof(Func<,>).MakeGenericType(channelType, typeof(UniTask)), instance);
                var registerMethod = typeof(MicroservicesApi).GetMethod(nameof(MicroservicesApi.RegisterChannelPullerAsync), BindingFlags.NonPublic | BindingFlags.Static);
                var genericRegister = registerMethod.MakeGenericMethod(channelType);
                genericRegister.Invoke(null, new object[] { instance, pullerDelegate });
            }
        }
        
        internal static void RegisterAllAsyncCommandPullers(object instance)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));

            var type = instance.GetType();
            var pullerInterfaces = type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommandPullerAsync<,>))
                .ToArray();

            foreach (var itf in pullerInterfaces)
            {
                var genericArgs = itf.GetGenericArguments();
                var commandType = genericArgs[0];
                var responseType = genericArgs[1];

                var interfaceMethod = itf.GetMethod(nameof(ICommandPullerAsync<ICommand<object>, object>.PullBroadcastAsync));
                if (interfaceMethod == null)
                    throw new InvalidOperationException($"Интерфейс {itf} не содержит PullBroadcast");

                var map = type.GetInterfaceMap(itf);
                var index = Array.IndexOf(map.InterfaceMethods, interfaceMethod);
                if (index < 0)
                    throw new InvalidOperationException($"Не найдена реализация PullBroadcast для {itf}");

                var implMethod = map.TargetMethods[index];

                // Создаем делегат Func<TCommand, UniTask<TResponse>>
                var delegateType = typeof(Func<,>).MakeGenericType(commandType, typeof(UniTask<>).MakeGenericType(responseType));
                var pullerDelegate = implMethod.CreateDelegate(delegateType, instance);

                // Получаем метод RegisterCommandPuller и делаем его generic
                var registerMethod = typeof(MicroservicesApi).GetMethod(nameof(MicroservicesApi.RegisterCommandPullerAsync), BindingFlags.NonPublic | BindingFlags.Static);
                var genericRegister = registerMethod.MakeGenericMethod(commandType, responseType);

                genericRegister.Invoke(null, new object[] { instance, pullerDelegate });
            }
        }

    }
}
