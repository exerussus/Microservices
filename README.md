# Microservices

Минимальный набор интерфейсов и API для создания **микросервисной архитектуры**.

> ⚠️ **Внимание!** Пакет имеет зависимость от: [UniTask](https://github.com/Cysharp/UniTask.git)

---

## 📦 Основные понятия

- **Сервис (`IService`)** — единица логики.
- **Канал (`IChannel`)** — сообщение или трансляция между сервисами.  
  Сервисы **не взаимодействуют напрямую**: они работают только через каналы.
- **ServiceHandle** — ручка для управления состоянием сервиса (подписки, отправка сообщений и т.д.).

---

## 🚀 Создание сервиса

Пример сервиса с подпиской на канал:

```c#
    // Канал для создания взаимодействия
    public class Some1Channel : IChannel
    {
        public int Id { get; set; }
    }

    public class Pull1Service : IService,
        // Наследуем для подписки на канал
        IChannelPuller<Some1Channel>
    {
        // Ручка для управления состоянием данного сервиса
        public ServiceHandle Handle { get; set; }

        // Имплементация метода из интерфейса IChannelPuller
        public async UniTask PullBroadcast(Some1Channel channel)
        {
            // Тут идёт обработка сообщения
        }
    }
```

---

## 📤 Отправка сообщений

Чтобы сервис мог **отправлять канал**, нужно унаследовать `IChannelPusher<T>`:

```c#
    public class PushService : IService, 
        // Теперь можно отправлять сообщение через Handle.Push
        IChannelPusher<Some1Channel>
    {
        public ServiceHandle Handle { get; set; }
    }
```

---

## 🛠 Регистрация сервисов

Сервисы можно регистрировать двумя способами:

```c#
    public class MicroserviceStarter : MonoBehaviour 
    {
        public void Start() 
        {
            // Через Api
            var pull1ServiceHandle = MicroservicesApi.RegisterService(new Pull1Service());
            // Через Extension
            var pull2ServiceHandle = new Pull2Service().RegisterService();
        }
    }
```

---

## 🔄 Обновление без MonoBehaviour

Можно подписаться на **Update**, унаследовав `IServiceRun`:

```c#
    public class RunService : IService, IServiceRun
    {
        public ServiceHandle Handle { get; set; }

        public void Run()
        {
            // Будет обновляться каждый Update
        }
    }
```

---

## 🔍 Инспекция сервисов

Для анализа и кастомизации можно реализовать `IServiceInspector`.  
Он даёт доступ к внутренним структурам и событиям регистрации/удаления сервисов:

```c#
    public interface IServiceInspector
    {
        public Dictionary<Type, object> ChannelsSubs {get; set; }
        public Dictionary<int, RegisteredService> RegisteredServices {get; set; }
        public Dictionary<int, HashSet<Type>> PushersToChannels {get; set; }
        public Dictionary<Type, HashSet<int>> ChannelsToPullers {get; set; }

        public virtual void OnServiceRegistered(RegisteredService registeredService) {}
        public virtual void OnServiceUnregistered(RegisteredService registeredService) {}
    }
```

> 💡 Рекомендуется регистрировать инспекторы **в первую очередь**.

---
