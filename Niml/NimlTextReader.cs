using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Niml
{
    public class NimlTextReader
    {
        private readonly TextReader r;

        public NimlTextReader(TextReader tr)
        {
            r = tr;
            states.Push(State.Limbo);
        }

        enum State
        {
            Limbo,

            TagName,

            InlineText,
            TagBody,
            MultilineText,

            Attributes,
            AttributeName,
            AttributeValue,
            QuotedText,
            LimboMultilineText
        }

        NimlToken token;

        public NimlToken Token { get { return token; } }

        Queue<NimlToken> futureTokens = new Queue<NimlToken>();
        Stack<State> states = new Stack<State>();        

        public bool Read()
        {
            if (futureTokens.Count > 0)
            {
                token = futureTokens.Dequeue();
                return true;
            }

            char prevChar, c = '\0';

            while (true)
            {
                prevChar = c;
                if (!GetNextChar(out c))
                    return false;

                var state = states.Peek();
                switch (state)
                {
                    case State.Limbo:
                        switch (c)
                        {
                            case '+':
                                return Report(NimlToken.AddToLastElement);

                            case '-':
                                return Report(NimlToken.IndentDecrease);

                            case '<':
                                states.Push(State.LimboMultilineText);
                                continue;

                            default:
                                if (Char.IsLetter(c))
                                {
                                    buffer.Clear();
                                    buffer.Append(c);
                                    states.Push(State.TagName);
                                }
                                continue;
                        }

                    case State.TagName:
                        switch (c)
                        {
                            case ' ':                                
                                states.Pop();
                                states.Push(State.TagBody);
                                return Report(NimlToken.StartTag);

                            case '\r':
                            case '\n':
                                states.Pop();
                                return Report(NimlToken.StartTag);

                            case '+':
                                states.Pop();
                                states.Push(State.TagBody);
                                futureTokens.Enqueue(NimlToken.EnterElement);
                                return Report(NimlToken.StartTag);                                

                            default:
                                buffer.Append(c);
                                continue;
                        }

                    case State.TagBody:

                        if (prevChar=='/' && c == '>')
                        {
                            states.Pop();
                            continue;
                        } 

                        switch (c)
                        {
                            case ' ':
                                continue;

                            case '\r':
                            case '\n':
                                states.Pop();
                                continue;

                            case '<':
                                states.Pop();
                                states.Push(State.MultilineText);
                                buffer.Clear();
                                continue;

                            case '{':
                                states.Push(State.Attributes);
                                continue;

                            case '|':
                                states.Pop();
                                continue;

                            default:
                                states.Pop();
                                states.Push(State.InlineText);
                                buffer.Clear();
                                buffer.Append(c);
                                continue;
                        }

                    case State.InlineText:

                        if (prevChar == '/' && c == '>')
                        {
                            states.Pop();
                            --buffer.Length;
                            if (buffer.Length > 0)
                                return Report(NimlToken.InlineText);
                            continue;
                        }

                        switch (c)
                        {
                            case '\r':
                            case '\n':
                            case '|':
                                states.Pop();
                                if (buffer.Length > 0)
                                    return Report(NimlToken.InlineText);
                                    
                                continue;

                            default:
                                buffer.Append(c);
                                continue;
                        }

                    case State.LimboMultilineText:
                    case State.MultilineText:
                        if (prevChar == '<' && Char.IsLetter(c))
                        {
                            --buffer.Length;

                            Report(state == State.LimboMultilineText ? NimlToken.Text : NimlToken.MultilineText);

                            buffer.Append(c);
                            states.Push(State.TagName);
                            return true;
                        }

                        switch (c)
                        {
                            case '>':
                                states.Pop();
                                if (buffer.Length > 0)
                                    return Report(state == State.LimboMultilineText ? NimlToken.Text : NimlToken.MultilineText);

                                continue;

                            default:
                                buffer.Append(c);
                                continue;
                        }

                    case State.Attributes:
                        switch (c)
                        {
                            case '}':
                                states.Pop();
                                continue;

                            case '\r':
                            case '\n':
                            case ' ':
                            case ',':
                                continue;

                            default:
                                buffer.Clear();
                                buffer.Append(c);
                                states.Push(State.AttributeName);
                                continue;

                        }

                    case State.AttributeName:
                        switch (c)
                        {
                            case ':':
                                states.Pop();
                                states.Push(State.AttributeValue);
                                return Report(NimlToken.AttributeName);

                            case ',':
                                states.Pop();
                                return Report(NimlToken.AttributeName);

                            case '}':
                                states.Pop();
                                states.Pop();
                                return Report(NimlToken.AttributeName);

                            case ' ':
                                if (buffer.Length == 0)
                                    continue;
                                buffer.Append(c);
                                continue;

                            default:
                                buffer.Append(c);
                                continue;
                        }

                    case State.AttributeValue:
                        switch (c)
                        {
                            case ',':
                                states.Pop();
                                return Report(NimlToken.AttributeValue);

                            case ' ':
                                states.Pop();
                                return Report(NimlToken.AttributeValue);

                            case '}':
                                states.Pop();
                                states.Pop();
                                return Report(NimlToken.AttributeValue);

                            default:
                                if (buffer.Length == 0 && c == '\"')
                                {
                                    states.Push(State.QuotedText);
                                    continue;
                                }
                                buffer.Append(c);
                                continue;
                        }

                    case State.QuotedText:
                        switch (c)
                        {
                            case '"':
                                states.Pop();
                                continue;

                            default:
                                buffer.Append(c);
                                continue;
                        }
                }
            }
        }

        readonly StringBuilder buffer = new StringBuilder();

        public String Value { get; private set; }

        char[] rb = new char[1024];
        int pos = 0;
        int cnt = 0;

        bool GetNextChar(out char c)
        {
            if (pos < cnt)
            {
                c = rb[pos];
                pos++;
                return true;
            }

            cnt = r.Read(rb, 0, rb.Length);
            if (cnt > 0)
            {
                pos = 0;
                return GetNextChar(out c);
            }

            c = ' ';
            return false;
        }

        bool Report(NimlToken token)
        {
            this.token = token;
            this.Value = buffer.ToString();
            buffer.Clear();
            return true;
        }
    }
}
