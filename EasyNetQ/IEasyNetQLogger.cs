namespace EasyNetQ
{
    public interface IEasyNetQLogger
    {
        void DebugWrite(string format, params object[] args);
    }
}