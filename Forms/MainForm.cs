using ControleFinanceiro.Data;
using ControleFinanceiro.Models;
using ControleFinanceiro.Services;

namespace ControleFinanceiro.Forms;

public class MainForm : Form
{
    private record Opcao(int? Id, string Nome)
    {
        public override string ToString() => Nome;
    }

    private readonly FinanceiroService _svc = new();
    private bool _carregando;

    private readonly DateTimePicker dtMes = new()
    {
        Format = DateTimePickerFormat.Custom,
        CustomFormat = "MMMM 'de' yyyy",
        ShowUpDown = true,
        Width = 170
    };
    private readonly ComboBox cmbConta = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 160 };
    private readonly ComboBox cmbCategoria = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 160 };
    private readonly ComboBox cmbSituacao = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 110 };
    private readonly TextBox txtBusca = new() { Width = 170, PlaceholderText = "Descrição..." };

    private readonly DataGridView grid = NovaGrid();
    private readonly DataGridView gridCat = NovaGrid();

    private readonly Label lblSaldo = NovoCard();
    private readonly Label lblReceitas = NovoCard();
    private readonly Label lblDespesas = NovoCard();
    private readonly Label lblPendente = NovoCard();

    public MainForm()
    {
        Text = "Controle Financeiro Pessoal";
        Width = 1150;
        Height = 680;
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9.5f);

        // ----- Resumo (cards) -----
        var pnlResumo = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 80,
            ColumnCount = 4,
            Padding = new Padding(8)
        };
        for (int i = 0; i < 4; i++)
            pnlResumo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        pnlResumo.Controls.Add(lblSaldo, 0, 0);
        pnlResumo.Controls.Add(lblReceitas, 1, 0);
        pnlResumo.Controls.Add(lblDespesas, 2, 0);
        pnlResumo.Controls.Add(lblPendente, 3, 0);

        lblSaldo.BackColor = Color.AliceBlue;
        lblReceitas.BackColor = Color.Honeydew;
        lblDespesas.BackColor = Color.MistyRose;
        lblPendente.BackColor = Color.LemonChiffon;

        cmbSituacao.Items.AddRange(new object[] { "Todas", "Pendentes", "Pagos", "Vencidos" });
        cmbSituacao.SelectedIndex = 0;

        // ----- Filtros -----
        var pnlFiltros = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 46,
            Padding = new Padding(8, 8, 0, 0)
        };
        pnlFiltros.Controls.AddRange(new Control[]
        {
            Rotulo("Mês:"), dtMes,
            Rotulo("Conta:"), cmbConta,
            Rotulo("Categoria:"), cmbCategoria,
            Rotulo("Situação:"), cmbSituacao,
            Rotulo("Buscar:"), txtBusca
        });

        // ----- Botões -----
        var pnlBotoes = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 54,
            Padding = new Padding(8)
        };
        pnlBotoes.Controls.AddRange(new Control[]
        {
            Botao("Novo", Novo),
            Botao("Editar", Editar),
            Botao("Duplicar", Duplicar),
            Botao("Excluir", Excluir),
            Botao("Pago / Pendente", AlternarPago),
            Botao("Contas", AbrirContas),
            Botao("Categorias", AbrirCategorias),
            Botao("Gráficos", AbrirGraficos),
            Botao("Exportar Excel", ExportarExcel),
            Botao("Backup", FazerBackup),
            Botao("Restaurar", RestaurarBackup)
        });

        gridCat.Dock = DockStyle.Right;
        gridCat.Width = 300;
        grid.Dock = DockStyle.Fill;

        // Ordem importa para o Dock: o último adicionado é posicionado primeiro
        Controls.AddRange(new Control[] { grid, gridCat, pnlBotoes, pnlFiltros, pnlResumo });

        // ----- Eventos -----
        dtMes.ValueChanged += (_, _) => { if (!_carregando) Carregar(); };
        cmbConta.SelectedIndexChanged += (_, _) => { if (!_carregando) Carregar(); };
        cmbCategoria.SelectedIndexChanged += (_, _) => { if (!_carregando) Carregar(); };
        cmbSituacao.SelectedIndexChanged += (_, _) => { if (!_carregando) Carregar(); };
        txtBusca.TextChanged += (_, _) => { if (!_carregando) Carregar(); };

        // Atalhos: Ctrl+N novo, Ctrl+D duplicar, Ctrl+F buscar, F5 atualizar, Delete exclui (com a tabela em foco)
        KeyPreview = true;
        KeyDown += (_, e) =>
        {
            if (e.Control && e.KeyCode == Keys.N) { Novo(); e.SuppressKeyPress = true; }
            else if (e.Control && e.KeyCode == Keys.D) { Duplicar(); e.SuppressKeyPress = true; }
            else if (e.Control && e.KeyCode == Keys.F) { txtBusca.Focus(); e.SuppressKeyPress = true; }
            else if (e.KeyCode == Keys.F5) { CarregarFiltros(); Carregar(); }
            else if (e.KeyCode == Keys.Delete && ActiveControl == grid) { Excluir(); e.SuppressKeyPress = true; }
        };

        Shown += (_, _) => AvisarVencimentos();
        grid.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) Editar(); };
        grid.CellFormatting += FormatarCelula;

        Load += (_, _) =>
        {
            dtMes.Value = DateTime.Today;
            CarregarFiltros();
            Carregar();
        };
    }

    // ---------- Helpers de UI ----------
    private static DataGridView NovaGrid() => new()
    {
        ReadOnly = true,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        AllowUserToResizeRows = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        MultiSelect = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        RowHeadersVisible = false,
        BackgroundColor = Color.White
    };

    private static Label NovoCard() => new()
    {
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleCenter,
        Font = new Font("Segoe UI", 11f, FontStyle.Bold),
        Margin = new Padding(4)
    };

    private static Label Rotulo(string texto) => new()
    {
        Text = texto,
        AutoSize = true,
        Margin = new Padding(8, 6, 2, 0)
    };

    private static Button Botao(string texto, Action acao)
    {
        var b = new Button { Text = texto, AutoSize = true, Padding = new Padding(8, 2, 8, 2) };
        b.Click += (_, _) => acao();
        return b;
    }

    // ---------- Carregamento ----------
    private void CarregarFiltros()
    {
        _carregando = true;

        var contaSel = (cmbConta.SelectedItem as Opcao)?.Id;
        var catSel = (cmbCategoria.SelectedItem as Opcao)?.Id;

        cmbConta.Items.Clear();
        cmbConta.Items.Add(new Opcao(null, "Todas"));
        foreach (var c in _svc.ListarContas())
            cmbConta.Items.Add(new Opcao(c.Id, c.Nome));
        Selecionar(cmbConta, contaSel);

        cmbCategoria.Items.Clear();
        cmbCategoria.Items.Add(new Opcao(null, "Todas"));
        foreach (var c in _svc.ListarCategorias())
            cmbCategoria.Items.Add(new Opcao(c.Id, c.Nome));
        Selecionar(cmbCategoria, catSel);

        _carregando = false;
    }

    private static void Selecionar(ComboBox cmb, int? id)
    {
        cmb.SelectedIndex = 0;
        for (int i = 0; i < cmb.Items.Count; i++)
        {
            if (cmb.Items[i] is Opcao o && o.Id == id)
            {
                cmb.SelectedIndex = i;
                break;
            }
        }
    }

    private void Carregar()
    {
        int ano = dtMes.Value.Year;
        int mes = dtMes.Value.Month;
        var contaId = (cmbConta.SelectedItem as Opcao)?.Id;
        var catId = (cmbCategoria.SelectedItem as Opcao)?.Id;

        var busca = txtBusca.Text.Trim();
        grid.DataSource = _svc.Listar(ano, mes, contaId, catId,
            busca.Length > 0 ? busca : null, cmbSituacao.SelectedItem as string);
        ConfigurarColunas();

        var r = _svc.Resumo(ano, mes);
        lblSaldo.Text = $"Saldo atual\n{_svc.SaldoTotal():C2}";
        lblReceitas.Text = $"Receitas recebidas\n{r.Receitas:C2}";
        lblDespesas.Text = $"Despesas pagas\n{r.Despesas:C2}";
        lblPendente.Text = $"A pagar (pendentes)\n{r.Pendente:C2}";

        gridCat.DataSource = _svc.DespesasPorCategoria(ano, mes);
        foreach (DataGridViewColumn c in gridCat.Columns)
        {
            if (c.Name == "Total")
            {
                c.DefaultCellStyle.Format = "C2";
                c.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
            else if (c.Name == "Categoria")
            {
                c.HeaderText = "Categoria";
            }
        }
    }

    private void ConfigurarColunas()
    {
        foreach (DataGridViewColumn c in grid.Columns)
        {
            switch (c.Name)
            {
                case "Id":
                    c.Visible = false;
                    break;
                case "Data":
                case "Vencimento":
                    c.DefaultCellStyle.Format = "dd/MM/yyyy";
                    break;
                case "Valor":
                    c.DefaultCellStyle.Format = "C2";
                    c.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                    break;
                case "Descricao":
                    c.HeaderText = "Descrição";
                    c.FillWeight = 200;
                    break;
                case "Situacao":
                    c.HeaderText = "Situação";
                    break;
            }
        }
    }

    private void FormatarCelula(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || grid.Rows[e.RowIndex].DataBoundItem is not LancamentoItem item)
            return;

        if (grid.Columns[e.ColumnIndex].Name == "Valor")
            e.CellStyle!.ForeColor = item.Tipo == "Receita" ? Color.DarkGreen : Color.Firebrick;

        // Pendente e vencido: destaca a linha
        if (item.Situacao == "Pendente" && (item.Vencimento ?? item.Data).Date < DateTime.Today)
            e.CellStyle!.BackColor = Color.MistyRose;
    }

    // ---------- Ações ----------
    private LancamentoItem? Selecionado() => grid.CurrentRow?.DataBoundItem as LancamentoItem;

    private void Novo()
    {
        using var f = new LancamentoForm(_svc, null);
        if (f.ShowDialog(this) == DialogResult.OK) Carregar();
    }

    private void Editar()
    {
        var item = Selecionado();
        if (item == null) return;
        var lanc = _svc.Obter(item.Id);
        if (lanc == null) return;

        using var f = new LancamentoForm(_svc, lanc);
        if (f.ShowDialog(this) == DialogResult.OK) Carregar();
    }

    private void Excluir()
    {
        var item = Selecionado();
        if (item == null) return;

        var resp = MessageBox.Show($"Excluir o lançamento \"{item.Descricao}\"?", "Confirmar",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (resp != DialogResult.Yes) return;

        _svc.Excluir(item.Id);
        Carregar();
    }

    private void AlternarPago()
    {
        var item = Selecionado();
        if (item == null) return;
        _svc.AlternarPago(item.Id);
        Carregar();
    }

    private void AbrirGraficos()
    {
        using var f = new ChartsForm(_svc, dtMes.Value.Year, dtMes.Value.Month);
        f.ShowDialog(this);
    }

    private void ExportarExcel()
    {
        int ano = dtMes.Value.Year;
        int mes = dtMes.Value.Month;

        using var dlg = new SaveFileDialog
        {
            Title = "Exportar para Excel",
            Filter = "Planilha Excel (*.xlsx)|*.xlsx",
            FileName = $"financeiro_{ano}-{mes:00}.xlsx"
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            var contaId = (cmbConta.SelectedItem as Opcao)?.Id;
            var catId = (cmbCategoria.SelectedItem as Opcao)?.Id;
            var busca = txtBusca.Text.Trim();
            var situacao = cmbSituacao.SelectedItem as string;

            ExcelExporter.Exportar(dlg.FileName, ano, mes,
                _svc.Listar(ano, mes, contaId, catId, busca.Length > 0 ? busca : null, situacao),
                _svc.DespesasPorCategoria(ano, mes),
                _svc.Resumo(ano, mes));

            var abrir = MessageBox.Show("Planilha salva! Deseja abrir agora?", "Exportar para Excel",
                MessageBoxButtons.YesNo, MessageBoxIcon.Information);
            if (abrir == DialogResult.Yes)
                System.Diagnostics.Process.Start(
                    new System.Diagnostics.ProcessStartInfo(dlg.FileName) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message + "\n\nSe o arquivo estiver aberto no Excel, feche-o e tente de novo.",
                "Não foi possível exportar", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void Duplicar()
    {
        var item = Selecionado();
        if (item == null) return;
        var original = _svc.Obter(item.Id);
        if (original == null) return;

        var copia = new Lancamento
        {
            Descricao = original.Descricao,
            Valor = original.Valor,
            Data = DateTime.Today,
            Vencimento = null,
            Pago = true,
            Tipo = original.Tipo,
            CategoriaId = original.CategoriaId,
            ContaId = original.ContaId
        };

        using var f = new LancamentoForm(_svc, copia);
        if (f.ShowDialog(this) == DialogResult.OK) Carregar();
    }

    private void AvisarVencimentos()
    {
        var lista = _svc.Vencimentos(7);
        if (lista.Count == 0) return;

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Contas vencidas ou que vencem nos próximos 7 dias:");
        sb.AppendLine();
        foreach (var i in lista.Take(10))
        {
            var venc = (i.Vencimento ?? i.Data).Date;
            var marca = venc < DateTime.Today ? "VENCIDA"
                      : venc == DateTime.Today ? "vence hoje"
                      : $"vence {venc:dd/MM}";
            sb.AppendLine($"• {i.Descricao} - {i.Valor:C2} ({marca})");
        }
        if (lista.Count > 10)
            sb.AppendLine($"... e mais {lista.Count - 10}.");

        MessageBox.Show(sb.ToString(), "Vencimentos",
            MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    private void RestaurarBackup()
    {
        using var dlg = new OpenFileDialog
        {
            Title = "Escolha o backup para restaurar",
            Filter = "Banco SQLite (*.db)|*.db",
            InitialDirectory = Directory.Exists(FinanceiroService.PastaBackups)
                ? FinanceiroService.PastaBackups : ""
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        var resp = MessageBox.Show(
            "Restaurar vai SUBSTITUIR todos os dados atuais pelos do backup.\n" +
            "Uma cópia dos dados atuais será guardada antes.\n\nContinuar?",
            "Restaurar backup", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (resp != DialogResult.Yes) return;

        try
        {
            var copia = _svc.Restaurar(dlg.FileName);
            CarregarFiltros();
            Carregar();
            MessageBox.Show("Backup restaurado!\n\nCópia dos dados anteriores:\n" + copia,
                "Restaurar backup", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Não foi possível restaurar",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void FazerBackup()
    {
        using var dlg = new SaveFileDialog
        {
            Title = "Salvar backup do banco",
            Filter = "Banco SQLite (*.db)|*.db",
            FileName = $"financeiro_{DateTime.Today:yyyyMMdd}.db"
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            _svc.Backup(dlg.FileName);
            MessageBox.Show("Backup salvo com sucesso!", "Backup",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Não foi possível fazer o backup",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void AbrirContas()
    {
        using var f = new ContasForm(_svc);
        f.ShowDialog(this);
        CarregarFiltros();
        Carregar();
    }

    private void AbrirCategorias()
    {
        using var f = new CategoriasForm(_svc);
        f.ShowDialog(this);
        CarregarFiltros();
        Carregar();
    }
}
