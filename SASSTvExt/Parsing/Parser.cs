using System;
using System.Collections.Generic;
using System.Linq;
using OlapParser.DataRepresentation;
using OlapParser.Parsing.Tokens;
using System.Text.RegularExpressions;

namespace OlapParser.Parsing
{
    public class Parser
    {
        private Stack<Token> _tokenSequence;
        private Token _lookaheadFirst;
        private Token _lookaheadSecond;

        private OlapQueryModel _queryModel1;
        private FilterStruct _currentFilterStruct;

        private const string ExpectedObjectErrorText = "Expected =, !=, LIKE, NOT LIKE, IN or NOT IN but found: ";

        public OlapQueryModel ParseEval(List<Token> tokens)
        {
            LoadSequenceStack(tokens);
            PrepareLookaheads();
            _queryModel1 = new OlapQueryModel();

            Eval();

            DiscardToken(TokenType.SequenceTerminator);

            return _queryModel1;
        }

        private void LoadSequenceStack(List<Token> tokens)
        {
            _tokenSequence = new Stack<Token>();
            int count = tokens.Count;
            for (int i = count - 1; i >= 0; i--)
            {
                _tokenSequence.Push(tokens[i]);
            }
        }

        private void PrepareLookaheads()
        {
            _lookaheadFirst = _tokenSequence.Pop();
            _lookaheadSecond = _tokenSequence.Pop();
        }

        private Token ReadToken(TokenType tokenType)
        {
            if (_lookaheadFirst.TokenType != tokenType)
                throw new ArgumentException(string.Format("Expected {0} but found: {1}", tokenType.ToString().ToUpper(), _lookaheadFirst.Value));

            return _lookaheadFirst;
        }

        private Token DiscardToken()
        {
            var res = _lookaheadFirst;

            _lookaheadFirst = _lookaheadSecond.Clone();

            if (_tokenSequence.Any())
                _lookaheadSecond = _tokenSequence.Pop();
            else
                _lookaheadSecond = new Token(TokenType.SequenceTerminator, string.Empty);

            Console.WriteLine(string.Format("{0} \t {1}", _lookaheadFirst.TokenType, _lookaheadFirst.Value));

            return res;
        }

        private Token DiscardToken(TokenType tokenType)
        {
            if (_lookaheadFirst.TokenType != tokenType)
                throw new ArgumentException(string.Format("Expected {0} but found: {1}", tokenType.ToString().ToUpper(), _lookaheadFirst.Value));

            return DiscardToken();
        }


        private void Eval()
        {
            DiscardToken(TokenType.Evaluate);
            if (IsFirst(TokenType.TopN))
            {
                DiscardToken(TokenType.TopN);
                DiscardToken(TokenType.OpenParenthesis);
                DiscardToken();
                DiscardToken(TokenType.Comma);
                EvaluateCondition();
                DiscardToken(TokenType.Comma);
                SkipFunctionBody();
                DiscardToken(TokenType.CloseParenthesis);
                if (IsFirst(TokenType.OrderBy)) ParseOrderBy();
            } else
                EvaluateCondition();
        }


        private void EvaluateCondition()
        {
            //if (IsFunction(_lookaheadFirst))
            //{
            if (_lookaheadSecond.TokenType == TokenType.OpenParenthesis)
            {
                ParseCalcuatable();
            }
            else
            {
                throw new ArgumentException(ExpectedObjectErrorText + " " + _lookaheadSecond.Value);
            }

            //MatchConditionNext();
            //}
            //else
            //{
            //    throw new DslParserException(ExpectedObjectErrorText + _lookaheadFirst.Value);
            //}
        }

        private void ParseCalcuatable()
        {
            DiscardToken(TokenType.Calculatetable);

            DiscardToken(TokenType.OpenParenthesis);

            SkipFunction(); //param 1
            ParseCalcutableParamNext();

            DiscardToken(TokenType.CloseParenthesis);
        }

        private void ParseCalcutableParamNext()
        {
            if (_lookaheadFirst.TokenType == TokenType.Comma)
            {
                DiscardToken();
                if (_lookaheadFirst.TokenType == TokenType.Keepfilters)
                    ParseContextKeepfilters();
                else if (_lookaheadFirst.TokenType == TokenType.All)
                    ParseFunctionAll();
                else
                    SkipFunction();

                ParseCalcutableParamNext();
            }
        }

        private void ParseFunctionAll()
        {
            CreateNewCondClause();

            DiscardToken(TokenType.All);
            DiscardToken(TokenType.OpenParenthesis);
            if (IsFirst(TokenType.HierLink))
            {
                var hierParts = OlapQueryModel.getHierParts(HierLinkToHier(DiscardToken(TokenType.HierLink).Value));
                _queryModel1.FilterStructs.Last().DimName = hierParts.Item1;
                _queryModel1.FilterStructs.Last().HierName = hierParts.Item2;
                AddTupleBlock().Children.Add(new ExprBlock(hierParts.Item1, hierParts.Item2, false, "(All)"));

            }
            else
            {
                ExceptExceptButNotFound(TokenType.HierLink);
            }
            DiscardToken(TokenType.CloseParenthesis);

        }
        private void ParseContextKeepfilters()
        {
            CreateNewCondClause();

            DiscardToken(TokenType.Keepfilters);
            DiscardToken(TokenType.OpenParenthesis);

            if (_lookaheadFirst.TokenType == TokenType.Filter)
            {
                ParseFilterFunction();
            }
            else if (_lookaheadFirst.TokenType == TokenType.HierLink)
            {

            }
            else
            {
                throw new ArgumentException("Expected valid Keepfilters parameters, but found: " + _lookaheadFirst.Value);
            }

            //SkipFunctionBody();
            DiscardToken(TokenType.CloseParenthesis);
        }

        //helper
        private static string HierLinkToHier(string hierLink) { return hierLink.Replace(".Key0", "").Replace(".UniqueName", ""); }
        //helper
        private bool IsFirst(TokenType tt) { return _lookaheadFirst.TokenType == tt; }
        //helper
        private void ExceptExceptButNotFound(TokenType tt)
        {
            throw new ArgumentException(string.Format("Expected {0} but found: {1}", tt.ToString().ToUpper(), _lookaheadFirst.Value));
        }



        private void ParseFilterFunction()
        {
            DiscardToken(TokenType.Filter);
            DiscardToken(TokenType.OpenParenthesis);
            {
                DiscardToken(TokenType.Keepfilters);
                DiscardToken(TokenType.OpenParenthesis);
                {
                    DiscardToken(TokenType.Values);
                    DiscardToken(TokenType.OpenParenthesis);
                    {
                        //if (!IsFirst(TokenType.HierLink)) ExceptExceptButNotFound(TokenType.HierLink);
                        _queryModel1.FilterStructs.Last().DaxHierarchy = HierLinkToHier(DiscardToken(TokenType.HierLink).Value);
                    }
                    DiscardToken(TokenType.CloseParenthesis); //values
                }
                DiscardToken(TokenType.CloseParenthesis); //keepfilters
                DiscardToken(TokenType.Comma);

                if (IsFirst(TokenType.HierLink))
                {
                    ParseExprBlock(AddTupleBlock());
                }
                else
                {
                    ParseLogicalFunction(AddTupleBlock());
                }
            }
            DiscardToken(TokenType.CloseParenthesis); //filter
        }

        public void ParseLogicalFunction(TupleBlock tupleBlock)
        {
            if (IsFirst(TokenType.Not))
            {
                tupleBlock.LogOperation = LogicalOperator.Not;
                DiscardToken();
                DiscardToken(TokenType.OpenParenthesis);
                ParseLogicalFunctionParam(tupleBlock, TokenType.Not);
                DiscardToken(TokenType.CloseParenthesis);
            }
            else if (IsFirst(TokenType.Or))
            {
                tupleBlock.LogOperation = LogicalOperator.Or;
                DiscardToken();
                DiscardToken(TokenType.OpenParenthesis);
                ParseLogicalFunctionParam(tupleBlock, TokenType.Or);
                DiscardToken(TokenType.Comma);
                ParseLogicalFunctionParam(tupleBlock, TokenType.Or);
                DiscardToken(TokenType.CloseParenthesis);
            }
            else if (IsFirst(TokenType.And))
            {
                SkipFunction(); //todo it here temprarilly
            }
            else
            {
                throw new ArgumentException(string.Format("Expected NOT or OR but found: {0}", _lookaheadFirst.Value));
            }
        }

        public void ParseLogicalFunctionParam(TupleBlock tupleBlock, TokenType ownerToken)
        {
            if (IsFirst(TokenType.HierLink))
            {
                ParseExprBlock(tupleBlock);
            }
            else
            {
                //only for simplify
                var tb = tupleBlock;
                if (ownerToken != _lookaheadFirst.TokenType)
                {
                    tb = new TupleBlock();
                    tupleBlock.Children.Add(tb);
                }
                ParseLogicalFunction(tb);
            }
        }

        private void ParseExprBlock(TupleBlock tupleBlock)
        {
            var hierLink = DiscardToken(TokenType.HierLink).Value;
            DiscardToken(TokenType.Equals);
            string value;
            var isDatePresentFunction = IsFirst(TokenType.DatePresentFunction);
            if (isDatePresentFunction)
            {
                value = daxDateToXmlDate(DiscardToken().Value);
            }
            else if (IsUnameHierLink(hierLink))
            {
                value = uniqueNameToKey(DiscardToken().Value);
            }
            else
            {
                value = DiscardToken().Value;
            }

            var hier = HierLinkToHier(hierLink);
            var isKey = hierLink != hier || isDatePresentFunction;
            var dimName = OlapQueryModel.getHierParts(hier).Item1;
            var hierName = OlapQueryModel.getHierParts(hier).Item2;

            tupleBlock.Children.Add(new ExprBlock(dimName, hierName, isKey, value));
        }

        private TupleBlock AddTupleBlock()
        {
            var tuple = new TupleBlock();
            _queryModel1.FilterStructs.Last().TupleBlock = tuple;

            return tuple;
        }

        private void SkipFunction()
        {

            if (IsFunction(_lookaheadFirst))
            {
                DiscardToken();
                DiscardToken(TokenType.OpenParenthesis);
                SkipFunctionBody();
                DiscardToken(TokenType.CloseParenthesis);
            }
            else
            {
                throw new ArgumentException("Expected  function, but found: " + _lookaheadFirst.TokenType);

            }
        }


        private void SkipFunctionBody()
        {
            while (_lookaheadFirst.TokenType != TokenType.CloseParenthesis && _lookaheadFirst.TokenType != TokenType.SequenceTerminator)
            {
                if (IsFunction(_lookaheadFirst))
                {
                    SkipFunction();
                }
                else
                {
                    DiscardToken();
                }

            }
        }

        private void ParseOrderBy()
        {
            DiscardToken(TokenType.OrderBy);
            while (!IsFirst(TokenType.SequenceTerminator))
            {
                DiscardToken();
            }
        }


        //todo refactor (using getFunction)
        private bool IsFunction(Token token)
        {
            return token.TokenType == TokenType.Keepfilters
                || token.TokenType == TokenType.Calculatetable
                || token.TokenType == TokenType.SomeFunction
                || token.TokenType == TokenType.Filter
                || token.TokenType == TokenType.Values
                || token.TokenType == TokenType.And
                || token.TokenType == TokenType.Or
                || token.TokenType == TokenType.Not;
        }

        private void CreateNewCondClause()
        {
            _currentFilterStruct = new FilterStruct();
            _queryModel1.FilterStructs.Add(_currentFilterStruct);
        }



        //helpers
        public static string daxDateToXmlDate(string input)
        {
            string pattern = DATE_FUN_PATTERN;

            return Regex.Replace(input, pattern,
                m =>
                {
                    var ret = string.Format(@"{0}-{1:D2}-{2:D2}T00:00:00", m.Groups["year"].Value, Int32.Parse(m.Groups["mon"].Value), Int32.Parse(m.Groups["day"].Value));
                    return ret;
                }
                , RegexOptions.Singleline);
        }


        private bool IsUnameHierLink(string hierLink)
        {
            return Regex.IsMatch(hierLink, @"\.UniqueName");
        }

        public static string uniqueNameToKey(string input)
        {
            string pattern = @"\.&\[(?<key>\S+)\]";

            var match = Regex.Match(input, pattern);
            if (match.Success)
            {
                return match.Groups["key"].Value;
            }
            else
            {
                return "";
            }
        }

        public static String DATE_FUN_PATTERN = @"^DATE\s*\(\s*(?<year>\d{4}),\s*(?<mon>\d{1,2}),\s*(?<day>\d{1,2})\s*\)";
    }

}
