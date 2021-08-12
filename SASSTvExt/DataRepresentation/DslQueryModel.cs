using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OlapParser.DataRepresentation
{
    public class DslQueryModel
    {
        public DslQueryModel()
        {
            MatchConditions = new List<MatchCondition>();
        }

        public DateRange DateRange { get; set; }
        public int? Limit { get; set; }
        public IList<MatchCondition> MatchConditions { get; set; }
    }
}
