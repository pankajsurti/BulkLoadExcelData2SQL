using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExcelDataReader;

namespace ImportServiceNowDataConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            DataTableBulkCopySample();
        }
        private static void DataTableBulkCopySample()
        {
            var dt = new DataTable();
            using (var stream = File.Open("SampleInput.csv", FileMode.Open, FileAccess.Read))
            {
                using (var reader = ExcelReaderFactory.CreateCsvReader(stream))
                {
                    var result = reader.AsDataSet(new ExcelDataSetConfiguration()
                    {
                        // Gets or sets a value indicating whether to set the DataColumn.DataType 
                        // property in a second pass.
                        UseColumnDataType = true,
                        // Gets or sets a callback to determine whether to include the current sheet
                        // in the DataSet. Called once per sheet before ConfigureDataTable.
                        FilterSheet = (tableReader, sheetIndex) => true,

                        // Gets or sets a callback to obtain configuration options for a DataTable. 
                        ConfigureDataTable = (tableReader) => new ExcelDataTableConfiguration()
                        {
                            // Gets or sets a value indicating the prefix of generated column names.
                            EmptyColumnNamePrefix = "Column",

                            // Gets or sets a value indicating whether to use a row from the 
                            // data as column names.
                            UseHeaderRow = false,

                            // Gets or sets a callback to determine which row is the header row. 
                            // Only called when UseHeaderRow = true.
                            ReadHeaderRow = (rowReader) => {
                                // F.ex skip the first row and use the 2nd row as column headers:
                                rowReader.Read();
                            },

                            // Gets or sets a callback to determine whether to include the 
                            // current row in the DataTable.
                            FilterRow = (rowReader) => {
                                return true;
                            },

                            // Gets or sets a callback to determine whether to include the specific
                            // column in the DataTable. Called once per column after reading the 
                            // headers.
                            FilterColumn = (rowReader, columnIndex) => {
                                return true;
                            }
                        }
                    });
                    Console.WriteLine(result.Tables[0].Rows.Count);
                    Console.WriteLine(result.Tables[0].Columns.Count);

                    foreach (DataColumn col in result.Tables[0].Columns)
                    {
                        String strColumnName = (String)result.Tables[0].Rows[0][col.Ordinal];
                        strColumnName = strColumnName.Replace('.', '_');
                        Console.WriteLine("SOURCE: {0} \t\t DEST: {1}", col.ColumnName, strColumnName);

                    }
                    String strConnection = @"TODO";
                    using (SqlConnection con = new SqlConnection(strConnection))
                    {
                        SqlBulkCopy bulk = new SqlBulkCopy(con);
                        bulk.DestinationTableName = "KnowledgeBaseData";

                        // Set up the event handler to notify after 50 rows.
                        bulk.SqlRowsCopied +=
                            new SqlRowsCopiedEventHandler(OnSqlRowsCopied);
                        bulk.NotifyAfter = 50;
                        bulk.BatchSize = 100;
                        bulk.BulkCopyTimeout = 6000;
                        foreach (DataColumn col in result.Tables[0].Columns)
                        {
                            String strColumnName = (String)result.Tables[0].Rows[0][col.Ordinal];
                            strColumnName = strColumnName.Replace('.', '_');
                            bulk.ColumnMappings.Add(col.ColumnName, strColumnName);
                        }
                        con.Open();
                        bulk.WriteToServer(result.Tables[0]);

                        con.Close();
                    }
                }
            }


        }
        private static void OnSqlRowsCopied( object sender, SqlRowsCopiedEventArgs e)
        {
            Console.WriteLine("Copied {0} so far...", e.RowsCopied);
        }

    }
}
