using System.Threading.Tasks;

namespace EasyNetQ.AutoSubscribe
{
    public interface IConsumeAsync<in T> where T : class 
    {
        Task Consume(T message);
    }
}