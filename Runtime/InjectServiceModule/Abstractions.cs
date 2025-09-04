using System;

namespace Exerussus.Microservices.Runtime.Modules.InjectModule
{
    public interface IInjectedService
    {
        
    }
    
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public class InjectAttribute : Attribute
    {
        
    }
}