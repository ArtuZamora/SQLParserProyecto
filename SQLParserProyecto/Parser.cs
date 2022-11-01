using Microsoft.SqlServer.TransactSql.ScriptDom;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLParserProyecto
{
    public static class Parser
    {
        public static List<string>? Parse(string sql)
        {
            TSql100Parser parser = new TSql100Parser(false);
            IList<ParseError> errors;
            var fragment = parser.Parse(new StringReader(sql), out errors);
            if (errors != null && errors.Count > 0)
            {
                List<string> errorList = new List<string>();
                foreach (var error in errors)
                {
                    errorList.Add(error.Message);
                }
                return errorList;
            }
            return null;
        }
    }
}
