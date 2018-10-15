

using System;
using System.Globalization;
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

    Tag,

    Identifier
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

    private char Current => mLocation.StartIndex < mText.Length ? mText[mLocation.StartIndex] : '\0';
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

    public Token PeekToken()
    {
        var oldLocation = mLocation.Clone();

        try {
            return NextToken();
        }
        finally {
            mLocation = oldLocation;
        }
    }

    public Token NextToken()
    {
        if (SkipWhitespaceAndComments(out Span loc))
        {
            loc.EndIndex = loc.StartIndex;
            Token tok = new Token();
            tok.Location = loc;
            tok.Type = TokenType.Newline;
            return tok;
        }

        return ReadToken();
    }

    private Token ReadToken()
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

            case char c when IsIdentBegin(c): {
                ParseIdentifier(ref token);
                CheckKeywords(ref token);
                break;
            }

            case char cc when IsDigit(cc):
                ParseNumberLiteral(ref token);
                break;

            default:
                token.Type = TokenType.Unknown;
                mLocation.StartIndex += 1;
                break;
        }

        token.Location.EndIndex = mLocation.StartIndex;
        return token;
    }

    private void CheckKeywords(ref Token token) {
        switch (token.value as string) {
            case "true": token.Type = TokenType.KwTrue; break;
            case "false": token.Type = TokenType.KwFalse; break;
            case "none": token.Type = TokenType.KwNone; break;
        }
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

    private void ParseIdentifier(ref Token token) {
        token.Type = TokenType.Identifier;
        var sb = new StringBuilder();

        while (mLocation.StartIndex < mText.Length) {
            char c = Current;

            if (!IsIdent(c)) {
                break;
            }

            sb.Append(c);
            mLocation.StartIndex++;
        }

        token.value = sb.ToString();
    }

    private void SimpleToken(ref Token token, TokenType type, int len = 1)
    {
        token.Type = type;
        mLocation.StartIndex += len;
    }

    private void ParseNumberLiteral(ref Token token) {
        token.Type = TokenType.Int;

        var sb = new StringBuilder();

        Func<char, bool> isDigit = IsDigit;

        int b = 10;
        bool canBeFloat = true;
        bool isFloat = false;

        if (Current == '0') {
            switch (Next) {
            case 'b':
                b = 2;
                isDigit = CreateIsDigit(2);
                mLocation.StartIndex += 2;
                canBeFloat = false;
                break;
                
            case 'o':
                b = 8;
                isDigit = CreateIsDigit(8);
                mLocation.StartIndex += 2;
                canBeFloat = false;
                break;

            case 'x':
                b = 16;
                isDigit = CreateIsDigit(16);
                mLocation.StartIndex += 2;
                canBeFloat = false;
                break;
            }
        }


        var state = ParseNumberState.Start;
        while (state != ParseNumberState.End) {
            var c = Current;

            switch (state) {
            case ParseNumberState.Start:
                if (isDigit(c)) {
                    sb.Append(c);
                    state = ParseNumberState.IntDigit;
                }
                else state = ParseNumberState.End;
                break;

            case ParseNumberState.IntDigit:
                if (isDigit(c)) sb.Append(c); // unchanged
                else if (c == '_') state = ParseNumberState.IntDigitUnderscore;
                else if (c == '.') {
                    sb.Append(c);
                    state = ParseNumberState.Period;
                    isFloat = true;
                }
                else state = ParseNumberState.End;
                break;

            case ParseNumberState.IntDigitUnderscore:
                if (isDigit(c)) {
                    sb.Append(c);
                    state = ParseNumberState.IntDigit;
                }
                else if (c == '_') ; // unchanged
                else ReportError($"Invalid character in number literal: {c}");
                break;

            case ParseNumberState.Period:
                if (IsDigit(c)) {
                    sb.Append(c);
                    state = ParseNumberState.FloatDigit;
                }
                else ReportError($"Invalid character in number literal: {c}");
                break;

            case ParseNumberState.FloatDigit:
                if (IsDigit(c)) sb.Append(c); // unchanged
                else if (c == '_') state = ParseNumberState.FloatDigitUnderscore;
                else if (c == 'e') {
                    sb.Append(c);
                    state = ParseNumberState.Exponent;
                }
                else state = ParseNumberState.End;
                break;

            case ParseNumberState.FloatDigitUnderscore:
                if (IsDigit(c)) {
                    sb.Append(c);
                    state = ParseNumberState.FloatDigit;
                }
                else if (c == '_') ; // unchanged
                else ReportError($"Invalid character in number literal: {c}");
                break;

            case ParseNumberState.Exponent:
                if (IsDigit(c)) {
                    sb.Append(c);
                    state = ParseNumberState.ExponentDigit;
                }
                else if (c == '+') {
                    sb.Append(c);
                    state = ParseNumberState.ExponentPlus;
                }
                else if (c == '-') {
                    sb.Append(c);
                    state = ParseNumberState.ExponentMinus;
                }
                else ReportError($"Invalid character in number literal: {c}");
                break;

            case ParseNumberState.ExponentPlus:
                if (IsDigit(c)) {
                    sb.Append(c);
                    state = ParseNumberState.ExponentDigit;
                }
                else ReportError($"Invalid character in number literal: {c}");
                break;

            case ParseNumberState.ExponentMinus:
                if (IsDigit(c)) {
                    sb.Append(c);
                    state = ParseNumberState.ExponentDigit;
                }
                else ReportError($"Invalid character in number literal: {c}");
                break;

            case ParseNumberState.ExponentDigit:
                if (IsDigit(c)) {
                    sb.Append(c);
                }
                else state = ParseNumberState.End;
                break;
            }

            if (state != ParseNumberState.End)
                mLocation.StartIndex++;
        }

        if (isFloat && !canBeFloat)
            ReportError($"Invalid number literal. Can't be a float!");
        
        if (isFloat) {
            if (double.TryParse(sb.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double value)) {
                token.Type = TokenType.Float;
                token.value = value;
            }
            else {
                ReportError($"Invalid float literal: '{sb}'");
            }
        }
        else {
            try {
                token.Type = TokenType.Int;
                token.value = Convert.ToInt64(sb.ToString(), b);
            }
            catch (Exception e) {
                ReportError($"Invalid int literal: '{sb}': {e.Message}");
            }
        }
    }

    private void ReportError(string message) {
        throw new OMCLParserError(mLocation, $"({mLocation}) {message}");
    }

    private enum ParseNumberState {
        Start,
        End,
        IntDigit,
        IntDigitUnderscore,
        Period,
        FloatDigit,
        Exponent,
        FloatDigitUnderscore,
        ExponentDigit,
        ExponentMinus,
        ExponentPlus,
    }

    private bool IsTagCharacter(char c) {
        return IsAlpha(c) || IsDigit(c) || c == '_' || c == '-';
    }

    private bool IsDigit(char c)
    {
        return c >= '0' && c <= '9';
    }

    private Func<char, bool> CreateIsDigit(int b)
    {
        return c =>  (c >= '0' && c <= ('0' + Math.Min(b, 10) - 1)) ||
                (c >= 'a' && c <= ('a' + b - 11)) ||
                (c >= 'A' && c <= ('A' + b - 11));
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
        return IsIdentBegin(c) || IsDigit(c) || c == '-';
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
