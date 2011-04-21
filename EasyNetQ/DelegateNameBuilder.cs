using System;

namespace EasyNetQ
{
    public class DelegateNameBuilder
    {
        public static string CreateNameFrom(Delegate @delegate)
        {
            var name =  @delegate.Method.DeclaringType + "_" + @delegate.Method.Name;
            return name.Replace('.', '_').Replace('<', '_').Replace('>', '_');
        }
    }
}