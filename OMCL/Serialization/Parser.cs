using System;
using System.Collections.Generic;
using System.IO;
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

    private void ReportError(string message) {
        throw new OMCLParserError(NextLocation, $"({NextLocation}) {message}");
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

    public OMCLItem ParseObject() {
        var tags = ParseTags();
        return DoParseObject(tags);
    }

    private OMCLItem DoParseObject(List<string> tags) {
        OMCLObject resultObject = new OMCLObject();

        SkipNewlines();
        if (!Expect(TokenType.OpenBrace)) ReportError("Failed to parse object. Expected '{'");

        if (CheckToken(TokenType.ClosingBrace)) {
            NextToken();
            if (!Expect(TokenType.Newline, TokenType.EOF)) ReportError("Failed to parse object. Expected '\\n'");
            return resultObject;
        }

        if (!Expect(TokenType.Newline)) ReportError("Failed to parse object. Expected '\\n'");

        SkipNewlines();
        while (true) {
            string key = null;

            var next = PeekToken(KeyName: true);

            if (next.Type == TokenType.EOF) {
                ReportError("Unexpected end of file in object");
            }

            if (next.Type == TokenType.ClosingBrace) {
                break;
            }

            if (next.Type != TokenType.String) {
                ReportError("Failed to parse property. Expected property name");
                return null;
            }

            key = next.value as string;

            NextToken(KeyName: true);
            SkipNewlines();

            next = PeekToken();
            if (!(next.Type == TokenType.OpenBrace || next.Type == TokenType.OpenBracket)) {
                if (!Expect(TokenType.Equals)) ReportError("Failed to parse property. Expected '='");
                SkipNewlines();
            }

            var item = ParseItem();
            resultObject.Add(key, item);
        }

        if (!Expect(TokenType.ClosingBrace)) ReportError("Failed to parse object. Expected '}}'");
        if (!Expect(TokenType.Newline, TokenType.EOF)) ReportError("Failed to parse object. Expected '\\n'");

        OMCLItem result = resultObject;
        result.Tags = tags;
        return result;
    }

    public OMCLItem ParseArray() {
        var tags = ParseTags();
        return DoParseArray(tags);
    }

    private OMCLItem DoParseArray(List<string> tags) {
        OMCLArray resultArray = new OMCLArray();

        if (!Expect(TokenType.OpenBracket)) ReportError("Failed to parse array. Expected '['");

        if (CheckToken(TokenType.ClosingBracket)) {
            NextToken();
            if (!Expect(TokenType.Newline, TokenType.EOF)) ReportError("Failed to parse array. Expected '\\n'");
            return resultArray;
        }

        if (!Expect(TokenType.Newline)) ReportError("Failed to parse array. Expected '\\n'");

        SkipNewlines();
        while (true) {
            var next = PeekToken(KeyName: true);

            if (next.Type == TokenType.EOF) {
                ReportError("Unexpected end of file in array");
            }

            if (next.Type == TokenType.ClosingBracket) {
                break;
            }

            var item = ParseItem();
            resultArray.Add(item);
        }

        if (!Expect(TokenType.ClosingBracket)) ReportError("Failed to parse array. Expected ']'");
        if (!Expect(TokenType.Newline, TokenType.EOF)) ReportError("Failed to parse array. Expected '\\n'");

        OMCLItem result = resultArray;
        result.Tags = tags;
        return result;
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

                if (!Expect(TokenType.Newline)) ReportError("Failed to parse string. Expected '\\n'");

                OMCLItem result = sb.ToString();
                result.Tags = tags;
                return result;
            }

        }

        throw new NotImplementedException(nameof(ParseItem));
    }

    private void SkipNewlines()
    {
        while (true)
        {
            var tok = mLexer.PeekToken();

            if (tok.Type == TokenType.EOF)
                break;

            if (tok.Type == TokenType.Newline)
            {
                NextToken();
                continue;
            }

            break;
        }
    }

    /// <summary>
    /// Read the next token from the lexer
    /// </summary>
    private Token NextToken(bool KeyName = false)
    {
        mCurrentToken = mLexer.NextToken(KeyName);
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

    private Token PeekToken(bool KeyName = false)
    {
        return mLexer.PeekToken(KeyName);
    }
}

} // namespace
