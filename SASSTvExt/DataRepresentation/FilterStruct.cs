using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OlapParser.Parsing.Tokens;

namespace OlapParser.DataRepresentation
{
    public class FilterStruct
    {
        public LogicalOperator LogOpToNextCondition { get; set; }
        public string DimName { get; set; }
        public string HierName { get; set; }

        private string _hierarchyName;
        public string DaxHierarchy {
            get { return _hierarchyName; }

            set
            {
                DimName = OlapQueryModel.getHierParts(value).Item1;
                HierName = OlapQueryModel.getHierParts(value).Item2;
                _hierarchyName = value;
            }
        }

        public TupleBlock TupleBlock {  get; set; }
      

    }

    public abstract class TupleKnot { }

    public class TupleBlock: TupleKnot
    {

        public TupleBlock()
        {
            Children = new List<TupleKnot>();
        }

        public LogicalOperator LogOperation { get; set; } = LogicalOperator.Or;


        //alya compound type
        public List<TupleKnot> Children;
    }

    public class ExprBlock: TupleKnot //Leave
    {
        public string DimName;
        public string HierName;
        public bool IsKey = true;
        public string Value; 

        public string DaxExpr;
        public string MdxExpr;
        public ExprBlock(string dimName, string hierName, bool isKey, string value)
        {
            DimName = dimName;
            HierName = hierName;
            IsKey = isKey;
            Value = value;
        }
    }




}
