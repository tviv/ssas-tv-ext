using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using OlapParser.Exceptions;
using OlapParser.Parsing.Tokens;
using OlapParser.Parsing.Tokens.Tokenizers.SlowAndSimple;

namespace OlapParser.Parsing.Tokenizers.SlowAndSimple
{
    public class SimpleRegexTokenizer : ITokenizer
    {
        private List<TokenDefinition> _tokenDefinitions;

        public SimpleRegexTokenizer()
        {
            _tokenDefinitions = new List<TokenDefinition>();

            _tokenDefinitions.Add(new TokenDefinition(TokenType.HierLink, @"^(?<hierLink>'?(?<dim>[а-яА-Яa-zA-Z0-9 ]+)'?\[(?<hier>[а-яА-Яa-zA-Z0-9 ]+)((?<key>\.Key0)|(?<uname>\.UniqueName))*\])"));
            _tokenDefinitions.Add(new TokenDefinition(TokenType.DatePresentFunction, Parser.DATE_FUN_PATTERN));


            _tokenDefinitions.Add(new TokenDefinition(TokenType.And, "^and"));
            _tokenDefinitions.Add(new TokenDefinition(TokenType.Application, "^app|^application"));
            _tokenDefinitions.Add(new TokenDefinition(TokenType.Between, "^between"));
            _tokenDefinitions.Add(new TokenDefinition(TokenType.CloseParenthesis, "^\\)"));
            _tokenDefinitions.Add(new TokenDefinition(TokenType.Comma, "^,"));
            _tokenDefinitions.Add(new TokenDefinition(TokenType.Equals, "^="));
            _tokenDefinitions.Add(new TokenDefinition(TokenType.ExceptionType, "^ex|^exception"));
            _tokenDefinitions.Add(new TokenDefinition(TokenType.Fingerprint, "^fingerprint"));
            _tokenDefinitions.Add(new TokenDefinition(TokenType.NotIn, "^not in"));
            _tokenDefinitions.Add(new TokenDefinition(TokenType.In, "^in"));
            _tokenDefinitions.Add(new TokenDefinition(TokenType.Like, "^like"));
            _tokenDefinitions.Add(new TokenDefinition(TokenType.Limit, "^limit"));
            _tokenDefinitions.Add(new TokenDefinition(TokenType.Match, "^match"));
            _tokenDefinitions.Add(new TokenDefinition(TokenType.Message, "^msg|^message"));
            _tokenDefinitions.Add(new TokenDefinition(TokenType.NotEquals, "^!="));
            _tokenDefinitions.Add(new TokenDefinition(TokenType.NotLike, "^not like"));
            _tokenDefinitions.Add(new TokenDefinition(TokenType.OpenParenthesis, "^\\("));
            _tokenDefinitions.Add(new TokenDefinition(TokenType.StackFrame, "^sf|^stackframe"));
            _tokenDefinitions.Add(new TokenDefinition(TokenType.DateTimeValue, "^\\d\\d\\d\\d-\\d\\d-\\d\\d \\d\\d:\\d\\d:\\d\\d"));
            _tokenDefinitions.Add(new TokenDefinition(TokenType.StringValue, @"^""[^""]*"""));
            _tokenDefinitions.Add(new TokenDefinition(TokenType.Number, "^\\d+"));

            _tokenDefinitions.Add(new TokenDefinition(TokenType.Evaluate, "^evaluate"));
            _tokenDefinitions.Add(new TokenDefinition(TokenType.Calculatetable, "^Calculatetable"));
            _tokenDefinitions.Add(new TokenDefinition(TokenType.TopN, "^Topn"));
            _tokenDefinitions.Add(new TokenDefinition(TokenType.Keepfilters, "^keepfilters"));
            _tokenDefinitions.Add(new TokenDefinition(TokenType.All, "^all"));
            _tokenDefinitions.Add(new TokenDefinition(TokenType.Values, "^values"));
            _tokenDefinitions.Add(new TokenDefinition(TokenType.Filter, "^filter"));
            _tokenDefinitions.Add(new TokenDefinition(TokenType.OrderBy, "^order by"));
            _tokenDefinitions.Add(new TokenDefinition(TokenType.Or, "^or"));
            _tokenDefinitions.Add(new TokenDefinition(TokenType.Not, "^not"));
            _tokenDefinitions.Add(new TokenDefinition(TokenType.SomeFunction, @"^(?<value>\S+?)\s*\("));


        }


        public IEnumerable<DslToken> Tokenize(string lqlText)
        {
            var tokens = new List<DslToken>();

            string remainingText = lqlText;

            while (!string.IsNullOrWhiteSpace(remainingText))
            {
                var match = FindMatch(remainingText);
                if (match.IsMatch)
                {
                    tokens.Add(new DslToken(match.TokenType, match.Value));
                    remainingText = match.RemainingText;
                }
                else
                {
                    if (IsWhitespace(remainingText))
                    {
                        remainingText = remainingText.Substring(1);
                    }
                    else
                    {
                        var invalidTokenMatch = CreateInvalidTokenMatch(remainingText);
                        tokens.Add(new DslToken(invalidTokenMatch.TokenType, invalidTokenMatch.Value));
                        remainingText = invalidTokenMatch.RemainingText;
                    }
                }
            }

            tokens.Add(new DslToken(TokenType.SequenceTerminator, string.Empty));

            return tokens;
        }

        private TokenMatch FindMatch(string lqlText)
        {
            foreach (var tokenDefinition in _tokenDefinitions)
            {
                var match = tokenDefinition.Match(lqlText);
                if (match.IsMatch)
                    return match;
            }

            return new TokenMatch() {  IsMatch = false };
        }

        private bool IsWhitespace(string lqlText)
        {
            return Regex.IsMatch(lqlText, "^\\s+");
        }

        private TokenMatch CreateInvalidTokenMatch(string lqlText)
        {
            var match = Regex.Match(lqlText, @"(^[^,\)\s]+)");
            if (match.Success)
            {
                return new TokenMatch()
                {
                    IsMatch = true,
                    RemainingText = lqlText.Substring(match.Length),
                    TokenType = TokenType.Invalid,
                    Value = match.Value.Trim()
                };
            }

            throw new ArgumentException("Failed to generate invalid token");
        }
    }
}
