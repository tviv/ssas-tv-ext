using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OlapParser.Parsing.Tokens
{
    public enum TokenType
    {
        NotDefined,
        And,
        Application,
        Between,
        CloseParenthesis,
        Comma,
        DateTimeValue,
        Equals,
        ExceptionType,
        Fingerprint,
        In,
        Invalid,
        Like,
        Limit,
        Match,
        Message,
        NotEquals,
        NotIn,
        NotLike,
        Number,
        Or,
        OpenParenthesis,
        StackFrame,
        StringValue,


        Evaluate,
        Calculatetable,
        TopN,
        Keepfilters,
        All,
        Filter,
        Values,
        Not,
        HierLink,
        DatePresentFunction,

        OrderBy,
        SomeFunction,


        SequenceTerminator
    }
}
