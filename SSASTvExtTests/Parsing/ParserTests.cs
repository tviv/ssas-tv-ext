using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using OlapParser.MdxGeneration;
using OlapParser.Parsing;
using OlapParser.Parsing.Tokenizers.SlowAndSimple;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OlapParser.Parsing.Tests
{
    [TestClass()]
    public class ParserTests
    {
        [TestMethod()]
        public void ParseEvalTest1()
        {

            var tokenizer = new SimpleRegexTokenizer();

            var parser = new Parser();

            string query = @"
EVALUATE
  CALCULATETABLE(
    CALCULATETABLE(
      ROW (
        ""GMROI"", 'Общее'[GMROI],
        ""GMROI_Прогресс_пгп\"", 'Общее'[GMROI Прогресс пгп]
      ),
      KEEPFILTERS(
        FILTER(
          KEEPFILTERS(VALUES('Даты'[Месяц.Key0])),
          OR(
            NOT(ISBLANK('Общее'[GMROI])),
            NOT(ISBLANK('Общее'[GMROI Прогресс пгп]))
          )
        )
      )
    ),
    KEEPFILTERS(
      FILTER(
        KEEPFILTERS(VALUES('Даты'[Месяц.Key0])),
          NOT(
          OR(
            OR(
              OR(
                OR('Даты'[Месяц.Key0] = DATE(2019, 1, 1), 'Даты'[Месяц.Key0] = DATE(2019, 2, 1)),
                'Даты'[Месяц.Key0] = DATE(2019, 3, 1)
              ),
              'Даты'[Месяц.Key0] = DATE(2019, 12, 1)
            ),
            'Даты'[Месяц.Key0] = DATE(2019, 11, 1)
          )
        )
      )
    )
   ,
   KEEPFILTERS(
      FILTER(KEEPFILTERS(VALUES('Даты'[Год])), 'Даты'[Год] = 2019)
   )
  )
";

            var pm = new ParseManager(query);
            var res = pm.MdxGenerator.GetContextQueryStrictSet(@"");

            Assert.AreEqual(@"([Даты].[Месяц].AllMembers-[Даты].[Месяц].[(All)]-{[Даты].[Месяц].&[2019-01-01T00:00:00],[Даты].[Месяц].&[2019-02-01T00:00:00],[Даты].[Месяц].&[2019-03-01T00:00:00],[Даты].[Месяц].&[2019-12-01T00:00:00],[Даты].[Месяц].&[2019-11-01T00:00:00]},{[Даты].[Год].[2019]})", res);

            res = pm.MdxGenerator.GetContextQueryStrictSet(@"^Даты.*");

            Assert.AreEqual(@"([Даты].[Месяц].AllMembers-[Даты].[Месяц].[(All)]-{[Даты].[Месяц].&[2019-01-01T00:00:00],[Даты].[Месяц].&[2019-02-01T00:00:00],[Даты].[Месяц].&[2019-03-01T00:00:00],[Даты].[Месяц].&[2019-12-01T00:00:00],[Даты].[Месяц].&[2019-11-01T00:00:00]},{[Даты].[Год].[2019]})", res);
        }



        [TestMethod()]
        public void ParseEvalTest2()
        {
            string query = @"
EVALUATE
  CALCULATETABLE(
    CALCULATETABLE(
      ROW(
        ""GMROI"", 'Общее'[GMROI],
        ""Коэф_оборачиваемости"", 'Бюджет'[Коэф оборачиваемости]
      ),
      KEEPFILTERS(
        FILTER(
          KEEPFILTERS(VALUES('Подразделения'[Подразделение.Key0])),
          OR(
            NOT(ISBLANK('Общее'[GMROI])),
            NOT(ISBLANK('Бюджет'[Коэф оборачиваемости]))
          )
        )
      )
    ),
    KEEPFILTERS(
      FILTER(
        KEEPFILTERS(VALUES('Даты'[Год])),
        OR('Даты'[Год] = 2019, 'Даты'[Год] = 2020)
      )
    ),
    KEEPFILTERS(
      FILTER(
        KEEPFILTERS(VALUES('Даты'[Месяц.Key0])),
        'Даты'[Месяц.Key0] = DATE(2020, 1, 1)
      )
    ),
    KEEPFILTERS(
      FILTER(
        KEEPFILTERS(VALUES('Товары'[Сегмент.Key0])),
        NOT('Товары'[Сегмент.Key0] = 109997)
      )
    ),
    KEEPFILTERS(
      FILTER(
        KEEPFILTERS(VALUES('Подразделения'[Анализируемый.Key0])),
        'Подразделения'[Анализируемый.Key0] = 1
      )
    )
    ,ALL('Продажные'[Наличие.Key0])
  )
";

            var pm = new ParseManager(query);
            var res =  pm.MdxGenerator.GetContextQueryStrictSet(@"");

            Assert.AreEqual(@"({[Даты].[Год].[2019],[Даты].[Год].[2020]},{[Даты].[Месяц].&[2020-01-01T00:00:00]},[Товары].[Сегмент].AllMembers-[Товары].[Сегмент].[(All)]-{[Товары].[Сегмент].&[109997]},{[Подразделения].[Анализируемый].&[1]},{[Продажные].[Наличие].[(All)]})", res);

            res = pm.MdxGenerator.GetContextQueryStrictSet(@"^Даты.*");

            Assert.AreEqual(@"({[Даты].[Год].[2019],[Даты].[Год].[2020]},{[Даты].[Месяц].&[2020-01-01T00:00:00]})", res);

        }



    [TestMethod()]
    public void ParseEval_DimShop_Test()
    {
        string query = @"
EVALUATE
  CALCULATETABLE(
    CALCULATETABLE(
      ROW(
        ""v__выполнения_плана_выручки_без_НДС"", 'Бюджет'[% выполнения плана выручки без НДС],
        ""v__выполнения_плана_маржи_без_НДС"", 'Бюджет'[% выполнения плана маржи без НДС],
        ""Сумма_без_НДС"", 'Общее'[Сумма без НДС],
        ""Маржа_без_НДС__"", 'Общее'[Маржа без НДС %]
      ),
      KEEPFILTERS(
        FILTER(
          KEEPFILTERS(VALUES('Подразделения'[Подразделение.Key0])),
          OR(
            OR(
              OR(
                NOT(ISBLANK('Бюджет'[% выполнения плана выручки без НДС])),
                NOT(ISBLANK('Бюджет'[% выполнения плана маржи без НДС]))
              ),
              NOT(ISBLANK('Общее'[Сумма без НДС]))
            ),
            NOT(ISBLANK('Общее'[Маржа без НДС %]))
          )
        )
      )
    ),
    KEEPFILTERS(
      FILTER(
        KEEPFILTERS(VALUES('Даты'[Месяц.Key0])),
        'Даты'[Месяц.Key0] = DATE(2020, 2, 1)
      )
    ),
    KEEPFILTERS(
      FILTER(
        KEEPFILTERS(VALUES('Даты'[Год])),
        AND(
          OR('Даты'[Год] = 2019, 'Даты'[Год] = 2020),
          OR('Даты'[Год] = 2019, 'Даты'[Год] = 2020)
        )
      )
    ),
    KEEPFILTERS(
      FILTER(
        KEEPFILTERS(VALUES('Даты'[Это полный день.UniqueName])),
        'Даты'[Это полный день.UniqueName] = ""[Даты].[Это полный день].&[Да]""
      )
    ),
    KEEPFILTERS(
      FILTER(
        KEEPFILTERS(VALUES('Товары'[Сегмент.Key0])),
        NOT('Товары'[Сегмент.Key0] = 109997)
      )
    ),
    KEEPFILTERS(
      FILTER(
        KEEPFILTERS(VALUES('Подразделения'[Анализируемый.Key0])),
        'Подразделения'[Анализируемый.Key0] = 1
      )
    ),
    KEEPFILTERS(
      FILTER(
        KEEPFILTERS(VALUES('Подразделения'[Подразделение.Key0])),
        OR(
          OR(
            OR(
              OR(
                OR(
                  'Подразделения'[Подразделение.Key0] = 201,
                  'Подразделения'[Подразделение.Key0] = 202
                ),
                'Подразделения'[Подразделение.Key0] = 203
              ),
              'Подразделения'[Подразделение.Key0] = 206
            ),
            'Подразделения'[Подразделение.Key0] = 208
          ),
          'Подразделения'[Подразделение.Key0] = 209
        )
      )
    )
  )
";

        var pm = new ParseManager(query);
        var res = pm.MdxGenerator.GetContextQueryStrictSet(@"");

        Assert.AreEqual(@"({[Даты].[Месяц].&[2020-02-01T00:00:00]},{[Даты].[Это полный день].&[Да]},[Товары].[Сегмент].AllMembers-[Товары].[Сегмент].[(All)]-{[Товары].[Сегмент].&[109997]},{[Подразделения].[Анализируемый].&[1]},{[Подразделения].[Подразделение].&[201],[Подразделения].[Подразделение].&[202],[Подразделения].[Подразделение].&[203],[Подразделения].[Подразделение].&[206],[Подразделения].[Подразделение].&[208],[Подразделения].[Подразделение].&[209]})", res);

        res = pm.MdxGenerator.GetContextQueryStrictSet(@"^Подразделения.*", "Подразделение");
            

        Assert.AreEqual(@"({[Подразделения].[Подразделение].&[201],[Подразделения].[Подразделение].&[202],[Подразделения].[Подразделение].&[203],[Подразделения].[Подразделение].&[206],[Подразделения].[Подразделение].&[208],[Подразделения].[Подразделение].&[209]})", res);

    }


        [TestMethod()]
        public void ParseEval_DateWithoutKey_Test()
        {
            string query = @"
EVALUATE
  CALCULATETABLE(
    ROW(
      ""Товарооборот"", 'Общее'[Товарооборот]
    ),
    KEEPFILTERS(
      FILTER(
        KEEPFILTERS(VALUES('Даты'[Дата])),
        OR('Даты'[Дата] = DATE(2019, 4, 1), 'Даты'[Дата] = DATE(2019, 4, 2))
      )
    )
  )";

            var pm = new ParseManager(query);
            var res = pm.MdxGenerator.GetContextQueryStrictSet(@"Даты");

            Assert.AreEqual(@"({[Даты].[Дата].&[2019-04-01T00:00:00],[Даты].[Дата].&[2019-04-02T00:00:00]})", res);

 
        }

        [TestMethod()]
        public void ParseEval_TwoDates_Test()
        {
            string query = @"
EVALUATE
  CALCULATETABLE(
    CALCULATETABLE(
      ROW(
        ""План_ТО_дд"", 'Общее'[План ТО дд]
      ),
      KEEPFILTERS(
        FILTER(
          KEEPFILTERS(VALUES('Подразделения'[Подразделение.Key0])),
          OR(
            OR(
              NOT(ISBLANK('Общее'[План ТО дд])),
              NOT(ISBLANK('Общее'[Выполнение плана ТО дд]))
            ),
            NOT(ISBLANK('Общее'[Прогресс товарооборот пгп дд]))
          )
        )
      )
    ),
    KEEPFILTERS(
      FILTER(
        KEEPFILTERS(VALUES('Даты'[Месяц.Key0])),
        'Даты'[Месяц.Key0] = DATE(2020, 5, 1)
      )
    ),
    KEEPFILTERS(
      FILTER(
        KEEPFILTERS(VALUES('Даты'[Дата])),
        OR('Даты'[Дата] = DATE(2020, 5, 3), 'Даты'[Дата] = DATE(2020, 5, 4))
      )
    )
  )
  ";

            var pm = new ParseManager(query);
            var res = pm.MdxGenerator.GetContextQueryStrictSet(@"Даты");

            Assert.AreEqual(@"({[Даты].[Месяц].&[2020-05-01T00:00:00]},{[Даты].[Дата].&[2020-05-03T00:00:00],[Даты].[Дата].&[2020-05-04T00:00:00]})", res);


        }


        [TestMethod()]
        public void ParseEval_TwoDateInTop_Test()
        {
            string query = @"
EVALUATE
  TOPN(
    5,
    CALCULATETABLE(
      ADDCOLUMNS(
        KEEPFILTERS(
          FILTER(
            KEEPFILTERS(
              SUMMARIZE(
                VALUES('Подразделения'),
                'Подразделения'[Подразделение.Key0],
                'Подразделения'[Подразделение]
              )
            ),
            OR(
              OR(
                NOT(ISBLANK('Общее'[План ТО дд])),
                NOT(ISBLANK('Общее'[Выполнение плана ТО дд]))
              ),
              NOT(ISBLANK('Общее'[Прогресс товарооборот пгп дд]))
            )
          )
        ),
        ""План_ТО_дд"", 'Общее'[План ТО дд]
      ),
      KEEPFILTERS(
        FILTER(
          KEEPFILTERS(VALUES('Даты'[Месяц.Key0])),
          'Даты'[Месяц.Key0] = DATE(2020, 5, 1)
        )
      ),
      KEEPFILTERS(
        FILTER(
          KEEPFILTERS(VALUES('Даты'[Дата])),
          OR('Даты'[Дата] = DATE(2020, 5, 3), 'Даты'[Дата] = DATE(2020, 5, 4))
        )
      )
    ),
    'Подразделения'[Подразделение.Key0],
    1,
    'Подразделения'[Подразделение],
    1
  )

ORDER BY
  'Подразделения'[Подразделение.Key0], 'Подразделения'[Подразделение]
";

            var pm = new ParseManager(query);
            var res = pm.MdxGenerator.GetContextQueryStrictSet(@"Даты");

            Assert.AreEqual(@"({[Даты].[Месяц].&[2020-05-01T00:00:00]},{[Даты].[Дата].&[2020-05-03T00:00:00],[Даты].[Дата].&[2020-05-04T00:00:00]})", res);

 
        }

        [TestMethod()]
        public void ParseEval_TwoDateASMounthHierWithSpacesInDateFunc_Test()
        {
            string query = @"
EVALUATE
CALCULATETABLE (
    ROW ( 
    ""План_Маржа_без_НДС"", 'Бюджет'[План Маржа без НДС] 
    ,""test324"", 'Общее'[test324]
    ),
    KEEPFILTERS (
        FILTER (
            KEEPFILTERS ( VALUES ( 'Даты'[Месяц.Key0] ) ),
            OR (
                'Даты'[Месяц.Key0] = DATE ( 2019, 4, 1 ),
                'Даты'[Месяц.Key0] = DATE ( 2019, 3, 1 )
            )
        )
    )
)
";

            var pm = new ParseManager(query);
            var res = pm.MdxGenerator.GetContextQueryStrictSet(@"Даты");

            Assert.AreEqual(@"({[Даты].[Месяц].&[2019-04-01T00:00:00],[Даты].[Месяц].&[2019-03-01T00:00:00]})", res);
        }

        [TestMethod()]
        public void ParseEval_TwoDateASMounthHierComplexWithSpacesInDateFunc_Test()
        {
            string query = @"
EVALUATE
  CALCULATETABLE(
    ROW(
      ""План_Выручка_без_НДС"", 'Бюджет'[План Выручка без НДС]
    ),
    KEEPFILTERS(
      FILTER(
        KEEPFILTERS(VALUES('Даты'[Год])),
        AND(
          OR('Даты'[Год] = 2019, 'Даты'[Год] = 2020),
          OR('Даты'[Год] = 2019, 'Даты'[Год] = 2020)
        )
      )
    ),
    KEEPFILTERS(
      FILTER(
        KEEPFILTERS(VALUES('Даты'[Месяц.Key0])),
        OR('Даты'[Месяц.Key0] = DATE(2020, 5, 1), 'Даты'[Месяц.Key0] = DATE(2020, 4, 1))
      )
    ),
    KEEPFILTERS(
      FILTER(
        KEEPFILTERS(VALUES('Даты'[Это полный день.UniqueName])),
        'Даты'[Это полный день.UniqueName] = ""[Даты].[Это полный день].&[Да]""
      )
    ),
    KEEPFILTERS(
      FILTER(
        KEEPFILTERS(VALUES('Товары'[Сегмент.Key0])),
        NOT('Товары'[Сегмент.Key0] = 109997)
      )
    ),
    KEEPFILTERS(
      FILTER(
        KEEPFILTERS(VALUES('Подразделения'[Анализируемый.Key0])),
        'Подразделения'[Анализируемый.Key0] = 1
      )
    )
  )";

            var pm = new ParseManager(query);
            var res = pm.MdxGenerator.GetContextQueryStrictSet(@"Даты");

            Assert.AreEqual(@"({[Даты].[Месяц].&[2020-05-01T00:00:00],[Даты].[Месяц].&[2020-04-01T00:00:00]},{[Даты].[Это полный день].&[Да]})", res);
        }


        [TestMethod()]
        //todo add AND function to parser
        public void ParseEval_TwoDateASMounthHierAndYear_Test()
        {
            string query = @"
EVALUATE
  CALCULATETABLE(
    ROW(
      ""План_Выручка_без_НДС"", 'Бюджет'[План Выручка без НДС],
      ""test324"", 'Общее'[test324]
    ),
    KEEPFILTERS(
      FILTER(
        KEEPFILTERS(VALUES('Даты'[Месяц.Key0])),
        OR('Даты'[Месяц.Key0] = DATE(2019, 3, 1), 'Даты'[Месяц.Key0] = DATE(2019, 4, 1))
      )
    ),
    KEEPFILTERS(
      FILTER(
        KEEPFILTERS(VALUES('Даты'[Год])),
        AND('Даты'[Год] = 2019, OR('Даты'[Год] = 2019, 'Даты'[Год] = 2020))
      )
    ),
    KEEPFILTERS(
      FILTER(
        KEEPFILTERS(VALUES('Даты'[Это полный день.UniqueName])),
        'Даты'[Это полный день.UniqueName] = ""[Даты].[Это полный день].&[Да]""
      )
    ),
    KEEPFILTERS(
      FILTER(
        KEEPFILTERS(VALUES('Товары'[Сегмент.Key0])),
        NOT('Товары'[Сегмент.Key0] = 109997)
      )
    ),
    KEEPFILTERS(
      FILTER(
        KEEPFILTERS(VALUES('Подразделения'[Анализируемый.Key0])),
        'Подразделения'[Анализируемый.Key0] = 1
      )
    )
  )
";

            var pm = new ParseManager(query);
            var res = pm.MdxGenerator.GetContextQueryStrictSet(@"Даты");

            Assert.AreEqual(@"({[Даты].[Месяц].&[2019-03-01T00:00:00],[Даты].[Месяц].&[2019-04-01T00:00:00]},{[Даты].[Это полный день].&[Да]})", res);
        }

    }

}