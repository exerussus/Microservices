using System;
using System.Collections.Generic;
using Exerussus.Microservices.Runtime.Registration;

namespace Exerussus.Microservices.Runtime
{
    public class InternalInstances
    {
        public readonly Dictionary<Type, object> ChannelsSubs = new ();
        public readonly Dictionary<Type, object> CommandSubs = new ();
        public readonly Dictionary<Type, object> AsyncChannelsSubs = new ();
        public readonly Dictionary<Type, object> AsyncCommandSubs = new ();
        public readonly Dictionary<int, IServiceInspector> InspectorServices = new ();
        public readonly Dictionary<int, RegisteredService> RegisteredServices = new ();
        public readonly Dictionary<int, HashSet<Type>> AsyncPushersToChannels = new ();
        public readonly Dictionary<Type, HashSet<int>> AsyncChannelsToPullers = new ();
        public readonly Dictionary<int, HashSet<Type>> PushersToChannels = new ();
        public readonly Dictionary<Type, HashSet<int>> ChannelsToPullers = new ();
        public readonly object RegisterLock = new();
        public readonly object LockAsyncChannelsPullers = new();
        public readonly object LockChannelsPullers = new();
    }
}