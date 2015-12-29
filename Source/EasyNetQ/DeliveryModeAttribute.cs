using System;

namespace EasyNetQ
{
#if !DOTNET5_4
#endif
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false)]
    public class DeliveryModeAttribute : Attribute
    {
        public DeliveryModeAttribute(bool isPersistent)
        {
            IsPersistent = isPersistent;
        }

        public bool IsPersistent { get; private set; }
    }
}