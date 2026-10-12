using ControleFinanceiro.Data;
using ControleFinanceiro.Models;
using ControleFinanceiro.Services;
using ScottPlot.WinForms;
using System;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;

namespace ControleFinanceiro.Forms;

public class MainForm : Form
{
    private record Opcao(int? Id, string Nome)
    {
        public override string ToString() => Nome;
    }

    private readonly FinanceiroService _svc = new();
    private bool _carregando;

    // Paleta de Cores Estilo FlowFin Profissional
    private static readonly Color CorSidebar = Color.FromArgb(30, 41, 59);       
    private static readonly Color CorSidebarHover = Color.FromArgb(51, 65, 85);       
    private static readonly Color CorFundoClaro = Color.FromArgb(248, 250, 252);   
    private static readonly Color CorVerdeSucesso = Color.FromArgb(16, 185, 129);  
    private static readonly Color CorBordaCard = Color.FromArgb(226, 232, 240);    

    // Componentes de Filtro e Dados
    private readonly DateTimePicker dtMes = new()
    {
        Format = DateTimePickerFormat.Custom,
        CustomFormat = "MMMM 'de' yyyy",
        ShowUpDown = true,
        Width = 160
    };
    private readonly ComboBox cmbConta = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 140 };
    private readonly ComboBox cmbCategoria = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 140 };
    private readonly ComboBox cmbPeriodoRapido = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 130 };

    private readonly DataGridView grid = NovaGrid();
    
    // Cards Superiores com Indicadores
    private readonly Panel cardSaldo = CriarCardPanel();
    private readonly Panel cardReceitas = CriarCardPanel();
    private readonly Panel cardDespesas = CriarCardPanel();
    private readonly Panel cardTransacoes = CriarCardPanel();

    private readonly Label lblSaldoVal = new() { Font = new Font("Segoe UI", 13f, FontStyle.Bold), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
    private readonly Label lblReceitasVal = new() { Font = new Font("Segoe UI", 13f, FontStyle.Bold), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Color.FromArgb(16, 185, 129) };
    private readonly Label lblDespesasVal = new() { Font = new Font("Segoe UI", 13f, FontStyle.Bold), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Color.FromArgb(225, 29, 72) };
    private readonly Label lblTransacoesVal = new() { Font = new Font("Segoe UI", 13f, FontStyle.Bold), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };

    private readonly Label lblSaldoSub = new() { Font = new Font("Segoe UI", 7.5f), ForeColor = Color.FromArgb(100, 116, 139), Dock = DockStyle.Bottom, Height = 18 };
    private readonly Label lblReceitasSub = new() { Font = new Font("Segoe UI", 7.5f), ForeColor = Color.FromArgb(100, 116, 139), Dock = DockStyle.Bottom, Height = 18 };
    private readonly Label lblDespesasSub = new() { Font = new Font("Segoe UI", 7.5f), ForeColor = Color.FromArgb(100, 116, 139), Dock = DockStyle.Bottom, Height = 18 };
    private readonly Label lblTransacoesSub = new() { Font = new Font("Segoe UI", 7.5f), ForeColor = Color.FromArgb(100, 116, 139), Dock = DockStyle.Bottom, Height = 18 };

    // Gráfico ScottPlot integrado
    private readonly FormsPlot plotGrafico = new() { Dock = DockStyle.Fill };

    public MainForm()
    {
        Text = "Controle Financeiro Pessoal";
        Width = 1320;
        Height = 800;
        MinimumSize = new Size(1100, 700);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9.5f);
        BackColor = CorFundoClaro;

       // 1. MENU LATERAL (Sidebar com o logotipo no topo e botões abaixo)
        var pnlSidebar = new Panel
        {
            Dock = DockStyle.Left,
            Width = 240,
            BackColor = CorSidebar,
            Padding = new Padding(10)
        };

        var lblLogo = new Label
        {
            Text = "Meu Financeiro",
            Font = new Font("Segoe UI", 14f, FontStyle.Bold),
            ForeColor = Color.White,
            Dock = DockStyle.Top,
            Height = 60,
            TextAlign = ContentAlignment.MiddleCenter
        };

        // Adiciona primeiro o logotipo para ocupar o topo
        pnlSidebar.Controls.Add(lblLogo);

        // Adiciona os botões de atalho em sequência (de cima para baixo)
        pnlSidebar.Controls.Add(BotaoMenu("🔒  Fazer Backup", FazerBackup));
        pnlSidebar.Controls.Add(BotaoMenu("📥  Exportar Excel", ExportarExcel));
        pnlSidebar.Controls.Add(BotaoMenu("📁  Categorias", AbrirCategorias));
        pnlSidebar.Controls.Add(BotaoMenu("💳  Gerir Contas", AbrirContas));
        pnlSidebar.Controls.Add(BotaoMenu("📊  Visão Geral", () => { }));
        

        // 2. PAINEL PRINCIPAL
        var pnlPrincipal = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(15)
        };

        var pnlHeaderTop = new Panel
        {
            Dock = DockStyle.Top,
            Height = 55
        };
        
        var btnNovaTransacao = new Button
        {
            Text = "+ Nova Transação",
            BackColor = CorVerdeSucesso,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            Width = 165,
            Height = 38,
            Cursor = Cursors.Hand,
            Dock = DockStyle.Right
        };
        btnNovaTransacao.FlatAppearance.BorderSize = 0;
        btnNovaTransacao.Click += (_, _) => Novo();
        pnlHeaderTop.Controls.Add(btnNovaTransacao);

        var lblTituloTopo = new Label
        {
            Text = "Visão Geral",
            Font = new Font("Segoe UI", 16f, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 41, 59),
            Dock = DockStyle.Left,
            TextAlign = ContentAlignment.MiddleLeft,
            Width = 200
        };
        pnlHeaderTop.Controls.Add(lblTituloTopo);

        // 3. CARDS SUPERIORES COM INDICADORES
        var pnlResumo = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 95,
            ColumnCount = 4,
            Padding = new Padding(0, 0, 0, 10)
        };
        for (int i = 0; i < 4; i++) pnlResumo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));

        MontarConteudoCard(cardSaldo, "Saldo Total", lblSaldoVal, lblSaldoSub);
        MontarConteudoCard(cardReceitas, "Receitas", lblReceitasVal, lblReceitasSub);
        MontarConteudoCard(cardDespesas, "Despesas", lblDespesasVal, lblDespesasSub);
        MontarConteudoCard(cardTransacoes, "Transações", lblTransacoesVal, lblTransacoesSub);

        pnlResumo.Controls.Add(cardSaldo, 0, 0);
        pnlResumo.Controls.Add(cardReceitas, 1, 0);
        pnlResumo.Controls.Add(cardDespesas, 2, 0);
        pnlResumo.Controls.Add(cardTransacoes, 3, 0);

        // 4. PAINEL DE FILTROS AVANÇADOS E RÁPIDOS
        cmbPeriodoRapido.Items.AddRange(new object[] { "Mês Atual", "Este Ano", "Personalizado" });
        cmbPeriodoRapido.SelectedIndex = 0;

        var pnlFiltros = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 50,
            Padding = new Padding(0, 5, 0, 5)
        };
        pnlFiltros.Controls.AddRange(new Control[]
        {
            Rotulo("Período:"), cmbPeriodoRapido,
            Rotulo("Mês:"), dtMes,
            Rotulo("Conta:"), cmbConta,
            Rotulo("Categoria:"), cmbCategoria,
            BotaoAcaoSecundaria("Pago/Pendente", AlternarPago),
            BotaoAcaoSecundaria("Editar", Editar),
            BotaoAcaoSecundaria("Excluir", Excluir)
        });

        // 5. ÁREA INFERIOR: Tabela + Gráfico Otimizado
        var splitInferior = new SplitContainer
        {
            Dock = DockStyle.Fill,
            SplitterDistance = 650,
            Orientation = Orientation.Vertical
        };

        EstilizarGrid(grid);
        splitInferior.Panel1.Controls.Add(grid);
        
        var pnlGraficoContainer = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12, 0, 0, 0), BackColor = Color.White };
        pnlGraficoContainer.Controls.Add(plotGrafico);
        splitInferior.Panel2.Controls.Add(pnlGraficoContainer);

        pnlPrincipal.Controls.AddRange(new Control[] { splitInferior, pnlFiltros, pnlResumo, pnlHeaderTop });
        Controls.AddRange(new Control[] { pnlPrincipal, pnlSidebar });

        // Eventos
        dtMes.ValueChanged += (_, _) => { if (!_carregando) Carregar(); };
        cmbConta.SelectedIndexChanged += (_, _) => { if (!_carregando) Carregar(); };
        cmbCategoria.SelectedIndexChanged += (_, _) => { if (!_carregando) Carregar(); };
        cmbPeriodoRapido.SelectedIndexChanged += (_, _) =>
        {
            if (cmbPeriodoRapido.SelectedIndex == 0) dtMes.Value = DateTime.Today;
            Carregar();
        };
        grid.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) Editar(); };
        grid.CellFormatting += FormatarCelula;

        Load += (_, _) =>
        {
            dtMes.Value = DateTime.Today;
            CarregarFiltros();
            Carregar();
        };
    }

    private static Panel CriarCardPanel() => new()
    {
        Dock = DockStyle.Fill,
        BackColor = Color.White,
        Margin = new Padding(4),
        Padding = new Padding(12, 8, 12, 8)
    };

    private static void MontarConteudoCard(Panel painel, string titulo, Label lblValor, Label lblSub)
    {
        var lblTitulo = new Label
        {
            Text = titulo.ToUpper(),
            Font = new Font("Segoe UI", 8f, FontStyle.Bold),
            ForeColor = Color.FromArgb(100, 116, 139),
            Dock = DockStyle.Top,
            Height = 18
        };
        painel.Controls.Add(lblValor);
        painel.Controls.Add(lblSub);
        painel.Controls.Add(lblTitulo);
    }

 private static Button BotaoMenu(string texto, Action acao)
    {
        var b = new Button
        {
            Text = texto,
            ForeColor = Color.FromArgb(203, 213, 225),
            BackColor = Color.Transparent,
            FlatStyle = FlatStyle.Flat,
            Height = 44,
            Dock = DockStyle.Top,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 10f),
            Cursor = Cursors.Hand,
            Padding = new Padding(10, 0, 0, 0)
        };
        b.FlatAppearance.BorderSize = 0;
        b.MouseEnter += (_, _) => { b.BackColor = CorSidebarHover; b.ForeColor = Color.White; };
        b.MouseLeave += (_, _) => { b.BackColor = Color.Transparent; b.ForeColor = Color.FromArgb(203, 213, 225); };
        b.Click += (_, _) => acao();
        return b;
    }

    private static Button BotaoAcaoSecundaria(string texto, Action acao)
    {
        var b = new Button
        {
            Text = texto,
            AutoSize = true,
            Height = 30,
            BackColor = Color.White,
            ForeColor = Color.FromArgb(30, 41, 59),
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(4, 3, 4, 2),
            Cursor = Cursors.Hand
        };
        b.FlatAppearance.BorderColor = CorBordaCard;
        b.Click += (_, _) => acao();
        return b;
    }

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
        Dock = DockStyle.Fill,
        BackgroundColor = Color.White,
        BorderStyle = BorderStyle.None
    };

    private static void EstilizarGrid(DataGridView tabela)
    {
        tabela.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        tabela.GridColor = CorBordaCard;
        tabela.EnableHeadersVisualStyles = false;
        tabela.ColumnHeadersDefaultCellStyle.BackColor = CorSidebar;
        tabela.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        tabela.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
        tabela.ColumnHeadersHeight = 36;
        tabela.RowTemplate.Height = 32;
        tabela.DefaultCellStyle.SelectionBackColor = Color.FromArgb(224, 242, 254);
        tabela.DefaultCellStyle.SelectionForeColor = CorSidebar;
        tabela.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(249, 250, 251);
    }

    private static Label Rotulo(string texto) => new()
    {
        Text = texto,
        AutoSize = true,
        Margin = new Padding(4, 8, 2, 0),
        ForeColor = Color.FromArgb(71, 85, 105)
    };

    private void CarregarFiltros()
    {
        _carregando = true;
        var contaSel = (cmbConta.SelectedItem as Opcao)?.Id;
        var catSel = (cmbCategoria.SelectedItem as Opcao)?.Id;

        cmbConta.Items.Clear();
        cmbConta.Items.Add(new Opcao(null, "Todas"));
        foreach (var c in _svc.ListarContas()) cmbConta.Items.Add(new Opcao(c.Id, c.Nome));
        Selecionar(cmbConta, contaSel);

        cmbCategoria.Items.Clear();
        cmbCategoria.Items.Add(new Opcao(null, "Todas"));
        foreach (var c in _svc.ListarCategorias()) cmbCategoria.Items.Add(new Opcao(c.Id, c.Nome));
        Selecionar(cmbCategoria, catSel);
        _carregando = false;
    }

    private static void Selecionar(ComboBox cmb, int? id)
    {
        cmb.SelectedIndex = 0;
        for (int i = 0; i < cmb.Items.Count; i++)
        {
            if (cmb.Items[i] is Opcao o && o.Id == id) { cmb.SelectedIndex = i; break; }
        }
    }

    private void Carregar()
    {
        int ano = dtMes.Value.Year;
        int mes = dtMes.Value.Month;
        var contaId = (cmbConta.SelectedItem as Opcao)?.Id;
        var catId = (cmbCategoria.SelectedItem as Opcao)?.Id;

        var lista = _svc.Listar(ano, mes, contaId, catId);
        grid.DataSource = lista;
        ConfigurarColunas();

        var r = _svc.Resumo(ano, mes);
        lblSaldoVal.Text = _svc.SaldoTotal().ToString("C2");
        lblReceitasVal.Text = r.Receitas.ToString("C2");
        lblDespesasVal.Text = r.Despesas.ToString("C2");
        lblTransacoesVal.Text = lista.Count.ToString();

        // Subtítulos com indicadores inteligentes nos cards
        lblSaldoSub.Text = "Acumulado geral";
        lblReceitasSub.Text = $"Mês de {dtMes.Text}";
        lblDespesasSub.Text = $"Mês de {dtMes.Text}";
        lblTransacoesSub.Text = "Registros no período";

        AtualizarGraficoScottPlot(ano, mes);
    }

    private void AtualizarGraficoScottPlot(int ano, int mes)
    {
        plotGrafico.Plot.Clear();
        var despesasCat = _svc.DespesasPorCategoria(ano, mes);
        if (despesasCat.Count > 0)
        {
            double[] valores = despesasCat.Select(x => (double)x.Total).ToArray();
            string[] labels = despesasCat.Select(x => x.Categoria).ToArray();

            var bar = plotGrafico.Plot.Add.Bars(valores);
            foreach (var b in bar.Bars)
            {
                b.FillColor = ScottPlot.Color.FromHex("#10B981");
            }

            plotGrafico.Plot.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericManual(
                Enumerable.Range(0, labels.Length).Select(i => (double)i).ToArray(),
                labels
            );
        }
        else
        {
            plotGrafico.Plot.Title("Sem despesas registadas no período");
        }

        plotGrafico.Plot.HideGrid();
        plotGrafico.Refresh();
    }

    private void ConfigurarColunas()
    {
        foreach (DataGridViewColumn c in grid.Columns)
        {
            switch (c.Name)
            {
                case "Id": c.Visible = false; break;
                case "Data":
                case "Vencimento": c.DefaultCellStyle.Format = "dd/MM/yyyy"; break;
                case "Valor": c.DefaultCellStyle.Format = "C2"; c.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight; break;
                case "Descricao": c.HeaderText = "Descrição"; c.FillWeight = 180; break;
                case "Situacao": c.HeaderText = "Situação"; break;
            }
        }
    }

    private void FormatarCelula(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || grid.Rows[e.RowIndex].DataBoundItem is not LancamentoItem item) return;
        
        // Destacar valor por cor (Receita = Verde, Despesa = Vermelho)
        if (grid.Columns[e.ColumnIndex].Name == "Valor")
            e.CellStyle!.ForeColor = item.Tipo == "Receita" ? CorVerdeSucesso : Color.FromArgb(225, 29, 72);

        // Alerta visual para vencimentos pendentes
        if (grid.Columns[e.ColumnIndex].Name == "Vencimento" && item.Situacao == "Pendente" && item.Vencimento < DateTime.Today)
        {
            e.CellStyle!.ForeColor = Color.FromArgb(225, 29, 72);
            e.CellStyle.Font = new Font(grid.Font, FontStyle.Bold);
        }
    }

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
        if (MessageBox.Show($"Pretende excluir \"{item.Descricao}\"?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
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

    private void ExportarExcel()
    {
        int ano = dtMes.Value.Year;
        int mes = dtMes.Value.Month;
        using var dlg = new SaveFileDialog { Filter = "Excel (*.xlsx)|*.xlsx", FileName = $"financeiro_{ano}-{mes:00}.xlsx" };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            ExcelExporter.Exportar(dlg.FileName, ano, mes, _svc.Listar(ano, mes, null, null), _svc.DespesasPorCategoria(ano, mes), _svc.Resumo(ano, mes));
            MessageBox.Show("Planilha exportada com sucesso!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    }

    private void FazerBackup()
    {
        using var dlg = new SaveFileDialog { Filter = "Base de dados (*.db)|*.db", FileName = $"backup_{DateTime.Today:yyyyMMdd}.db" };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        try { _svc.Backup(dlg.FileName); MessageBox.Show("Backup efetuado com sucesso!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    }

    private void AbrirContas()
    {
        using var f = new ContasForm(_svc); f.ShowDialog(this); CarregarFiltros(); Carregar();
    }

    private void AbrirCategorias()
    {
        using var f = new CategoriasForm(_svc); f.ShowDialog(this); CarregarFiltros(); Carregar();
    }
}