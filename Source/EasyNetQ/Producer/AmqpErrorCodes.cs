namespace EasyNetQ.Producer
{
    internal static class AmqpErrorCodes
    {
        public const ushort ConnectionClosed = 320;
        public const ushort AccessRefused = 403;
        public const ushort NotFound = 404;
        public const ushort ResourceLocked = 405;
        public const ushort PreconditionFailed = 406;
    }
}
