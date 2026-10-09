using System.Globalization;
using ControleFinanceiro.Models;
using ControleFinanceiro.Services;

namespace ControleFinanceiro.Forms;

public class LancamentoForm : Form
{
    private readonly FinanceiroService _svc;
    private readonly Lancamento _lanc;

    private readonly RadioButton rbDespesa = new() { Text = "Despesa", Checked = true, AutoSize = true };
    private readonly RadioButton rbReceita = new() { Text = "Receita", AutoSize = true };
    private readonly TextBox txtDescricao = new() { Dock = DockStyle.Fill };
    private readonly TextBox txtValor = new() { Dock = DockStyle.Fill };
    private readonly DateTimePicker dtData = new() { Format = DateTimePickerFormat.Short, Dock = DockStyle.Fill };
    private readonly CheckBox chkPago = new() { Text = "Já foi pago / recebido", Checked = true, AutoSize = true };
    private readonly DateTimePicker dtVenc = new() { Format = DateTimePickerFormat.Short, ShowCheckBox = true, Checked = false, Dock = DockStyle.Fill };
    private readonly ComboBox cmbCategoria = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill, DisplayMember = "Nome", ValueMember = "Id" };
    private readonly ComboBox cmbConta = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill, DisplayMember = "Nome", ValueMember = "Id" };
    private readonly ComboBox cmbRepeticao = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
    private readonly NumericUpDown numQtd = new() { Minimum = 2, Maximum = 120, Value = 12, Dock = DockStyle.Fill, Enabled = false };

    public LancamentoForm(FinanceiroService svc, Lancamento? existente)
    {
        _svc = svc;
        _lanc = existente ?? new Lancamento { Data = DateTime.Today };

        Text = existente == null ? "Novo lançamento" : "Editar lançamento";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(430, 460);
        Font = new Font("Segoe UI", 9.5f);

        var tabela = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            Padding = new Padding(12)
        };
        tabela.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        tabela.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var tipoPanel = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill };
        tipoPanel.Controls.AddRange(new Control[] { rbDespesa, rbReceita });

        Linha(tabela, "Tipo", tipoPanel);
        Linha(tabela, "Descrição", txtDescricao);
        Linha(tabela, "Valor (R$)", txtValor);
        Linha(tabela, "Data", dtData);
        Linha(tabela, "Categoria", cmbCategoria);
        Linha(tabela, "Conta", cmbConta);
        Linha(tabela, "Situação", chkPago);
        Linha(tabela, "Vencimento", dtVenc);

        // Repetição só aparece ao criar um lançamento novo
        if (existente == null)
        {
            cmbRepeticao.Items.AddRange(new object[]
            {
                "Única vez",
                "Parcelado (valor = cada parcela)",
                "Recorrente (todo mês)"
            });
            cmbRepeticao.SelectedIndex = 0;
            cmbRepeticao.SelectedIndexChanged += (_, _) => numQtd.Enabled = cmbRepeticao.SelectedIndex > 0;

            Linha(tabela, "Repetição", cmbRepeticao);
            Linha(tabela, "Nº de meses", numQtd);
        }

        var btnSalvar = new Button { Text = "Salvar", Width = 90, Height = 32 };
        var btnCancelar = new Button { Text = "Cancelar", Width = 90, Height = 32, DialogResult = DialogResult.Cancel };
        btnSalvar.Click += (_, _) => Salvar();

        var botoes = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 52,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(10)
        };
        botoes.Controls.Add(btnSalvar);
        botoes.Controls.Add(btnCancelar);

        Controls.Add(tabela);
        Controls.Add(botoes);
        AcceptButton = btnSalvar;
        CancelButton = btnCancelar;

        cmbConta.DataSource = _svc.ListarContas();

        if (existente != null)
        {
            rbReceita.Checked = existente.Tipo == TipoLancamento.Receita;
            rbDespesa.Checked = !rbReceita.Checked;
            CarregarCategorias();

            txtDescricao.Text = existente.Descricao;
            txtValor.Text = existente.Valor.ToString("N2");
            dtData.Value = existente.Data;
            chkPago.Checked = existente.Pago;
            if (existente.Vencimento.HasValue)
            {
                dtVenc.Value = existente.Vencimento.Value;
                dtVenc.Checked = true;
            }
            cmbCategoria.SelectedValue = existente.CategoriaId;
            cmbConta.SelectedValue = existente.ContaId;
        }
        else
        {
            CarregarCategorias();
        }

        rbReceita.CheckedChanged += (_, _) => CarregarCategorias();
    }

    private static void Linha(TableLayoutPanel t, string rotulo, Control controle)
    {
        int linha = t.RowStyles.Count;
        t.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        t.Controls.Add(new Label
        {
            Text = rotulo,
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 8, 0, 8)
        }, 0, linha);
        controle.Margin = new Padding(3, 5, 3, 5);
        t.Controls.Add(controle, 1, linha);
    }

    private void CarregarCategorias()
    {
        var tipo = rbReceita.Checked ? TipoLancamento.Receita : TipoLancamento.Despesa;
        cmbCategoria.DataSource = _svc.ListarCategorias(tipo);
    }

    private void Salvar()
    {
        var textoValor = txtValor.Text.Replace("R$", "").Trim();
        if (!decimal.TryParse(textoValor, NumberStyles.Number, CultureInfo.CurrentCulture, out var valor))
        {
            MessageBox.Show("Valor inválido. Exemplo: 1234,56", "Atenção",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (cmbCategoria.SelectedValue is not int categoriaId)
        {
            MessageBox.Show("Escolha uma categoria.", "Atenção",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (cmbConta.SelectedValue is not int contaId)
        {
            MessageBox.Show("Escolha uma conta.", "Atenção",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _lanc.Descricao = txtDescricao.Text.Trim();
        _lanc.Valor = Math.Round(valor, 2);
        _lanc.Data = dtData.Value.Date;
        _lanc.Pago = chkPago.Checked;
        _lanc.Vencimento = dtVenc.Checked ? dtVenc.Value.Date : null;
        _lanc.Tipo = rbReceita.Checked ? TipoLancamento.Receita : TipoLancamento.Despesa;
        _lanc.CategoriaId = categoriaId;
        _lanc.ContaId = contaId;

        try
        {
            var modo = _lanc.Id == 0 && cmbRepeticao.SelectedIndex > 0
                ? (ModoRepeticao)cmbRepeticao.SelectedIndex
                : ModoRepeticao.Unica;

            if (modo == ModoRepeticao.Unica)
                _svc.Salvar(_lanc);
            else
                _svc.SalvarRepetido(_lanc, modo, (int)numQtd.Value);

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Não foi possível salvar",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
