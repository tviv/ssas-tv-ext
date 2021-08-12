using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OlapParser.DataRepresentation
{
    public enum DslOperator
    {
        NotDefined,
        Equals,
        NotEquals,
        Like,
        NotLike,
        In,
        NotIn
    }
}
