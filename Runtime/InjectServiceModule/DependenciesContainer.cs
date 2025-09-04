using System;
using System.Collections.Generic;
using UnityEngine;

namespace Exerussus.Microservices.Runtime.Modules.InjectModule
{
    public class DependenciesContainer
    {
        private readonly Dictionary<Type, object> _refs = new();

        public void Clear()
        {
            _refs.Clear();
        }
        
        public DependenciesContainer Add(params object[] refs)
        {
            if (refs is not { Length: > 0 })
            {
                Debug.LogError($"DependenciesContainer ERROR | Can't add null refs.");
                return this;
            }

            for (int i = 0; i < refs.Length; i++)
            {
                ref var refObject = ref refs[i];

                var type = refObject.GetType();

                if (_refs.ContainsKey(type))
                {
                    Debug.LogWarning($"DependenciesContainer WARNING | Type {type} already added. Replaced with new reference.");
                }
                
                _refs[type] = refObject;
            }
            
            return this;
        }
        
        public T Get<T>()
        {
            return _refs.TryGetValue(typeof(T), out var value) ? (T)value : default;
        }
        
        public DependenciesContainer Remove<T>()
        {
            _refs.Remove(typeof(T));
            return this;
        }
        
        public DependenciesContainer Remove(Type type)
        {
            _refs.Remove(type);
            return this;
        }
        
        public DependenciesContainer Remove(object obj)
        {
            _refs.Remove(obj.GetType());
            return this;
        }
        
        public T Pop<T>()
        {
            var value = Get<T>();
            Remove<T>();
            return value;
        }
        
        public bool TryGet<T>(out T value)
        {
            if (_refs.TryGetValue(typeof(T), out var obj))
            {
                value = (T)obj;
                return value != null;
            }
            
            value = default;
            return false;
        }
        
        public bool TryGet(Type type, out object value)
        {
            return _refs.TryGetValue(type, out value);
        }
        
        public bool TryPop<T>(out T value)
        {
            value = Get<T>();
            Remove<T>();
            return value != null;
        }
    }
}