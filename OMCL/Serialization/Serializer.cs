using System.IO;
using System.Text;
using OMCL.Data;

namespace OMCL.Serialization {

public class Serializer {

    private TextWriter _writer;
    private int _indentationLevel = 0;
    private int _indentationSize = 4;

    public void Serialize(OMCLItem item) {
        switch (item.Type) {
        case OMCLItem.OMCLItemType.Object:
            Write(item.AsObject());
            break;
        case OMCLItem.OMCLItemType.Array:
            Write(item.AsArray());
            break;
        case OMCLItem.OMCLItemType.String:
            Write(item.AsString());
            break;
        case OMCLItem.OMCLItemType.Bool:
            Write(item.AsBool());
            break;
        case OMCLItem.OMCLItemType.Int:
            Write(item.AsInt());
            break;
        case OMCLItem.OMCLItemType.Float:
            Write(item.AsFloat());
            break;
        }

        _writer.Flush();
        _writer.Close();
    }

    public static Serializer ToStringBuilder(StringBuilder sb) {
        return new Serializer {
            _writer = new StringWriter(sb)
        };
    }

    public static Serializer ToFile(string path) {
        return new Serializer {
            _writer = new StreamWriter(File.Open(path, FileMode.Create, FileAccess.Write), Encoding.UTF8)
        };
    }

    private void Indent(int add = 0) {
        _writer.Write(new string(' ', (add + _indentationLevel) * _indentationSize));
    }

    public void Write(OMCLObject obj) {
        _writer.Write("{");
        if (obj.Empty) {
            _writer.WriteLine("}");
            return;
        }

        _writer.WriteLine();

        foreach (var (key, val) in obj) {
            Indent(1);
            if (PropNameNeedsQuotation(key)) {
                _writer.Write('"');
                _writer.Write(key);
                _writer.Write('"');
            }
            else {
                _writer.Write(key);
            }
                
            if (val.Type == OMCLItem.OMCLItemType.Object || val.Type == OMCLItem.OMCLItemType.Array) {
                _writer.Write(" ");
            }
            else {
                _writer.Write(" = ");
            }

            WriteValue(val);
        }

        Indent();
        _writer.WriteLine("}");
    }

    public void Write(OMCLArray obj) {
        _writer.Write("[");
        if (obj.Empty) {
            _writer.WriteLine("]");
            return;
        }

        _writer.WriteLine();

        foreach (var val in obj) {
            Indent(1);
            WriteValue(val);
        }

        Indent();
        _writer.WriteLine("]");
    }

    public void Write(string obj) {
        _writer.Write('"');
        _writer.Write(obj);
        _writer.WriteLine('"');

    }

    public void Write(long l) {
        // _writer.Write("!f ");
        _writer.WriteLine(l);
    }

    public void Write(double d) {
        // _writer.Write("!f ");
        _writer.WriteLine(d);
    }

    public void Write(bool b) {
        // _writer.Write("!b ");
        _writer.WriteLine(b);
    }

    private void WriteValue(OMCLItem item) {
        var prev = _indentationLevel++;
        try {
            switch (item.Type) {
            case OMCLItem.OMCLItemType.Object:
                Write(item.AsObject());
                break;
            case OMCLItem.OMCLItemType.Array:
                Write(item.AsArray());
                break;
            case OMCLItem.OMCLItemType.String:
                var str = item.AsString();
                var parts = str.Split('"');
                var escaped = string.Join("\" '\"' \"", parts);
                Write(escaped);
                break;
            case OMCLItem.OMCLItemType.Bool:
                Write(item.AsBool());
                break;
            case OMCLItem.OMCLItemType.Int:
                Write(item.AsInt());
                break;
            case OMCLItem.OMCLItemType.Float:
                Write(item.AsFloat());
                break;
            }
        }
        finally {
            _indentationLevel = prev;
        }
    }

    #region Helpers

    private bool PropNameNeedsQuotation(string name) {
        foreach (char c in name) {
            if ((c >= 'a' && c <= 'z') ||
                (c >= 'A' && c <= 'Z') ||
                (c >= '0' && c <= '9') ||
                (c == '_') ||
                (c == '.'))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    #endregion
}

}