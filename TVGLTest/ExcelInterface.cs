using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Office.Core;
using Excel = Microsoft.Office.Interop.Excel;

namespace TVGLTest
{
    public static class ExcelInterface
    {
        /// <summary>
        /// Creates a new graph in a new excel workbook. Values is a list of data series, where a series is a list of X,Y value pairs.
        /// </summary>
        /// <param name="values"></param>
        /// <param name="graphTitle"></param>
        /// <param name="xAxis"></param>
        /// <param name="yAxis"></param>
        /// <param name="headers"></param>
        public static void CreateNewGraph(IList<List<double[]>> values, string graphTitle = "", 
             string xAxis = "", string yAxis = "", IList<string> headers = null)
        {
            //Create a new excel workbook and sheet
            Excel.Application xlApp;
            Excel.Workbook xlWorkbook;
            Excel.Worksheet xlWorksheet;
            object misValue = System.Reflection.Missing.Value;

            xlApp = new Excel.Application(); 
            xlWorkbook = xlApp.Workbooks.Add(misValue);
            xlWorksheet = xlWorkbook.Worksheets.get_Item(1);

            //Export data to Excel Sheet from List<List<double>>
            //Set headers if given.
            if (headers != null)
            {
                for (var i = 0; i < headers.Count; i++)
                {
                    xlWorksheet.Cells[2, i+1].Value = headers[i];
                }
            }

            //Set data
            //Find number of columns required
            var row = 2;
            for (var i = 0; i < values.Count; i++) //Column
            {
                for (var j = 0; j < values[i].Count; j++) //Row
                {
                    for (var k = 0; k < values[i][j].Count(); k++) //Column offset for items in double[]
                    {
                      //  var row = i +j+2; //+1 for excel, +1 for headers
                        var column =  k + 1;
                        xlWorksheet.Cells[row, column] = values[i][j][k];
                    }  
                row++;
                }
            }
            
            //Create Chart
            Excel.Range chartRange;
            Excel.Range range;
            Excel.ChartObjects xlCharts = (Excel.ChartObjects)xlWorksheet.ChartObjects(Type.Missing);
            Excel.ChartObject myChart = (Excel.ChartObject)xlCharts.Add(200, 30, 400, 300);
            Excel.Chart chart = myChart.Chart;
            chart.ChartType = Excel.XlChartType.xlXYScatterLines;
            Excel.SeriesCollection seriesCollection = (Excel.SeriesCollection)chart.SeriesCollection();
            
            for (var i = 0; i < values.Count; i++) //For each series
            {
                var xValues = new double[values[i].Count];
                var yValues = new double[values[i].Count];
                for (var j = 0; j < values[i].Count; j++) //For each point in series
                {
                    xValues[j] = values[i][j][0];
                    yValues[j] = values[i][j][1];
                }
                Excel.Series series = seriesCollection.NewSeries();
                series.Values = yValues;
                series.XValues = xValues;
            }
            
            chart.ChartWizard(
                Title: graphTitle,
                CategoryTitle: xAxis,
                ValueTitle: yAxis);
            xlApp.Visible = true;

            System.Runtime.InteropServices.Marshal.ReleaseComObject(xlWorksheet);
            System.Runtime.InteropServices.Marshal.ReleaseComObject(xlWorkbook);
            System.Runtime.InteropServices.Marshal.ReleaseComObject(xlApp);
        }
        /// <summary>
        /// Creates a new graph in a new excel workbook. Values is a list of data series, where a series is a list of X,Y value pairs.
        /// </summary>
        /// <param name="graphTitle"></param>
        /// <param name="xAxis"></param>
        /// <param name="yAxis"></param>
        /// <param name="headers"></param>
        public static void PlotDataSets(List<List<double[]>> dataSet1, List<List<double[]>> dataSet2, string seriesTitle = "",
             string xAxis = "", string yAxis = "", List<string> headers = null)
        {
            //Create a new excel workbook and sheet
            Excel.Application xlApp;
            Excel.Workbook xlWorkbook;
            Excel.Worksheet xlWorksheet;
            object misValue = System.Reflection.Missing.Value;

            xlApp = new Excel.Application();
            xlWorkbook = xlApp.Workbooks.Add(misValue);
            xlWorksheet = xlWorkbook.Worksheets.get_Item(1);

            //Create Chart
            Excel.Range chartRange;
            Excel.Range range;
            Excel.ChartObjects xlCharts = (Excel.ChartObjects)xlWorksheet.ChartObjects(Type.Missing);
            var offset = 0;

            for (var i = 0; i < dataSet1.Count; i++) //For each series in dataSet1
            {
                //Create a new chart and offset from previous                
                offset = 300 * (i);
                Excel.ChartObject myChart = (Excel.ChartObject)xlCharts.Add(200, 30 + offset, 400, 300);
                Excel.Chart chart = myChart.Chart;
                chart.ChartType = Excel.XlChartType.xlXYScatterLines;
                Excel.SeriesCollection seriesCollection = (Excel.SeriesCollection)chart.SeriesCollection();

                //Add first series
                var xValues = new double[dataSet1[i].Count];
                var yValues= new double[dataSet1[i].Count];
                for (var j = 0; j < dataSet1[i].Count; j++) //For each point in series
                {
                    xValues[j] = dataSet1[i][j][0];
                    yValues[j] = dataSet1[i][j][1];
                }
                Excel.Series series1 = seriesCollection.NewSeries();
                series1.Values = yValues;
                series1.XValues = xValues;

                //Add second series
                xValues = new double[dataSet2[i].Count];
                yValues = new double[dataSet2[i].Count];
                for (var j = 0; j < dataSet2[i].Count; j++) //For each point in series
                {
                    xValues[j] = dataSet2[i][j][0];
                    yValues[j] = dataSet2[i][j][1];
                }
                Excel.Series series2 = seriesCollection.NewSeries();
                series2.Values = yValues;
                series2.XValues = xValues;

                var title = seriesTitle + " " + i;
                chart.ChartWizard(
                    Title: title,
                    CategoryTitle: xAxis,
                    ValueTitle: yAxis);

            }

            xlApp.Visible = true;
            System.Runtime.InteropServices.Marshal.ReleaseComObject(xlWorksheet);
            System.Runtime.InteropServices.Marshal.ReleaseComObject(xlWorkbook);
            System.Runtime.InteropServices.Marshal.ReleaseComObject(xlApp);
        }

        /// <summary>
        /// Creates a new graph in a new excel workbook. Values is a list of data series, where a series is a list of X,Y value pairs.
        /// </summary>
        /// <param name="values"></param>
        /// <param name="graphTitle"></param>
        /// <param name="xAxis"></param>
        /// <param name="yAxis"></param>
        /// <param name="headers"></param>
        public static void PlotEachSeriesSeperately(List<List<double[]>> values, string seriesTitle = "",
             string xAxis = "", string yAxis = "", List<string> headers = null)
        {
            //Create a new excel workbook and sheet
            Excel.Application xlApp;
            Excel.Workbook xlWorkbook;
            Excel.Worksheet xlWorksheet;
            object misValue = System.Reflection.Missing.Value;

            xlApp = new Excel.Application();
            xlWorkbook = xlApp.Workbooks.Add(misValue);
            xlWorksheet = xlWorkbook.Worksheets.get_Item(1);

            //Export data to Excel Sheet from List<List<double>>
            //Set headers if given.
            if (headers != null)
            {
                for (var i = 0; i < headers.Count; i++)
                {
                    xlWorksheet.Cells[1, i].Value = headers[i];
                }
            }
            
            //Create Chart
            Excel.Range chartRange;
            Excel.Range range;
            Excel.ChartObjects xlCharts = (Excel.ChartObjects)xlWorksheet.ChartObjects(Type.Missing);
            var offset = 0;

            for (var i = 0; i < values.Count; i++) //For each series
            {
                var xValues = new double[values[i].Count];
                var yValues = new double[values[i].Count];
                for (var j = 0; j < values[i].Count; j++) //For each point in series
                {
                    xValues[j] = values[i][j][0];
                    yValues[j] = values[i][j][1];
                }

                //Create a new chart and offset from previous                
                offset =  300 * (i);
                Excel.ChartObject myChart = (Excel.ChartObject)xlCharts.Add(200, 30 + offset, 400, 300 );
                Excel.Chart chart = myChart.Chart;
                chart.ChartType = Excel.XlChartType.xlXYScatterLines;
                Excel.SeriesCollection seriesCollection = (Excel.SeriesCollection)chart.SeriesCollection();
                Excel.Series series = seriesCollection.NewSeries();
                series.Values = yValues;
                series.XValues = xValues;
                
                var title = seriesTitle + " " + i;
                chart.ChartWizard(
                    Title: title,
                    CategoryTitle: xAxis,
                    ValueTitle: yAxis);
                
            }

            xlApp.Visible = true;
            System.Runtime.InteropServices.Marshal.ReleaseComObject(xlWorksheet);
            System.Runtime.InteropServices.Marshal.ReleaseComObject(xlWorkbook);
            System.Runtime.InteropServices.Marshal.ReleaseComObject(xlApp);
        }
    }
}
