using System;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using Exerussus._1Extensions.MicroserviceFeature;

namespace Exerussus.Microservices.Runtime
{
    public static class ChannelPullerRegistrar
    {
        internal static void RegisterAllPullers(object instance)
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
                var pullerDelegate = implMethod.CreateDelegate(typeof(Func<,>).MakeGenericType(channelType, typeof(UniTask)), instance);
                var registerMethod = typeof(MicroservicesApi).GetMethod(nameof(MicroservicesApi.RegisterPuller), BindingFlags.NonPublic | BindingFlags.Static);
                var genericRegister = registerMethod.MakeGenericMethod(channelType);
                genericRegister.Invoke(null, new object[] { instance, pullerDelegate });
            }
        }
    }
}
