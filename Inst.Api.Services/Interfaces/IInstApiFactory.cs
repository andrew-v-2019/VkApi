namespace Inst.Api.Services.Interfaces
{
    using System.Threading.Tasks;
    using InstagramApiSharp.API;

    public interface IInstApiFactory
    {
        Task<IInstaApi> Login();
    }
}