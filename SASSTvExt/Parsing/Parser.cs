using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OlapParser.DataRepresentation;
using OlapParser.Exceptions;
using OlapParser.Parsing.Tokens;
using System.Text.RegularExpressions;

namespace OlapParser.Parsing
{
    public class Parser
    {
        private Stack<DslToken> _tokenSequence;
        private DslToken _lookaheadFirst;
        private DslToken _lookaheadSecond;

        private DslQueryModel _queryModel;
        private OlapQueryModel _queryModel1;
        private MatchCondition _currentMatchCondition;
        private FilterStruct _currentFilterStruct;

        private const string ExpectedObjectErrorText = "Expected =, !=, LIKE, NOT LIKE, IN or NOT IN but found: ";

        public OlapQueryModel ParseEval(List<DslToken> tokens)
        {
            LoadSequenceStack(tokens);
            PrepareLookaheads();
            _queryModel1 = new OlapQueryModel();

            Eval();

            DiscardToken(TokenType.SequenceTerminator);

            return _queryModel1;
        }

        private void LoadSequenceStack(List<DslToken> tokens)
        {
            _tokenSequence = new Stack<DslToken>();
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

        private DslToken ReadToken(TokenType tokenType)
        {
            if (_lookaheadFirst.TokenType != tokenType)
                throw new ArgumentException(string.Format("Expected {0} but found: {1}", tokenType.ToString().ToUpper(), _lookaheadFirst.Value));

            return _lookaheadFirst;
        }

        private DslToken DiscardToken()
        {
            var res = _lookaheadFirst;

            _lookaheadFirst = _lookaheadSecond.Clone();

            if (_tokenSequence.Any())
                _lookaheadSecond = _tokenSequence.Pop();
            else
                _lookaheadSecond = new DslToken(TokenType.SequenceTerminator, string.Empty);

            Console.WriteLine(string.Format("{0} \t {1}", _lookaheadFirst.TokenType, _lookaheadFirst.Value));

            return res;
        }

        private DslToken DiscardToken(TokenType tokenType)
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
        private void SkipFunctionNext()
        {
            if (_lookaheadFirst.TokenType == TokenType.CloseParenthesis)
            {
                DiscardToken(TokenType.Comma);
                _currentMatchCondition.Values.Add(ReadToken(TokenType.StringValue).Value);
                DiscardToken(TokenType.StringValue);
                StringLiteralListNext();
            }
            else
            {
                // nothing
            }
        }

        
        private void EqualityMatchCondition()
        {
            _currentMatchCondition.Object = GetObject(_lookaheadFirst);
            DiscardToken();
            _currentMatchCondition.Operator = GetOperator(_lookaheadFirst);
            DiscardToken();
            _currentMatchCondition.Value = _lookaheadFirst.Value;
            DiscardToken();
        }

        private DslObject GetObject(DslToken token)
        {
            switch (token.TokenType)
            {
                case TokenType.Application:
                    return DslObject.Application;
                case TokenType.ExceptionType:
                    return DslObject.ExceptionType;
                case TokenType.Fingerprint:
                    return DslObject.Fingerprint;
                case TokenType.Message:
                    return DslObject.Message;
                case TokenType.StackFrame:
                    return DslObject.StackFrame;
                default:
                    throw new ArgumentException(ExpectedObjectErrorText + token.Value);
            }
        }

        private DaxFunction GetFunction(DslToken token)
        {
            switch (token.TokenType)
            {
                case TokenType.Keepfilters:
                    return DaxFunction.KEEPFILTERS;
                default:
                    throw new ArgumentException(ExpectedObjectErrorText + token.Value);
            }
        }

        private DslOperator GetOperator(DslToken token)
        {
            switch (token.TokenType)
            {
                case TokenType.Equals:
                    return DslOperator.Equals;
                case TokenType.NotEquals:
                    return DslOperator.NotEquals;
                case TokenType.Like:
                    return DslOperator.Like;
                case TokenType.NotLike:
                    return DslOperator.NotLike;
                case TokenType.In:
                    return DslOperator.In;
                case TokenType.NotIn:
                    return DslOperator.NotIn;
                default:
                    throw new ArgumentException("Expected =, !=, LIKE, NOT LIKE, IN, NOT IN but found: " + token.Value);
            }
        }

        private void NotInCondition()
        {
            ParseInCondition(DslOperator.NotIn);
        }

        private void InCondition()
        {
            ParseInCondition(DslOperator.In);
        }

        private void ParseInCondition(DslOperator inOperator)
        {
            _currentMatchCondition.Operator = inOperator;
            _currentMatchCondition.Values = new List<string>();
            _currentMatchCondition.Object = GetObject(_lookaheadFirst);
            DiscardToken();

            if (inOperator == DslOperator.In)
                DiscardToken(TokenType.In);
            else if (inOperator == DslOperator.NotIn)
                DiscardToken(TokenType.NotIn);

            DiscardToken(TokenType.OpenParenthesis);
            StringLiteralList();
            DiscardToken(TokenType.CloseParenthesis);
        }


        private void ParseOrderBy()
        {
            DiscardToken(TokenType.OrderBy);
            while (!IsFirst(TokenType.SequenceTerminator))
            {
                DiscardToken();
            }
        }

        private void StringLiteralList()
        {
            _currentMatchCondition.Values.Add(ReadToken(TokenType.StringValue).Value);
            DiscardToken(TokenType.StringValue);
            StringLiteralListNext();
        }

        private void StringLiteralList2()
        {
            //_currentFilterStruct.Value = ReadToken(TokenType.StringValue).Value;
            DiscardToken(TokenType.StringValue);
            //StringLiteralListNext();
        }

        private void StringLiteralListNext()
        {
            if (_lookaheadFirst.TokenType == TokenType.Comma)
            {
                DiscardToken(TokenType.Comma);
                _currentMatchCondition.Values.Add(ReadToken(TokenType.StringValue).Value);
                DiscardToken(TokenType.StringValue);
                StringLiteralListNext();
            }
            else
            {
                // nothing
            }
        }


        private void DateCondition()
        {
            DiscardToken(TokenType.Between);

            _queryModel.DateRange = new DateRange();
            _queryModel.DateRange.From = DateTime.ParseExact(ReadToken(TokenType.DateTimeValue).Value, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            DiscardToken(TokenType.DateTimeValue);
            DiscardToken(TokenType.And);
            _queryModel.DateRange.To = DateTime.ParseExact(ReadToken(TokenType.DateTimeValue).Value, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            DiscardToken(TokenType.DateTimeValue);
            DateConditionNext();
        }

        private void DateConditionNext()
        {
            if (_lookaheadFirst.TokenType == TokenType.Limit)
            {
                Limit();
            }
            else if (_lookaheadFirst.TokenType == TokenType.SequenceTerminator)
            {
                // nothing
            }
            else
            {
                throw new ArgumentException("Expected LIMIT or the end of the query but found: " + _lookaheadFirst.Value);
            }

        }

        private void Limit()
        {
            DiscardToken(TokenType.Limit);
            int limit = 0;
            bool success = int.TryParse(ReadToken(TokenType.Number).Value, out limit);
            if (success)
                _queryModel.Limit = limit;
            else
                throw new ArgumentException("Expected an integer number but found " + ReadToken(TokenType.Number).Value);

            DiscardToken(TokenType.Number);
        }

        private bool IsObject(DslToken token)
        {
            return token.TokenType == TokenType.Application
                   || token.TokenType == TokenType.ExceptionType
                   || token.TokenType == TokenType.Fingerprint
                   || token.TokenType == TokenType.Message
                   || token.TokenType == TokenType.StackFrame;
        }

        //todo refactor (using getFunction)
        private bool IsFunction(DslToken token)
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

        private bool IsEqualityOperator(DslToken token)
        {
            return token.TokenType == TokenType.Equals
                   || token.TokenType == TokenType.NotEquals
                   || token.TokenType == TokenType.Like
                   || token.TokenType == TokenType.NotLike;
        }

        private void CreateNewMatchCondition()
        {
            _currentMatchCondition = new MatchCondition();
            _queryModel.MatchConditions.Add(_currentMatchCondition);
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
