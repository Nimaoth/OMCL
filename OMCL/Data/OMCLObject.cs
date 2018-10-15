using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace OMCL.Data {

public class OMCLItem {

    public enum OMCLItemType {
        Object,
        Array,
        String,
        Int,
        Float,
        Bool,
        None
    }

    public OMCLItemType Type { get; private set; }

    private object _value;
    public List<string> Tags { get; set; } = new List<string>();

    public OMCLItem() {
    }

    public OMCLItem(OMCLObject item) {
        _value = item;
        Type = OMCLItemType.Object;
    }

    public OMCLItem(OMCLArray item) {
        _value = item;
        Type = OMCLItemType.Array;
    }

    public OMCLObject AsObject() {
        if (Type == OMCLItemType.Object)
            return (OMCLObject)_value;
        throw new System.Exception($"Value of type {Type} can't be casted to OMCLObject");
    }

    public OMCLArray AsArray() {
        if (Type == OMCLItemType.Array)
            return (OMCLArray)_value;
        throw new System.Exception($"Value of type {Type} can't be casted to OMCLArray");
    }

    public string AsString() {
        if (Type == OMCLItemType.String)
            return (string)_value;
        throw new System.Exception($"Value of type {Type} can't be casted to string");
    }

    public long AsInt() {
        if (Type == OMCLItemType.Int)
            return (long)_value;
        throw new System.Exception($"Value of type {Type} can't be casted to long");
    }

    public double AsFloat() {
        if (Type == OMCLItemType.Float)
            return (double)_value;
        throw new System.Exception($"Value of type {Type} can't be casted to double");
    }

    public bool AsBool() {
        if (Type == OMCLItemType.Bool)
            return (bool)_value;
        throw new System.Exception($"Value of type {Type} can't be casted to bool");
    }

    public OMCLNone AsNone() {
        if (Type == OMCLItemType.None)
            return (OMCLNone)_value;
        throw new System.Exception($"Value of type {Type} can't be casted to OMCLNone");
    }

    public static implicit operator OMCLItem(string value) => new OMCLItem { Type = OMCLItemType.String, _value = value };
    public static implicit operator OMCLItem(char value) => new OMCLItem { Type = OMCLItemType.String, _value = new string(value, 1) };
    public static implicit operator OMCLItem(int value) => new OMCLItem { Type = OMCLItemType.Int, _value = (long)value };
    public static implicit operator OMCLItem(long value) => new OMCLItem { Type = OMCLItemType.Int, _value = value };
    public static implicit operator OMCLItem(float value) => new OMCLItem { Type = OMCLItemType.Float, _value = (double)value };
    public static implicit operator OMCLItem(double value) => new OMCLItem { Type = OMCLItemType.Float, _value = value };
    public static implicit operator OMCLItem(bool value) => new OMCLItem { Type = OMCLItemType.Bool, _value = value };
    public static implicit operator OMCLItem(OMCLObject value) => new OMCLItem { Type = OMCLItemType.Object, _value = value };
    public static implicit operator OMCLItem(OMCLArray value) => new OMCLItem { Type = OMCLItemType.Array, _value = value };
    public static implicit operator OMCLItem(OMCLNone value) => new OMCLItem { Type = OMCLItemType.None, _value = value };

    public override bool Equals(object obj)
    {
        switch (obj) {
            case null: return false;

            case OMCLObject o: return _value?.Equals(o) ?? false;
            case OMCLArray o: return _value?.Equals(o) ?? false;
            case long x: return ((long)_value) == x;
            case double x: return ((double)_value) == x;
            case string x: return ((string)_value) == x;
            case bool x: return ((bool)_value) == x;

            case OMCLItem i: return _value?.Equals(i._value) ?? false;

            default: return false;
        }
    }
    
    public override int GetHashCode()
    {
        return _value?.GetHashCode() ?? 0;
    }
}

public class OMCLNone {
}

public class OMCLObject : IEnumerable<(string key, OMCLItem value)> {
    private IDictionary<string, OMCLItem> _properties = new Dictionary<string, OMCLItem>();

    public OMCLItem this[string index] {
        get { return _properties[index]; }
        set { _properties[index] = value; }
    }

    public IEnumerable<string> Keys {
        get {
            foreach (var k in _properties.Keys) 
                yield return k;
        }
    }

    public IEnumerable<(string key, OMCLItem value)> Properties {
        get {
            foreach (var kv in _properties)
                yield return (kv.Key, kv.Value);
        }
    }

    public bool Empty => _properties.Count == 0;

    public OMCLObject() {
        
    }

    public OMCLObject(IDictionary<string, OMCLItem> props) {
        _properties = props;
    }

    public bool HasProperty(string name) {
        return _properties.ContainsKey(name);
    }

    public void RemoveProperty(string name) {
        _properties.Remove(name);
    }

    public void Add(string key, OMCLItem val) {
        this[key] = val;
    }

    public IEnumerator<(string key, OMCLItem value)> GetEnumerator()
    {
        return Properties.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _properties.GetEnumerator();
    }
}

public class OMCLArray : IEnumerable<OMCLItem> {
    private List<OMCLItem> _items = new List<OMCLItem>();

    public OMCLItem this[int index] {
        get { return _items[index]; }
        set { _items[index] = value; }
    }

    public int Length => _items.Count;

    public bool Empty => _items.Count == 0;

    public OMCLArray() {

    }

    public OMCLArray(IEnumerable<OMCLItem> items) {
        _items = items.ToList();
    }

    public OMCLArray(params OMCLItem[] items) {
        _items = items.ToList();
    }

    public void Add(OMCLItem item) {
        _items.Add(item);
    }

    public void Add(OMCLObject item) {
        Add(new OMCLItem(item));
    }

    public void Add(OMCLArray item) {
        Add(new OMCLItem(item));
    }

    public IEnumerator<OMCLItem> GetEnumerator()
    {
        return _items.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _items.GetEnumerator();
    }
}


// end namespace
}
