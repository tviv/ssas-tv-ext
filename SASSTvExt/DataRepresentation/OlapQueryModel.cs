using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OlapParser.DataRepresentation
{
    public class OlapQueryModel
    {
        public OlapQueryModel()
        {
            FilterStructs = new List<FilterStruct>();
        }

        public IList<FilterStruct> FilterStructs { get; set; }


        //helpers
        public static Tuple<string, string> getHierParts(string hier)
        {
            string pattern = hier.StartsWith("[") 
                ? @"\[(?<dim>[а-яА-Яa-zA-Z0-9 ]+)\].\[(?<hier>[а-яА-Яa-zA-Z0-9 ]+)"  //MDX
                : @"'(?<dim>[а-яА-Яa-zA-Z0-9 ]+)'\[(?<hier>[а-яА-Яa-zA-Z0-9 ]+)"; //DAX
            var m = Regex.Match(hier, pattern);
            if (m.Success)
            {
                return new Tuple<string, string>(m.Groups["dim"].Value, m.Groups["hier"].Value);
            }

            return null;
        }


    }


    //public struct DimHierStruct
    //{
    //    string DimName;
    //    string HierName;
    //}

}
