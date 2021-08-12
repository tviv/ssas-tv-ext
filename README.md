# ssas-tv-ext
The library is intended for parsing DAX queries and obtaining its structured model. It is also possible to convert DAX filter expressions into mdx ones (this was the main goal of this project).
Examples of use can be found in the Parser Test.cs file.  

Example:
```cs
string query = @"
  EVALUATE
    CALCULATETABLE(
      ROW(
        ""Turnover"", 'Common'[Turnover]
      ),
      KEEPFILTERS(
        FILTER(
          KEEPFILTERS(VALUES('Dates'[Date])),
          OR('Dates'[Date] = DATE(2019, 4, 1), 'Dates'[Date] = DATE(2019, 4, 2))
        )
      )
    )";

var pm = new ParseManager(query);
var res = pm.MdxGenerator.GetContextQueryStrictSet(@"Dates");

Assert.AreEqual(@"({[Dates].[Date].&[2019-04-01T00:00:00],[Dates].[Date].&[2019-04-02T00:00:00]})", res);
```
