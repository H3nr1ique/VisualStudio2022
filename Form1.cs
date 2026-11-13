using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace H2026Cadastro
{
    public partial class Form1 : Form
    {
        private static readonly HttpClient httpClient = new HttpClient();

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Conecta os eventos automaticamente sem precisar configurar um por um no Designer
            textBox3.TextChanged -= textBox3_Cep_TextChanged;
            textBox3.TextChanged += textBox3_Cep_TextChanged;

            button1.Click -= button1_Salvar_Click;
            button1.Click += button1_Salvar_Click;
        }

        // Dispara automaticamente assim que o usuário digita os 8 números do CEP no textBox3
        private async void textBox3_Cep_TextChanged(object sender, EventArgs e)
        {
            string cepLimpo = new string(textBox3.Text.Where(char.IsDigit).ToArray());

            if (cepLimpo.Length == 8)
            {
                await BuscarEnderecoPorCepAsync(cepLimpo);
            }
        }

        private async Task BuscarEnderecoPorCepAsync(string cep)
        {
            try
            {
                this.Cursor = Cursors.WaitCursor;

                string url = $"https://viacep.com.br/ws/{cep}/json/";
                HttpResponseMessage response = await httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    using (JsonDocument doc = JsonDocument.Parse(json))
                    {
                        JsonElement root = doc.RootElement;

                        // Verifica se o CEP existe na base dos Correios
                        if (root.TryGetProperty("erro", out _))
                        {
                            MessageBox.Show("CEP não encontrado.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }

                        string uf = root.GetProperty("uf").GetString() ?? "";
                        string cidade = root.GetProperty("localidade").GetString() ?? "";
                        string logradouro = root.GetProperty("logradouro").GetString() ?? "";
                        string bairro = root.GetProperty("bairro").GetString() ?? "";

                        // Preenche os campos na ordem: ESTADO (textBox4), CIDADE (textBox5), ENDEREÇO (textBox6)
                        textBox4.Text = uf;
                        textBox5.Text = cidade;
                        textBox6.Text = string.IsNullOrWhiteSpace(bairro)
                            ? logradouro
                            : $"{logradouro} - {bairro}";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao buscar o CEP: {ex.Message}", "Erro de Conexão", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }

        // Ação do Botão Salvar (button1)
        private void button1_Salvar_Click(object sender, EventArgs e)
        {
            string nome = textBox1.Text.Trim();
            string cpf = textBox2.Text.Trim();
            string cep = textBox3.Text.Trim();
            string estado = textBox4.Text.Trim();
            string cidade = textBox5.Text.Trim();
            string endereco = textBox6.Text.Trim();

            // Validação básica para impedir cadastros em branco
            if (string.IsNullOrWhiteSpace(nome) || string.IsNullOrWhiteSpace(cpf) || string.IsNullOrWhiteSpace(cep))
            {
                MessageBox.Show("Preencha pelo menos Nome, CPF e CEP antes de salvar.", "Campos Obrigatórios", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                string caminhoArquivo = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "clientes_cadastrados.csv");
                bool arquivoExiste = File.Exists(caminhoArquivo);

                // Usa UTF8 com BOM para que o Excel reconheça acentos e cedilha automaticamente
                using (StreamWriter sw = new StreamWriter(caminhoArquivo, true, new UTF8Encoding(true)))
                {
                    if (!arquivoExiste)
                    {
                        sw.WriteLine("DataHora;Nome;CPF;CEP;Estado;Cidade;Endereco");
                    }

                    string linha = $"{DateTime.Now:dd/MM/yyyy HH:mm:ss};{LimparPontoEVirgula(nome)};{LimparPontoEVirgula(cpf)};{LimparPontoEVirgula(cep)};{LimparPontoEVirgula(estado)};{LimparPontoEVirgula(cidade)};{LimparPontoEVirgula(endereco)}";
                    sw.WriteLine(linha);
                }

                MessageBox.Show($"Cliente salvo com sucesso!\n\nArquivo salvo em:\n{caminhoArquivo}", "Cadastro Concluído", MessageBoxButtons.OK, MessageBoxIcon.Information);

                LimparCampos();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao salvar o cadastro: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string LimparPontoEVirgula(string texto)
        {
            return texto.Replace(";", ",");
        }

        private void LimparCampos()
        {
            textBox1.Clear();
            textBox2.Clear();
            textBox3.Clear();
            textBox4.Clear();
            textBox5.Clear();
            textBox6.Clear();
            textBox1.Focus();
        }

        // Métodos mantidos para não gerar erro no Form1.Designer.cs
        private void textBox5_TextChanged(object sender, EventArgs e)
        {
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
        }
    }
}