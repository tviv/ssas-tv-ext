using OlapParser.Parsing.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OlapParser.Parsing.Tokenizers
{
    public interface ITokenizer
    {
        IEnumerable<Token> Tokenize(string queryDsl);
    }
}
