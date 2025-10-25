using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Exerussus.Microservices.Runtime.Modules.InjectModule;
using Exerussus.Microservices.Runtime.Registration;

namespace Exerussus.Microservices.Runtime.Modules
{
    public class InjectService : IServiceInspector
    {
        public InjectService(DependenciesContainer container, bool needInjectInterface = true)
        {
            DependenciesContainer = container;
            _needInjectInterface = needInjectInterface;
        }
        
        public readonly DependenciesContainer DependenciesContainer;
        public ServiceHandle Handle { get; set; }
        public Dictionary<Type, object> AsyncChannelsSubs { get; } = null;
        public Dictionary<int, RegisteredService> RegisteredServices { get; } = null;
        public Dictionary<int, HashSet<Type>> AsyncPushersToChannels { get; } = null;
        public Dictionary<Type, HashSet<int>> AsyncChannelsToPullers { get; } = null;
        public Dictionary<int, HashSet<Type>> PushersToChannels { get; } = null;
        public Dictionary<Type, HashSet<int>> ChannelsToPullers { get; } = null;
        public Dictionary<Type, object> ChannelsSubs { get; } = null;

        private readonly bool _needInjectInterface;
        private static readonly Type DiAttrType = typeof(InjectAttribute);

        public void OnServiceRegistered(RegisteredService registeredService)
        {
            if (registeredService.Service is IInjectedService || !_needInjectInterface) TryInjectFields(registeredService.Service);
        }

        public void TryInjectFields(object target, DependenciesContainer localContainer = null)
        {
            foreach (var fi in target.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (fi.IsStatic) continue;

                if (Attribute.IsDefined(fi, DiAttrType))
                {
                    if (DependenciesContainer.TryGet(fi.FieldType, out var injectObj))
                    {
                        fi.SetValue(target, injectObj);
                    }
                    else if (localContainer != null && localContainer.TryGet(fi.FieldType, out injectObj))
                    {
                        fi.SetValue(target, injectObj);
                    }
                    else
                    {
#if DEBUG
                        throw new Exception($"Ошибка инъекции данных в \"{CleanTypeName(target.GetType())}\" - тип поля \"{fi.Name}\" отсутствует в контейнере зависимостей.");
#endif
                    }
                }
            }
        }


#if DEBUG || UNITY_EDITOR
        private static string CleanTypeName(Type type)
        {
            string name;
            if (!type.IsGenericType) name = type.Name;
            else
            {
                var constraints = new StringBuilder();
                foreach (var constraint in type.GetGenericArguments())
                {
                    if (constraints.Length > 0) constraints.Append(", ");
                    
                    constraints.Append(CleanTypeName(constraint));
                }

                var genericIndex = type.Name.LastIndexOf("`", StringComparison.Ordinal);
                var typeName = genericIndex == -1
                    ? type.Name
                    : type.Name.Substring(0, genericIndex);
                name = $"{typeName}<{constraints}>";
            }

            return name;
        }
#endif
    }
}