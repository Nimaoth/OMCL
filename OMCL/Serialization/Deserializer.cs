using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using OMCL.Data;

namespace OMCL.Serialization {

public interface IArrayConverter {
    object CreateInstance();

    void AddValue(object collection, object value);
    Type ElementType { get; }
}

public class Deserializer {

    private Dictionary<Type, IArrayConverter> _arrayConverters = new Dictionary<Type, IArrayConverter>();

    public Deserializer() {}
    
    public void AddArrayConverter<T>(IArrayConverter conv) {
        _arrayConverters[typeof(T)] = conv;
    }

    public T Deserialize<T>(Parser parser)
        where T : class, new() {
        var obj = parser.ParseObject();

        var result = new T();
        FillFields(result, obj);

        return result;
    }

    public object Deserialize(Type type, OMCLObject obj) {
        // check if its a dictionary
        var iDictionary = type.GetInterfaces().Where(t => {
            return t.Name.StartsWith(nameof(IDictionary) + "`") &&
                t.GenericTypeArguments.Length == 2 &&
                t.GenericTypeArguments[0] == typeof(string);
        }).FirstOrDefault();
        if (iDictionary != null) {
            var keyType = iDictionary.GenericTypeArguments[0];
            var valueType = iDictionary.GenericTypeArguments[1];
            var col = Activator.CreateInstance(type);
            var addMethod = col.GetType().GetMethod("Add", new Type[] { keyType, valueType });

            foreach (var (key, item) in obj) {
                var value = ConvertItemToValue(valueType, item);
                addMethod.Invoke(col, new object[] { key, value });
            }

            return col;
        }

        var result = Activator.CreateInstance(type);
        FillFields(type, result, obj);
        return result;
    }

    public object Deserialize(Type type, OMCLArray array) {
        // arrays
        if (type.IsArray) {
            var elemType = type.GetElementType();
            var arr = Array.CreateInstance(elemType, array.Length);

            for (int i = 0; i < arr.Length; i++) {
                var item = array[i];
                var value = ConvertItemToValue(elemType, item);
                arr.SetValue(value, i);
            }

            return arr;
        }

        // collections
        var iCollection = type.GetInterfaces().Where(t => t.Name.StartsWith(nameof(ICollection) + "`") && t.GenericTypeArguments.Length == 1).FirstOrDefault();
        if (iCollection != null) {
            var elemType = iCollection.GenericTypeArguments[0];
            var col = Activator.CreateInstance(type);
            var addMethod = col.GetType().GetMethod("Add", new Type[] { elemType });

            foreach (var item in array) {
                var value = ConvertItemToValue(elemType, item);
                addMethod.Invoke(col, new object[] { value });
            }

            return col;
        }

        // other types
        if (_arrayConverters.ContainsKey(type)) {
            var conv = _arrayConverters[type];
            var collection = conv.CreateInstance();
            foreach (var item in array) {
                var value = ConvertItemToValue(conv.ElementType, item);
                conv.AddValue(collection, value);
            }
            return collection;
        }

        throw new Exception($"Failed to deserialize array into type '{type.FullName}'. Please provide a custom converter if you want this type to be deserializable.");
    }

    private void FillFields<T>(T result, OMCLObject obj) {
        FillFields(typeof(T), result, obj);
    }

    private void FillFields(Type tType, object result, OMCLObject obj) {
        var fields = tType.GetFields(BindingFlags.Public | BindingFlags.Instance);
        var props = tType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var field in fields) {
            var name = field.Name;

            if (!obj.HasProperty(name))
                continue;
                
            var value = obj[name];

            SetField(field, result, value);
        }
    }

    private void SetField(MemberInfo info, object obj, OMCLItem item) {
        switch (info) {
        case PropertyInfo pi: {
            object val = ConvertItemToValue(pi.PropertyType, item);
            pi.SetValue(obj, val);
            break;
        }

        case FieldInfo fi: {
            object val = ConvertItemToValue(fi.FieldType, item);
            fi.SetValue(obj, val);
            break;
        }
        }
    }

    private object ConvertItemToValue(Type targetType, OMCLItem item) {
        var tags = item.Tags;
        var sourceType = item.Type;
        
        switch (sourceType) {
        case OMCLItem.OMCLItemType.None:
            return null;

        case OMCLItem.OMCLItemType.String:
            return item.AsString();
            
        case OMCLItem.OMCLItemType.Int:
            return Convert.ChangeType(item.AsInt(), targetType);

        case OMCLItem.OMCLItemType.Bool:
            return item.AsBool();

        case OMCLItem.OMCLItemType.Float:
            return Convert.ChangeType(item.AsFloat(), targetType);

        case OMCLItem.OMCLItemType.Object:
            return Deserialize(targetType, item.AsObject());

        case OMCLItem.OMCLItemType.Array:
            return Deserialize(targetType, item.AsArray());

        default:
            throw new NotImplementedException();
        }
    }
}

}