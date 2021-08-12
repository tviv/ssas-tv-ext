using OlapParser.DataRepresentation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OlapParser.MdxGeneration
{
    public class MdxGenerator
    {

        private OlapQueryModel model;

        public MdxGenerator(OlapQueryModel model)
        {
            this.model = model;
        }


        private string GetMdxSetFromTupleBlock(FilterStruct fs, TupleBlock tupleBlock, string itRes)
        {
            string res = itRes;

            if (tupleBlock != null)
            {
                var delim = "";
                foreach (var tbc in tupleBlock.Children)
                {
                    if (tbc is ExprBlock)
                    {
                        //res += !string.IsNullOrEmpty(res) && res.Last() == '}' ? "," : "";
                        res += string.IsNullOrEmpty(delim) ? "{" : "";
                        var i = (ExprBlock)tbc;
                        var item = string.Format("[{0}].[{1}].{2}[{3}]", i.DimName, i.HierName, i.IsKey ? "&" : "", i.Value);
                        res += delim + item;
                        delim = ",";
                    }
                    else if (tbc is TupleBlock)
                    {
                        res = GetMdxSetFromTupleBlock(fs, (TupleBlock)tbc, "");
                    }

                }
                res += string.IsNullOrEmpty(delim) ? "" : "}";
                if (!string.IsNullOrEmpty(res))
                {
                    if (tupleBlock.LogOperation == LogicalOperator.Not)
                    {
                        res = string.Format("[{0}].[{1}].AllMembers-[{0}].[{1}].[(All)]-{2}", fs.DimName, fs.HierName, res);
                    }
                }

            }


            return res;
        }

        public string GetContextQueryStrictSet(string dimPattern, string hierPattern = "")
        {
            string res = "";

            var delim = "";
            if (model.FilterStructs != null)
            {
                foreach (var fs in model.FilterStructs)
                {
                    if (!string.IsNullOrWhiteSpace(dimPattern) && !Regex.IsMatch(fs.DimName, dimPattern)) { continue; }
                    if (!string.IsNullOrWhiteSpace(hierPattern) && !Regex.IsMatch(fs.HierName, hierPattern)) {continue; }

                    if (fs.TupleBlock != null && fs.TupleBlock.Children.Count > 0)
                    {                      
                        res += delim + GetMdxSetFromTupleBlock(fs, fs.TupleBlock, "");
                        //if (!String.IsNullOrWhiteSpace(res))
                        delim = ",";
                    }
                }
            }

            res = string.Format("({0})", string.IsNullOrWhiteSpace(res) ? "{}" : res);

            return res;
        }
    }
}
