using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Text.Json;
using System.IO;
using System.Diagnostics;

using CsvHelper;
using System.Windows.Forms.DataVisualization.Charting;

namespace NumMetods2Lab1Frontend
{
    public partial class MainForm : Form
    {
        public Input_t input { get; set; }
        private int enableOscillation;
        public MainForm()
        {
            InitializeComponent();

            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");

            initializeInput();
        }

        private void initializeInput()
        {
            input = new Input_t();
            input.M1 = double.Parse(M1TextBox.Text);
            input.M2 = double.Parse(M2TextBox.Text);
            input.n = uint.Parse(NTextBox.Text);
            input.func_id = 0;
        }

        private void OscillationCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            enableOscillation = Convert.ToInt32(OscillationCheckBox.Checked);
            if(OscillationCheckBox.Checked)
            {
                for(int i = 0; i < FunctionComboBox.Items.Count; i++)
                {
                    string text = FunctionComboBox.Items[i].ToString();
                    text += " + cos(10 * x)";
                    FunctionComboBox.Items[i] = text;
                }
            }
            else
            {
                for (int i = 0; i < FunctionComboBox.Items.Count; i++)
                {
                    string text = FunctionComboBox.Items[i].ToString();
                    int plus_pos = -1;
                    for(int j = text.Length- 1; j >= 0;j--)
                    {
                        if (text[j] == '+')
                        {
                            plus_pos = j;
                            break;
                        }
                    }
                    text = text.Substring(0, plus_pos - 1);
                    FunctionComboBox.Items[i] = text;
                }
            }
        }

        private void FunctionComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            input.func_id = FunctionComboBox.SelectedIndex + 4 * enableOscillation + 1;
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(tabControl1.SelectedIndex == 0) 
            {
                input.func_id = 0;
            }
        }

        
        private void TryGetNumber(out double tmp, string str)
        {
            if(!double.TryParse(str, out tmp))
               MessageBox.Show("Wrong number format, try again");
        }

        private void TryGetNumber(out uint tmp, string str)
        {
            if (!uint.TryParse(str, out tmp))
                MessageBox.Show("Wrong number format, try again");
        }

        private void M1TextBox_Validated(object sender, EventArgs e)
        {
            double tmp;
            TryGetNumber(out tmp, M1TextBox.Text);
            input.M1 = tmp;
            M1TextBox.Text = tmp.ToString();
        }

        private void M2TextBox_Validated(object sender, EventArgs e)
        {
            double tmp;
            TryGetNumber(out tmp, M2TextBox.Text);
            input.M2 = tmp;
            M2TextBox.Text = tmp.ToString();
        }

        private void NTextBox_Validated(object sender, EventArgs e)
        {
            uint tmp;
            TryGetNumber(out tmp, NTextBox.Text);
            if (tmp == 0)
                tmp = 1;

            input.n = tmp;
            NTextBox.Text = tmp.ToString();
        }

        private void StartButton_Click(object sender, EventArgs e)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            string input_str = JsonSerializer.Serialize(input, options);

            File.WriteAllText("D:\\Code\\NumMetods2Lab1Backend\\Project9\\list.json",
                input_str);

            StartBackEndProcess();
        }

        private void StartBackEndProcess()
        {
            Process BackendProcess;
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "D:\\Code\\NumMetods2Lab1Backend\\x64\\Release\\Project9.exe",
                Arguments = "D:\\Code\\NumMetods2Lab1Backend\\Project9\\list.json " +
                "D:\\Code\\NumMetods2Lab1Backend\\Project9\\Table_1.csv " +
                "D:\\Code\\NumMetods2Lab1Backend\\Project9\\Table_2.csv " +
                "D:\\Code\\NumMetods2Lab1Backend\\Project9\\Directory.csv",
                CreateNoWindow = true,
                UseShellExecute = false
            };

            BackendProcess = Process.Start(processStartInfo);
            BackendProcess.EnableRaisingEvents = true;
            BackendProcess.Exited += new System.EventHandler(BackendProcess_Exited);
        }

        private double[,] GetTableData(string path, uint RowSize ,uint ColomSize)
        {
            double[,] result = new double[RowSize, ColomSize];
            double value;
            using (TextReader fileReader = File.OpenText(path))
            {
                CultureInfo culture = new CultureInfo("us-US");
                var csv = new CsvReader(fileReader, culture);
                /*csv.Configuration.HasHeaderRecord = false;*/
                int j = 0;
                while (csv.Read())
                {
                    int i = 0;
                    for (i = 0; csv.TryGetField(i, out value); i++)
                    {
                        result[j, i] = value;
                    }

                    if (i != ColomSize)
                    {
                        throw new Exception();
                    }
                    j++;

                    if (j > RowSize)
                    {
                        throw new Exception();
                    }
                }
                fileReader.Close();
            }

            return result;
        }

        delegate void del1(DataGridView Table, double[,] TableData);
        delegate void del2(double[,] TableData);
        delegate void del3(double[] v);

        private void BackendProcess_Exited(object sender, EventArgs e)
        {

            double[,] Table1_data = GetTableData("D:\\Code\\" +
                "NumMetods2Lab1Backend\\Project9\\Table_1.csv", input.n, 7);

            BeginInvoke(new del1(ShowTableData), 
                new object[] { Table1,Table1_data });

            double[,] Table2_data = GetTableData("D:\\Code\\" +
                "NumMetods2Lab1Backend\\Project9\\Table_2.csv", 3 * input.n + 1, 8);

            BeginInvoke(new del1(ShowTableData),
                new object[] { Table2, Table2_data });

            double[] reference = GetReference("D:\\Code\\" +
                "NumMetods2Lab1Backend\\Project9\\Directory.csv");

            BeginInvoke(new del3(ShowRerence), reference);

            BeginInvoke(new del2(PrintGraphics), Table1_data);
        }

        private void ShowRerence(double[] reference)
        {
            SplineSizeTextBox.Text = reference[0].ToString();
            ControlSizeTextBox.Text = reference[1].ToString();

            maxDiff1TextBox.Text = reference[2].ToString();
            X1TextBox.Text = reference[3].ToString();

            maxDiff2TextBox.Text = reference[4].ToString();
            X2TextBox.Text = reference[5].ToString();

            maxDiff3TextBox.Text = reference[6].ToString();
            X3TextBox.Text = reference[7].ToString();
        }

        private double[] GetReference(string path)
        {
            double[] result = new double[8];
            double value;
            using (TextReader fileReader = File.OpenText(path))
            {
                CultureInfo culture = new CultureInfo("us-US");
                var csv = new CsvReader(fileReader, culture);
                /*csv.Configuration.HasHeaderRecord = false;*/
                if (csv.Read())
                {
                    int i = 0;
                    for (i = 0; csv.TryGetField(i, out value); i++)
                    {
                        result[i] = value;
                    }

                    if(i < result.GetLength(0))
                    {
                        throw new Exception();
                    }
                }
                else
                    throw new Exception();

                fileReader.Close();
            }
            return result;
        }

        void ShowTableData(DataGridView Table, double[,] TableData)
        {
            Table.Rows.Clear();
            for (int i = 0; i < TableData.GetLength(0); i++)
            {
                Table.Rows.Add(new DataGridViewRow());
                for (int j = 0; j < TableData.GetLength(1); j++)
                {
                    Table[j, i].Value = TableData[i,j].ToString();
                }
            }
        }

        enum FuncType
        {
            Func, Der1, Der2
        }

        void PrintGraphics(double[,] SplinesCoeff)
        {
            FunctionChart.Series.Clear();
            Derivative1Chart.Series.Clear();
            Derivative2Chart.Series.Clear();


            PrintSpline(SplinesCoeff, FuncType.Func);
            PrintFunc(FuncType.Func);

            PrintSpline(SplinesCoeff, FuncType.Der1);
            PrintFunc(FuncType.Der1);

            PrintSpline(SplinesCoeff, FuncType.Der2);
            PrintFunc(FuncType.Der2);

            FunctionChart.ResetAutoValues();
        }

        delegate double function(double x);

        private void PrintFunc(FuncType opt)
        {
            const int PointsInFunc = 100;

            function func;
            double x0, x1;

            switch (input.func_id)
            {
                case 0:
                    x0 = -1;
                    x1 = 1;
                    func = (double x) =>
                    {
                        if(x > 0.0)
                        {
                            switch (opt)
                            {
                                case FuncType.Func:
                                    return -1 * Math.Pow(x, 3.0) + 3 * Math.Pow(x, 2.0);
                                case FuncType.Der1:
                                    return -3 * Math.Pow(x, 2.0) + 6 * x;
                                case FuncType.Der2:
                                    return -6 * x + 6;
                                default:
                                    throw new Exception();
                            }
                        }
                        else
                        {
                            switch (opt)
                            {
                                case FuncType.Func:
                                    return Math.Pow(x, 3.0) + 3 * Math.Pow(x, 2.0);
                                case FuncType.Der1:
                                    return 3 * Math.Pow(x, 2.0) + 6 * x;
                                case FuncType.Der2:
                                    return 6 * x + 6;
                                default:
                                    throw new Exception();
                            }
                        }
                    };
                    break;
                case 1:
                case 5:
                    x0 = 2;
                    x1 = 4;
                    func = (double x) =>
                    {
                        double result;
                        switch (opt)
                        {
                            case FuncType.Func:
                                result = Math.Log(x + 1) / x;
                                break;
                            case FuncType.Der1:
                                result = 1 / ((x + 1) * x) - Math.Log(x + 1) / (x * x);
                                break;
                            case FuncType.Der2:
                                result = (2 * Math.Log(x + 1)) / Math.Pow(x, 3) -
                                    (3 * x + 2) / ((x * x + 2 * x + 1) * x * x);
                                break;
                            default:
                                throw new Exception();
                        }

                        if ( input.func_id == 5)
                        {
                            result += AddOscil(x, opt);
                        }
                        return result;
                    };
                    break;
                case 2:
                case 6:
                    x0 = 1;
                    x1 = Math.PI;
                    func = (double x) =>
                    {
                        double result = 0;
                        switch (opt)
                        {
                            case FuncType.Func:
                                result = Math.Sin(x + 1) / x;
                                break;
                            case FuncType.Der1:
                                result = Math.Cos(x + 1) / x - Math.Sin(x + 1) / (x * x);
                                break;
                            case FuncType.Der2:
                                result = (Math.Sin(x + 1) * (-x * x + 2) -
                                2 * x * Math.Cos(x + 1)) / Math.Pow(x, 3);
                                break;
                            default:
                                break;
                        }

                        if (input.func_id == 6)
                        {
                            result += AddOscil(x, opt);
                        }
                        return result;
                    };
                    break;
                case 3:
                case 7:
                    x0 = 1;
                    x1 = Math.PI;
                    func = (double x) =>
                    {
                        double result = 0;
                        switch (opt)
                        {
                            case FuncType.Func:
                                result = Math.Pow(Math.Sin(x), 2) / x;
                                break;
                            case FuncType.Der1:
                                result = Math.Sin(2 * x) / x - Math.Pow(Math.Sin(x), 2) / (x * x);
                                break;
                            case FuncType.Der2:
                                result = (2 * x * x * Math.Cos(2 * x)
                                - 2 * x * Math.Sin(2 * x) + 2 * Math.Pow(Math.Sin(x), 2)) / Math.Pow(x, 3);
                                break;
                            default:
                                break;
                        }
                        if (input.func_id == 7)
                        {
                            result += AddOscil(x, opt);
                        }
                        return result;
                    };
                    break;
                case 4:
                case 8:
                    x0 = 0;
                    x1 = Math.PI;
                    func = (double x) =>
                    {
                        double result = 0;
                        switch (opt)
                        {
                            case FuncType.Func:
                                result = x * Math.Sin(x) / 3;
                                break;
                            case FuncType.Der1:
                                result = Math.Sin(x) / 3 + x * Math.Cos(x) / 3;
                                break;
                            case FuncType.Der2:
                                result = (2 * Math.Cos(x) - x * Math.Sin(x)) / 3;
                                break;
                            default:
                                break;
                        }

                        if (input.func_id == 8)
                        {
                            result += AddOscil(x, opt);
                        }
                        return result;
                    };
                    break;
                default:
                    throw new Exception();
            }

            Series FuncSeries = new Series("Function");
            FuncSeries.ChartType = SeriesChartType.Line;
            FuncSeries.BorderWidth = 2;

            double step = (x1 - x0) / PointsInFunc;

            while(x0 <= x1)
            {
                FuncSeries.Points.AddXY(x0, func(x0));
                x0 += step;
            }
            switch (opt)
            {
                case FuncType.Func:
                    FunctionChart.Series.Add(FuncSeries);
                    break;
                case FuncType.Der1:
                    Derivative1Chart.Series.Add(FuncSeries);
                    break;
                case FuncType.Der2:
                    Derivative2Chart.Series.Add(FuncSeries);
                    break;
                default:
                    break;
            }
        }

        private double AddOscil(double x, FuncType opt)
        {
            double result = 0;
            switch (opt)
            {
                case FuncType.Func:
                    result += Math.Cos(10 * x);
                    break;
                case FuncType.Der1:
                    result += -10 * Math.Sin(10 * x);
                    break;
                case FuncType.Der2:
                    result += -100 * Math.Cos(10 * x);
                    break;
                default:
                    break;
            }

            return result;
        }

        private void PrintSpline(double[,] SplinesCoeff, FuncType opt)
        {
            Series series = new Series("Spline approximation");

            series.ChartType = SeriesChartType.Line;
            series.BorderWidth = 2;

            for (int i = 0; i < SplinesCoeff.GetLength(0); i++)
            {
                Point[] Points = GetSpline(GetRow(SplinesCoeff, i), opt);

                foreach (var point in Points)
                {
                    series.Points.AddXY(point.x, point.y);
                }
            }

            switch (opt)
            {
                case FuncType.Func:
                    FunctionChart.Series.Add(series);
                    break;
                case FuncType.Der1:
                    Derivative1Chart.Series.Add(series);
                    break;
                case FuncType.Der2:
                    Derivative2Chart.Series.Add(series);
                    break;
                default:
                    break;
            }
        }

        public T[] GetRow<T>(T[,] matrix, int rowNumber)
        {
            return Enumerable.Range(0, matrix.GetLength(1))
                    .Select(x => matrix[rowNumber, x])
                    .ToArray();
        }

        delegate double SplineFunc(double x, double a, double b, double c, double d);

        private Point[] GetSpline(double[] v, FuncType opt)
        {
            const int p = 100;

            double x0 = v[1];
            double x1 = v[2];

            int PointsInSpline = Convert.ToInt32((x1 - x0) * p) + 1;
            Point[] result = new Point[PointsInSpline];

            SplineFunc Spline = (double x, double a, double b, double c, double d) =>
            {
                switch (opt)
                {
                    case FuncType.Func:
                        return a + b * (x - x1) +
                            c / 2 * Math.Pow((x - x1), 2) + d / 6 * Math.Pow((x - x1), 3);
                    case FuncType.Der1:
                        return b + c * (x - x1) + d / 2 * Math.Pow((x - x1), 2);
                    case FuncType.Der2:
                        return c + d * (x - x1);
                    default:
                        throw new Exception();
                }
                
            };

            double xCur = x0;
            double step = (x1 - x0) / PointsInSpline;
            for(int i = 0; i < PointsInSpline; i++) 
            {
                result[i].x = xCur;
                result[i].y = Spline(xCur, v[3], v[4], v[5], v[6]);
                xCur += step;
            }

            return result;
        }
    }
}

struct Point
{
    public double x, y;
}
