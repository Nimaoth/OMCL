

using System.IO;
using System.Text;

namespace OMCL.Serialization {

internal enum TokenType {

    Unknown,

    EOF,

    Newline,
    Comma,

    OpenBrace,
    ClosingBrace,
    OpenBracket,
    ClosingBracket,

    Equals,

    String,
    Int,
    Float,
    Bool,

    KwTrue,
    KwFalse,
    KwNone,

    Tag
}

public class Span {
    public int Line;
    public int StartIndex;
    public int EndIndex;
    public int LineStartIndex;

    public int Column => StartIndex - LineStartIndex + 1;

    public override string ToString() => $"{this.Line}:{this.Column}";

    public Span Clone() => new Span {
        Line = this.Line,
        StartIndex = this.StartIndex,
        EndIndex = this.EndIndex,
        LineStartIndex = this.LineStartIndex
    };
}

internal class Token {
    public TokenType Type { get; internal set; }

    public object value { get; internal set; }

    public Span Location { get; internal set; }

    public override string ToString() => $"({Location}) ({Type}) {value}";
}

internal class Lexer {

    private string mText;

    private Span mLocation;

    private char Current => mText[mLocation.StartIndex];
    private char Next => mLocation.StartIndex < mText.Length - 1 ? mText[mLocation.StartIndex + 1] : (char)0;
    private char Prev => mLocation.StartIndex > 0 ? mText[mLocation.StartIndex - 1] : (char)0;

    private Lexer() {}

    public static Lexer FromFile(string fileName)
    {
        if (!File.Exists(fileName))
        {
            throw new FileNotFoundException($"Failed to open file '{fileName}'");
        }
        return new Lexer
        {
            mText = File.ReadAllText(fileName, Encoding.UTF8).Replace("\r\n", "\n"),
            mLocation = new Span
            {
                Line = 1,
                StartIndex = 0,
                EndIndex = 0,
                LineStartIndex = 0
            }
        };
    }

    public static Lexer FromString(string str)
    {
        return new Lexer
        {
            mText = str.Replace("\r\n", "\n"),
            mLocation = new Span
            {
                Line = 1,
                StartIndex = 0,
                EndIndex = 0,
                LineStartIndex = 0
            }
        };
    }

    public Token PeekToken(bool KeyName = false)
    {
        var oldLocation = mLocation.Clone();

        try {
            return NextToken(KeyName);
        }
        finally {
            mLocation = oldLocation;
        }
    }

    public Token NextToken(bool KeyName = false)
    {
        if (SkipWhitespaceAndComments(out Span loc))
        {
            loc.EndIndex = loc.StartIndex;
            Token tok = new Token();
            tok.Location = loc;
            tok.Type = TokenType.Newline;
            return tok;
        }

        return ReadToken(KeyName);
    }

    private Token ReadToken(bool KeyName = false)
    {
        var token = new Token();
        token.Location = mLocation.Clone();
        token.Location.EndIndex = token.Location.StartIndex;
        token.Type = TokenType.EOF;
        if (mLocation.StartIndex >= mText.Length)
            return token;

        switch (Current)
        {
            case '=': SimpleToken(ref token, TokenType.Equals); break;
            case '{': SimpleToken(ref token, TokenType.OpenBrace); break;
            case '}': SimpleToken(ref token, TokenType.ClosingBrace); break;
            case '[': SimpleToken(ref token, TokenType.OpenBracket); break;
            case ']': SimpleToken(ref token, TokenType.ClosingBracket); break;
            case ',': SimpleToken(ref token, TokenType.Comma); break;

            case '!':
                ParseTag(ref token);
                break;

            case '"':
                ParseStringLiteral(ref token, '"');
                break;
            case '\'':
                ParseStringLiteral(ref token, '\'');
                break;

            case char c when KeyName && IsIdentBegin(c):
                ParseStringLiteral(ref token);
                break;

            // case '"': ParseStringLiteral(ref token, '"'); break;

            // case char cc when IsDigit(cc):
            //     ParseNumberLiteral(ref token);
            //     break;

            default:
                token.Type = TokenType.Unknown;
                mLocation.StartIndex += 1;
                break;
        }

        token.Location.EndIndex = mLocation.StartIndex;
        return token;
    }

    private void ParseTag(ref Token token) {
        token.Type = TokenType.Tag;
        mLocation.StartIndex++;

        var sb = new StringBuilder();

        while (mLocation.StartIndex < mText.Length) {
            char c = Current;

            if (IsTagCharacter(c)) {
                sb.Append(c);
                mLocation.StartIndex++;
            }
            else {
                break;
            }
        }

        token.value = sb.ToString();
    }

    private bool IsTagCharacter(char c) {
        return IsAlpha(c) || IsDigit(c) || c == '_' || c == '-';
    }

    private void ParseStringLiteral(ref Token token, char end) {
        token.Type = TokenType.String;
        mLocation.StartIndex++;
        var sb = new StringBuilder();

        bool foundEnd = false;
        while (mLocation.StartIndex < mText.Length) {
            char c = Current;
            mLocation.StartIndex++;

            if (c == end) {
                foundEnd = true;
                break;
            }

            sb.Append(c);
        }

        if (!foundEnd)
            throw new OMCLParserError(mLocation, $"({mLocation}) Unexpected end of string literal");
        
        token.value = sb.ToString();
    }

    private void ParseStringLiteral(ref Token token) {
        token.Type = TokenType.String;
        var sb = new StringBuilder();

        bool foundEnd = false;
        while (mLocation.StartIndex < mText.Length) {
            char c = Current;
            mLocation.StartIndex++;

            if (!IsIdent(c)) {
                foundEnd = true;
                break;
            }

            sb.Append(c);
        }

        if (!foundEnd)
            throw new OMCLParserError(mLocation, $"({mLocation}) Unexpected end of string literal");
        
        token.value = sb.ToString();
    }

    private void SimpleToken(ref Token token, TokenType type, int len = 1)
    {
        token.Type = type;
        mLocation.StartIndex += len;
    }

    private bool IsDigit(char c)
    {
        return c >= '0' && c <= '9';
    }

    private bool IsIdentBegin(char c)
    {
        return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c == '_';
    }

    private bool IsAlpha(char c)
    {
        return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
    }

    private bool IsIdent(char c)
    {
        return IsIdentBegin(c) || (c >= '0' && c <= '9') || c == '-';
    }

    private bool SkipWhitespaceAndComments(out Span loc)
    {
        loc = null;

        while (mLocation.StartIndex < mText.Length)
        {
            char c = Current;
            if (c == '/' && Next == '*')
            {
                ParseMultiLineComment();
            }

            else if (c == '/' && Next == '/')
            {
                ParseSingleLineComment();
            }

            else if (c == ' ' || c == '\t')
            {
                mLocation.StartIndex++;
            }

            else if (c == '\r')
            {
                mLocation.StartIndex++;
            }

            else if (c == '\n')
            {
                if (loc == null)
                {
                    loc = mLocation.Clone();
                }

                mLocation.Line++;
                mLocation.StartIndex++;
                mLocation.LineStartIndex = mLocation.StartIndex;
            }

            else break;
        }

        if (loc != null)
        {
            loc.EndIndex = mLocation.StartIndex;
            return true;
        }

        return false;
    }

    private void ParseSingleLineComment()
    {
        while (mLocation.StartIndex < mText.Length)
        {
            if (Current == '\n')
                break;
            mLocation.StartIndex++;
        }
    }

    private void ParseMultiLineComment()
    {
        int level = 0;
        while (mLocation.StartIndex < mText.Length)
        {
            char curr = Current;
            char next = Next;
            mLocation.StartIndex++;

            if (curr == '/' && next == '*')
            {
                mLocation.StartIndex++;
                level++;
            }

            else if (curr == '*' && next == '/')
            {
                mLocation.StartIndex++;
                level--;

                if (level == 0)
                    break;
            }

            else if (curr == '\n')
            {
                mLocation.Line++;
                mLocation.LineStartIndex = mLocation.StartIndex;
            }
        }
    }
}

}
