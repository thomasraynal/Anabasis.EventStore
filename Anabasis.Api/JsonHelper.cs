using FastMember;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Anabasis.Api
{
    public static class JsonHelper
    {
        private static bool IsDate(String str)
        {
            try
            {
                DateTime.Parse(str);
                return true;
            }
            catch
            {
                return false;
            }

        }

        public static Type GetType(object s)
        {
            if (s is JValue)
            {
                var jValue = s as JValue;
                if (jValue.Type == JTokenType.Boolean) return typeof(bool);
                if (jValue.Type == JTokenType.Date) return typeof(DateTime);
                //refacto: not optimal but prevent miscast from double to int 
                if (jValue.Type == JTokenType.Float || jValue.Type == JTokenType.Integer) return typeof(double);
                //if (jValue.Type == JTokenType.Integer) return typeof(int);
                else return typeof(string);
            }
            else
            {
                double output;
                if (double.TryParse(s.ToString(), NumberStyles.Number, CultureInfo.InvariantCulture, out output)) return typeof(double);
                if (IsDate(s.ToString())) return typeof(DateTime);
                return typeof(string);
            }
        }

        public static DataTable ToDataTable<T>(this IEnumerable<T> list)
        {
            var table = new DataTable();
            using (var reader = ObjectReader.Create(list))
            {
                table.Load(reader);
            }

            return table;
        }

        public static DataTable ToDataTable(this JArray data)
        {
            return ToDataTable(data.Cast<JObject>());

        }

        public static DataTable ToDataTable(this IEnumerable<JObject> data)
        {

            var table = new DataTable();

            var emptyColumns = new List<String>();

            foreach (var row in data)
            {

                var tableRow = table.NewRow();

                foreach (var column in row.Properties())
                {
                    if (!tableRow.Table.Columns.Contains(column.Name))
                    {
                        if (column.Value.Type == JTokenType.Null)
                        {
                            if (!emptyColumns.Contains(column.Name))
                            {
                                emptyColumns.Add(column.Name);
                            }

                            continue;
                        }

                        var isDate = IsDate(column.Value.ToString());

                        if (isDate && column.Value.Type != JTokenType.Float)
                        {
                            table.Columns.Add(column.Name, typeof(DateTime));
                        }
                        else
                        {
                            table.Columns.Add(column.Name, GetType(column.Value));
                        }

                        emptyColumns.Remove(column.Name);
                    }
                    else
                    {
                        emptyColumns.Remove(column.Name);
                    }

                    try
                    {
                        var value = column.Value.ToString();
                        if (String.IsNullOrEmpty(value)) continue;

                        if (tableRow.Table.Columns[column.Name].DataType == typeof(DateTime))
                        {
                            tableRow[column.Name] = DateTime.Parse(column.Value.ToString());
                        }
                        else
                        {
                            tableRow[column.Name] = tableRow.Table.Columns[column.Name].DataType == typeof(double) ? Double.Parse(value) : column.Value;
                        }
                    }
                    catch (Exception)
                    {
                        tableRow[column.Name] = DBNull.Value;
                    }
                }

                table.Rows.Add(tableRow);

            }

            foreach (var column in emptyColumns)
            {
                table.Columns.Add(column, typeof(String));
            }

            return table;
        }
    }
}
