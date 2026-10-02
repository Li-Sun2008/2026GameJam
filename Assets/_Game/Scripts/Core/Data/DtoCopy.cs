using System;
using System.Collections;
using System.Reflection;

namespace Spotlight.Core.Data
{
    public static class DtoCopy
    {
        public static T Clone<T>(T value) { return (T)Copy(value); }
        private static object Copy(object value)
        {
            if (value == null) return null;
            Type type = value.GetType();
            if (type.IsValueType || value is string) return value;
            IList collection = value as IList;
            if (collection != null)
            {
                Type element = type.IsArray ? type.GetElementType() : type.GetGenericArguments()[0];
                Array copy = Array.CreateInstance(element, collection.Count);
                for (int i = 0; i < collection.Count; i++) copy.SetValue(Copy(collection[i]), i);
                return copy;
            }
            object result = Activator.CreateInstance(type);
            foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Public)) field.SetValue(result, Copy(field.GetValue(value)));
            return result;
        }
    }
}

