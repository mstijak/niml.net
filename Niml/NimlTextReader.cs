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
            PushState(State.Limbo);
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

        struct StateEntry
        {
            public State State;
            public bool PreserveWhiteSpace;
            public bool Nest;
            internal bool NewLine;
            internal bool Raw;
        }

        void PushState(State state, bool nest = false, bool preserveWhiteSpace = false)
        {
            states.Push(new StateEntry
            {
                State = state,
                Nest = nest,
                PreserveWhiteSpace = preserveWhiteSpace
            });
        }
        
        Stack<StateEntry> states = new Stack<StateEntry>();
        StateEntry state;

        char prevChar, c = '\0';

        void RepeatLast()
        {
            c = prevChar;
            pos--;
        }

        public bool Read()
        {
            while (true)
            {
                prevChar = c;
                if (!GetNextChar(out c))
                    return false;

                if (c == '\r')
                {
                    c = prevChar;
                    continue;
                }

                state = states.Peek();
                switch (state.State)
                {
                    case State.Limbo:
                        switch (c)
                        {
                            case '+':
                                return Report(NimlToken.EnterElement);

                            case '-':
                                return Report(NimlToken.ExitElement);

                            case '<':
                                PushState(State.LimboMultilineText);
                                continue;

                            default:
                                if (Char.IsLetter(c))
                                {
                                    buffer.Clear();
                                    buffer.Append(c);
                                    PushState(State.TagName);
                                }
                                continue;
                        }

                    case State.TagName:
                        switch (c)
                        {
                            case ' ':
                                states.Pop();
                                PushState(State.TagBody);
                                return Report(NimlToken.Element);

                            case '\n':
                                states.Pop();
                                return Report(NimlToken.Element);

                            case '+':
                                states.Pop();
                                PushState(State.TagBody);
                                RepeatLast();
                                return Report(NimlToken.Element);

                            default:
                                buffer.Append(c);
                                continue;
                        }

                    case State.TagBody:

                        if (prevChar == '/' && c == '>')
                        {
                            states.Pop();
                            return Report(NimlToken.CloseLast);
                        }

                        switch (c)
                        {
                            case ' ':
                                continue;

                            case '+':
                                return Report(NimlToken.EndElement);

                            case '\n':
                                states.Pop();
                                continue;

                            case '<':
                                states.Pop();
                                PushState(State.MultilineText);
                                buffer.Clear();
                                continue;

                            case '{':
                                PushState(State.Attributes);
                                continue;

                            case '|':
                                states.Pop();
                                continue;

                            case '\"':
                                states.Pop();
                                PushState(State.InlineText);
                                PushState(State.QuotedText);
                                buffer.Clear();
                                buffer.Append(c);
                                continue;

                            default:
                                states.Pop();
                                PushState(State.InlineText);
                                buffer.Clear();
                                buffer.Append(c);
                                continue;
                        }

                    case State.InlineText:

                        if (prevChar == '/' && c == '>')
                        {
                            states.Pop();
                            PushState(State.TagBody);
                            --buffer.Length;
                            RepeatLast();
                            return Report(NimlToken.InlineText);
                        }

                        switch (c)
                        {
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

                        switch (c)
                        {
                            case ':':
                                if (buffer.Length == 0 && !state.NewLine)
                                {
                                    state.Raw = true;
                                    continue;
                                }
                                break;

                            case '\n':
                                if (buffer.Length == 0 && !state.NewLine)
                                {
                                    state.NewLine = true;
                                    continue;
                                }
                                break;

                            case '"':
                                if (buffer.Length == 0 && !state.NewLine)
                                {
                                    PushState(State.QuotedText);
                                    continue;
                                }
                                break;

                            case '>':
                                if (prevChar == '\n' || prevChar == '"')
                                {
                                    buffer.Length--;
                                    states.Pop();
                                    if (buffer.Length > 0)
                                        return Report(state.State == State.LimboMultilineText ? NimlToken.Text : NimlToken.MultilineText);
                                    continue;
                                }
                                break;
                        }

                        if (prevChar == '<' && Char.IsLetter(c))
                        {
                            --buffer.Length;
                            state.PreserveWhiteSpace = true;
                            PushState(State.TagName, nest: true);
                            RepeatLast();
                            return Report(state.State == State.LimboMultilineText ? NimlToken.Text : NimlToken.MultilineText);
                        }

                        buffer.Append(c);
                        continue;

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
                                PushState(State.AttributeName);
                                continue;
                        }

                    case State.AttributeName:
                        switch (c)
                        {
                            case ':':
                                states.Pop();
                                PushState(State.AttributeValue);
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
                            case ' ':
                                if (buffer.Length > 0)
                                {
                                    states.Pop();
                                    return Report(NimlToken.AttributeValue);
                                }
                                continue;

                            case ',':
                            case '\n':
                            case '\t':
                                states.Pop();
                                return Report(NimlToken.AttributeValue);

                            case '}':
                                states.Pop();
                                states.Pop();
                                return Report(NimlToken.AttributeValue);

                            default:
                                if (buffer.Length == 0 && c == '\"')
                                {
                                    PushState(State.QuotedText);
                                    continue;
                                }
                                buffer.Append(c);
                                continue;
                        }

                    case State.QuotedText:
                        switch (c)
                        {
                            case '\n':
                                if (buffer.Length == 0)
                                    continue;
                                break;

                            case '"':
                                states.Pop();
                                continue;

                            default:
                                buffer.Append(c);
                                continue;
                        }

                        if (buffer.Length > 0 && prevChar == '"')
                        {
                            states.Pop();
                            RepeatLast();
                            continue;
                        }

                        buffer.Append(c);
                        continue;
                }
            }
        }

        readonly StringBuilder buffer = new StringBuilder();

        public string Value { get; private set; }

        char[] rb = new char[1024];
        int pos = 0;
        int cnt = 0;
        bool fakeLineReported = false;

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

            if (!fakeLineReported)
            {
                fakeLineReported = true;
                c = '\n';
                return true;
            }

            c = ' ';
            return false;
        }

        bool Report(NimlToken token)
        {
            Token = token;
            Nest = state.Nest;
            Raw = state.Raw;

            var l = buffer.Length;
            if (l > 0 && !state.PreserveWhiteSpace && buffer[buffer.Length - 1] == ' ')
                l--;
            Value = buffer.ToString(0, l);
            buffer.Clear();            
            return true;
        }

        public bool Nest { get; private set; }
        public bool Raw { get; private set; }
        public NimlToken Token { get; private set; }
    }
}
