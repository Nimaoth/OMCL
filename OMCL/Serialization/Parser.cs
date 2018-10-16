using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using OMCL.Data;

namespace OMCL.Serialization {

[System.Serializable]
public class OMCLParserError : System.Exception
{

    public Span Location { get; private set; }

    public OMCLParserError(Span loc) {
        Location = loc;
    }
    public OMCLParserError(Span loc, string message) : base(message) {
        Location = loc;
    }
}

public class Parser {

    private Lexer mLexer;
    private Token lastNonWhitespace = null;
    private Token mCurrentToken = null;

    private Span NextLocation => PeekToken().Location;

    public static Parser FromFile(string filename) {
        return new Parser {
            mLexer = Lexer.FromFile(filename)
        };
    }

    public static Parser FromString(string str) {
        return new Parser {
            mLexer = Lexer.FromString(str)
        };
    }

    private void ReportError(string message) {
        throw new OMCLParserError(NextLocation, $"({NextLocation}) {message}");
    }

    public OMCLItem ParseItem() {
        var tags = ParseTags();

        SkipNewlines();

        var next = PeekToken();
        switch (next.Type) {
            case TokenType.OpenBrace:
                return DoParseObject(tags);

            case TokenType.OpenBracket:
                return DoParseArray(tags);

            case TokenType.String: {
                NextToken();

                var sb = new StringBuilder();
                sb.Append(next.value as string);

                while (true) {
                    next = PeekToken();
                    if (next.Type == TokenType.String) {
                        sb.Append(next.value as string);
                        NextToken();
                    }
                    else
                        break;
                }

                OMCLItem result = sb.ToString();
                result.Tags = tags;
                return result;
            }

            case TokenType.KwFalse: goto case TokenType.KwTrue;
            case TokenType.KwTrue: {
                NextToken();
                OMCLItem result = next.Type == TokenType.KwTrue;
                result.Tags = tags;
                return result;
            }

            case TokenType.KwNone: {
                NextToken();
                OMCLItem result = new OMCLNone();
                result.Tags = tags;
                return result;
            }

            case TokenType.Int: {
                NextToken();
                OMCLItem result = (long)next.value;
                result.Tags = tags;
                return result;
            }

            case TokenType.Float: {
                NextToken();
                OMCLItem result = (double)next.value;
                result.Tags = tags;
                return result;
            }
        }

        throw new NotImplementedException(nameof(ParseItem));
    }

    private List<string> ParseTags() {
        var tags = new List<string>();
        
        var next = PeekToken();
        while (next.Type == TokenType.Tag) {
            tags.Add(next.value as string);
            NextToken();
            SkipNewlines();
            next = PeekToken();
        }

        return tags;
    }

    public OMCLObject ParseObject() {
        return DoParseObject(TokenType.EOF);
    }
 
    private OMCLItem DoParseObject(List<string> tags) {
        SkipNewlines();
        if (!Expect(TokenType.OpenBrace)) ReportError("Failed to parse object. Expected '{'");

        if (CheckToken(TokenType.ClosingBrace)) {
            NextToken();
            return new OMCLObject();
        }

        var resultObject = DoParseObject(TokenType.ClosingBrace);

        if (!Expect(TokenType.ClosingBrace)) ReportError("Failed to parse object. Expected '}}'");

        OMCLItem result = resultObject;
        result.Tags = tags;
        return result;
    }

    private OMCLObject DoParseObject(params TokenType[] delimiters) {
        OMCLObject resultObject = new OMCLObject();
        SkipNewlines();

        var next = PeekToken();
        if (delimiters.Any(d => (d == next.Type)))
            return resultObject;

        while (true) {
            string key = null;

            if (next.Type != TokenType.String && next.Type != TokenType.Identifier) {
                ReportError("Failed to parse property. Expected property name (string or identifier)");
                return null;
            }

            key = next.value as string;

            NextToken();
            SkipNewlines();

            next = PeekToken();
            if (!(next.Type == TokenType.OpenBrace || next.Type == TokenType.OpenBracket)) {
                if (!Expect(TokenType.Equals)) ReportError("Failed to parse property. Expected '='");
                SkipNewlines();
            }

            var item = ParseItem();
            resultObject.Add(key, item);

            bool nl = SkipNewlines();
            next = PeekToken();
            if (delimiters.Any(d => (d == next.Type))) {
                break;
            }
            else if (next.Type == TokenType.Comma) {
                NextToken();
                SkipNewlines();
                next = PeekToken();
                if (delimiters.Any(d => (d == next.Type)))
                    break;
            }
            else if (!nl) {
                ReportError("Failed to parse object. Expected '\\n' or ',' after property");
            }
        }

        return resultObject;
    }

    public OMCLItem ParseArray() {
        var tags = ParseTags();
        return DoParseArray(tags);
    }

    private OMCLItem DoParseArray(List<string> tags) {
        OMCLArray resultArray = new OMCLArray();

        if (!Expect(TokenType.OpenBracket)) ReportError("Failed to parse array. Expected '['");

        SkipNewlines();

        var next = PeekToken();
        while (true) {
            if (next.Type == TokenType.EOF) {
                ReportError("Unexpected end of file in array");
                return null;
            }

            if (next.Type == TokenType.ClosingBracket) {
                break;
            }

            var item = ParseItem();
            resultArray.Add(item);

            bool nl = SkipNewlines();
            next = PeekToken();
            if (next.Type == TokenType.ClosingBracket) {
                break;
            }
            else if (next.Type == TokenType.Comma) {
                NextToken();
                SkipNewlines();
                next = PeekToken();
            }
            else if (!nl) {
                ReportError("Failed to parse array. Expected '\\n' or ',' after value in array");
                return null;
            }
        }

        if (!Expect(TokenType.ClosingBracket)) {
            ReportError("Failed to parse array. Expected ']'");
            return null;
        }

        OMCLItem result = resultArray;
        result.Tags = tags;
        return result;
    }

    private bool SkipNewlines()
    {
        bool skippedNewlines = false;
        while (true)
        {
            var tok = mLexer.PeekToken();

            if (tok.Type == TokenType.EOF)
                break;

            if (tok.Type == TokenType.Newline)
            {
                skippedNewlines = true;
                NextToken();
                continue;
            }

            break;
        }

        return skippedNewlines;
    }

    /// <summary>
    /// Read the next token from the lexer
    /// </summary>
    private Token NextToken()
    {
        mCurrentToken = mLexer.NextToken();
        if (mCurrentToken.Type != TokenType.Newline)
            lastNonWhitespace = mCurrentToken;
        return mCurrentToken;
    }

    /// <summary>
    /// Consume the next token if it has the specified type, return false if it has not
    /// </summary>
    private bool Expect(params TokenType[] types)
    {
        var tok = PeekToken();

        foreach (var t in types) {
            if (tok.Type == t)
            {
                NextToken();
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Check wether the next token is the specified type
    /// </summary>
    private bool CheckToken(TokenType type)
    {
        var next = PeekToken();
        return next.Type == type;
    }

    private Token PeekToken()
    {
        return mLexer.PeekToken();
    }
}

} // namespace
