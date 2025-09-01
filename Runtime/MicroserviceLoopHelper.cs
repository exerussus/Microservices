
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.LowLevel;
using PlayerLoopType = UnityEngine.PlayerLoop;
#if UNITY_EDITOR
using UnityEditor;
#endif


namespace Exerussus.Microservices.Runtime
{

namespace Exerussus._1Extensions.LoopFeature
{
    public static class MicroserviceLoopRunners
    {
        public struct Update { };
    }

    public static class MicroserviceLoopHelper
    {
        public static event Action OnUpdate;

        static bool initialized;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        static void Init()
        {
#if UNITY_EDITOR
            var domainReloadDisabled = EditorSettings.enterPlayModeOptionsEnabled && EditorSettings.enterPlayModeOptions.HasFlag(EnterPlayModeOptions.DisableDomainReload);
            if (!domainReloadDisabled && initialized) return;
#else
            if (initialized) return;
#endif
            
            var playerLoop = PlayerLoop.GetCurrentPlayerLoop();
            Initialize(ref playerLoop);
        }

        public static void Initialize(ref PlayerLoopSystem playerLoop)
        {
            initialized = true;
            var newLoop = playerLoop.subSystemList.ToArray();
            InsertLoop(newLoop, typeof(PlayerLoopType.Update), typeof(MicroserviceLoopRunners.Update), static () => OnUpdate?.Invoke());
            playerLoop.subSystemList = newLoop;
            PlayerLoop.SetPlayerLoop(playerLoop);
        }

        static void InsertLoop(PlayerLoopSystem[] loopSystems, Type loopType, Type loopRunnerType, PlayerLoopSystem.UpdateFunction updateDelegate)
        {
            var i = FindLoopSystemIndex(loopSystems, loopType);
            ref var loop = ref loopSystems[i];
            loop.subSystemList = InsertRunner(loop.subSystemList, loopRunnerType, updateDelegate);
        }

        static int FindLoopSystemIndex(PlayerLoopSystem[] playerLoopList, Type systemType)
        {
            for (int i = 0; i < playerLoopList.Length; i++)
            {
                if (playerLoopList[i].type == systemType)
                {
                    return i;
                }
            }

            throw new Exception("Target PlayerLoopSystem does not found. Type:" + systemType.FullName);
        }

        static PlayerLoopSystem[] InsertRunner(PlayerLoopSystem[] subSystemList, Type loopRunnerType, PlayerLoopSystem.UpdateFunction updateDelegate)
        {
            var source = subSystemList.Where(x => x.type != loopRunnerType).ToArray();
            var dest = new PlayerLoopSystem[source.Length + 1];

            Array.Copy(source, 0, dest, 1, source.Length);

            dest[0] = new PlayerLoopSystem
            {
                type = loopRunnerType,
                updateDelegate = updateDelegate
            };

            return dest;
        }
    }
}
}