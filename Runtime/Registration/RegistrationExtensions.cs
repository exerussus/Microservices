namespace Exerussus.Microservices.Runtime.Registration
{
    public static class RegistrationExtensions
    {
        public static ServiceHandle RegisterService(this IService service)
        {
            return MicroservicesApi.RegisterService(service);
        }
    }
}