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
        protected readonly string serverInstance;
        public MainWindow()
        {
            InitializeComponent();
            using var context = new Context();
            serverInstance = context.GetInstance();
            bases.Items.Add("Cargando...");
            tables.Items.Add("Seleccione una base y refresque");
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
        private async void executeBtn_Click(object sender, RoutedEventArgs e)
        {
            
            var sqlP = GetTextRTB(sqlScriptTxt);
            var errors = Parser.Parse(sqlP);
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
                var selectedbase = bases.SelectedValue == null ? "master" : bases.SelectedValue.ToString() == "Cargando..." || bases.SelectedValue.ToString() == "Error al recuperar las bases de la instancia indicada" ? "master" : bases.SelectedValue.ToString();
                using var context = new Context(database: selectedbase);
                using (var conn = context.GetConnection())
                {
                    await conn.OpenAsync();
                    var sql = GetTextRTB(sqlScriptTxt);
                    using (var command = new SqlCommand(sql, conn))
                    {
                        try
                        {
                            using var reader = await command.ExecuteReaderAsync();
                            DataTable table = new DataTable();
                            if (reader.HasRows)
                            {
                                int fields = reader.FieldCount;
                                for (int i = 0; i < fields; i++)
                                {
                                    table.Columns.Add(reader.GetName(i));
                                }
                                while (reader.Read())
                                {
                                    object[] row = new object[fields];
                                    for (int i = 0; i < fields; i++)
                                    {
                                        if (reader.IsDBNull(i))
                                            row[i] = "";
                                        else
                                            row[i] = reader[i];
                                    }
                                    table.Rows.Add(row);
                                }
                                Data data = new Data(table);
                                data.Show();
                            }
                            SetTextRTB(resultTxt, "Sentencia ejecutada correctamente.");
                        }
                        catch (SqlException err)
                        {
                            SetTextRTB(resultTxt, err.Message);
                        }
                    }
                }
            }
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
            {
                foreach (var bdd in bdds)
                {
                    bases.Items.Add(bdd);
                }
                bases.Items.Add("master");
            }
        }
        private async Task<List<string>?> GetDatabasesAsync(string instance)
        {
            // Las bases de datos propias de SQL Server
            string[] basesSys = { "master", "model", "msdb", "tempdb" };
            string[] bases;
            DataTable dt = new DataTable();
            // Usamos la seguridad integrada de Windows
            using var context = new Context();
            var sCnn = context.GetConnectionString();

            // La orden T-SQL para recuperar las bases de master
            string sel = "SELECT name FROM sysdatabases";
            try
            {
                if (await IsServerConnectedAsync())
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
        private async static Task<bool> IsServerConnectedAsync()
        {
            using var context = new Context();
            using (var connection = context.GetConnection())
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
        private void sqlScriptTxt_TextChanged(object sender, TextChangedEventArgs e)
        {
        }
        private async void refresh_MouseUp(object sender, MouseButtonEventArgs e)
        {
            bases.Items.Clear();
            bases.Items.Add("Cargando...");
            await GetDatabases();
        }
        private void bases_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
        }
        private async void refreshTables_MouseUp(object sender, MouseButtonEventArgs e)
        {
            var selectedbase = bases.SelectedValue == null ? "master" : bases.SelectedValue.ToString() == "Cargando..." || bases.SelectedValue.ToString() == "Error al recuperar las bases de la instancia indicada" ? "master" : bases.SelectedValue.ToString();
            using var context = new Context(database: selectedbase);
            using var conn = context.GetConnection();
            await conn.OpenAsync();
            DataTable t = await conn.GetSchemaAsync("Tables");
            var rows = t.Rows;
            tables.Items.Clear();
            if (rows.Count == 0)
                tables.Items.Add("No existen tablas en esta base de datos");
            foreach (DataRow row in rows)
            {
                tables.Items.Add(row[2].ToString());
            }
        }
    }
}
