namespace VKApi.BL.Interfaces
{
    public interface ICacheService
    {
        void Create(object data, string key);
        T Get<T>(string key);
        void Append<T>(T valueToAppend, string key);
    }
}
