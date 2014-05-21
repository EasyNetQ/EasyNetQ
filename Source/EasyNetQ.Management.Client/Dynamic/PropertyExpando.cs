using System;
using System.Collections.Generic;
using System.Dynamic;

namespace EasyNetQ.Management.Client.Dynamic
{
    public abstract class PropertyExpando : DynamicObject
    {
        protected readonly IDictionary<string, object> _properties;

        protected PropertyExpando(IDictionary<string, object> properties)
        {
            if (null == properties)
            {
                throw new ArgumentNullException("properties", "The argument properties must not be null");
            }
            _properties = properties;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            if (!_properties.Keys.Contains(binder.Name))
            {
                result = null;
                return false;
            }

            result = _properties[binder.Name];
            return true;
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            _properties[binder.Name] = value;
            return true;
        }

        protected T GetPropertyOrDefault<T>(String propertyName)
        {
            if (_properties.Keys.Contains(propertyName) && _properties[propertyName] != null)
            {
                return (T)_properties[propertyName];
            }
            else
            {
                return default(T);
            }
        }

        protected IDictionary<string, Object> Properties
        {
            get
            {
                return _properties;
            }
        }
    }
}
