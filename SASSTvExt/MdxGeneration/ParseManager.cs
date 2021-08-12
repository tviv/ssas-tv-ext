using OlapParser.DataRepresentation;
using OlapParser.Parsing;
using OlapParser.Parsing.Tokenizers.SlowAndSimple;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OlapParser.MdxGeneration
{
    public class ParseManager
    {

        private OlapQueryModel OlapModel;

        public MdxGenerator MdxGenerator { get; }

        public ParseManager(string query)
        {
            var tokenizer = new SimpleRegexTokenizer();
            var parser = new Parser();

            var tokenSequence = tokenizer.Tokenize(query).ToList();
            OlapModel = parser.ParseEval(tokenSequence);

            MdxGenerator = new MdxGenerator(OlapModel);
        }
    }
}
