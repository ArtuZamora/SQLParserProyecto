using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SQLParserProyecto
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        protected readonly string serverInstance = "localhost\\MSSQL";
        public MainWindow()
        {
            InitializeComponent();
            bases.Items.Add("Cargando...");
        }
        private void verifyBtn_Click(object sender, RoutedEventArgs e)
        {
            var sql = GetTextRTB(sqlScriptTxt);
            var errors = Parser.Parse(sql);
            if (errors != null)
            {
                var strBuilder = new StringBuilder();
                var count = 1;
                strBuilder.AppendLine("Errores:");
                foreach (var err in errors)
                {
                    strBuilder.AppendLine($"{count} -  {err}");
                    count++;
                }
                SetTextRTB(resultTxt, strBuilder.ToString());
            }
            else
            {
                SetTextRTB(resultTxt, "Script SQL correcta.");
            }
        }
        private void executeBtn_Click(object sender, RoutedEventArgs e)
        {

        }
        private string GetTextRTB(RichTextBox rtb)
        {
            TextRange textRange = new TextRange(
                rtb.Document.ContentStart,
                rtb.Document.ContentEnd
            );
            return textRange.Text;
        }
        private void SetTextRTB(RichTextBox rtb, string text)
        {
            rtb.Document.Blocks.Clear();
            rtb.Document.Blocks.Add(new Paragraph(new Run(text)));
        }
        private void sqlScriptTxt_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Tab ||
                (Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control)
                return;

            var richTextBox = sender as RichTextBox;
            if (richTextBox == null) return;

            if (richTextBox.Selection.Text != string.Empty)
                richTextBox.Selection.Text = string.Empty;

            var caretPosition = richTextBox.CaretPosition.GetPositionAtOffset(0,
                                  LogicalDirection.Forward);

            richTextBox.CaretPosition.InsertTextInRun("\t");
            richTextBox.CaretPosition = caretPosition;
            e.Handled = true;
        }
        private async Task GetDatabases()
        {
            var bdds = await GetDatabasesAsync(serverInstance);
            if (bdds != null)
                foreach (var bdd in bdds)
                {
                    bases.Items.Add(bdd);
                }
        }
        private async Task<List<string>?> GetDatabasesAsync(string instance)
        {
            // Las bases de datos propias de SQL Server
            string[] basesSys = { "master", "model", "msdb", "tempdb" };
            string[] bases;
            DataTable dt = new DataTable();
            // Usamos la seguridad integrada de Windows
            string sCnn = "Server=" + instance + "; database=master; integrated security=yes";

            // La orden T-SQL para recuperar las bases de master
            string sel = "SELECT name FROM sysdatabases";
            try
            {
                if (await IsServerConnectedAsync(sCnn))
                {
                    SqlDataAdapter da = new SqlDataAdapter(sel, sCnn);
                    da.Fill(dt);
                    bases = new string[dt.Rows.Count - 1];
                    int k = -1;
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        string s = dt.Rows[i]["name"].ToString();
                        // Solo asignar las bases que no son del sistema
                        if (Array.IndexOf(basesSys, s) == -1)
                        {
                            k += 1;
                            bases[k] = s;
                        }
                    }
                    if (k == -1) return null;
                    // ReDim Preserve
                    {
                        int i1_RPbases = bases.Length;
                        string[] copiaDe_bases = new string[i1_RPbases];
                        Array.Copy(bases, copiaDe_bases, i1_RPbases);
                        bases = new string[(k + 1)];
                        Array.Copy(copiaDe_bases, bases, (k + 1));
                    };
                    this.bases.Items.Clear();
                    return bases.ToList();
                }
                else
                {
                    this.bases.Items.Clear();
                    this.bases.Items.Add("Error al recuperar las bases de la instancia indicada");
                }

            }
            catch (Exception ex)
            {
                this.bases.Items.Clear();
                this.bases.Items.Add("Error al recuperar las bases de la instancia indicada");
            }
            return null;
        }
        private async static Task<bool> IsServerConnectedAsync(string connectionString)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    await connection.OpenAsync();
                    return true;
                }
                catch (SqlException)
                {
                    return false;
                }
            }
        }
        private async void Window_ContentRendered_1(object sender, EventArgs e)
        {
            await GetDatabases();
        }
    }
}
