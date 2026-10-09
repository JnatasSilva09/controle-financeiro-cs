using ControleFinanceiro.Services;

namespace ControleFinanceiro.Forms;

public class ChartsForm : Form
{
    private readonly FinanceiroService _svc;

    private readonly DateTimePicker dtMes = new()
    {
        Format = DateTimePickerFormat.Custom,
        CustomFormat = "MMMM 'de' yyyy",
        ShowUpDown = true,
        Width = 170
    };
    private readonly PieChartControl pie = new() { Dock = DockStyle.Fill };
    private readonly BarChartControl barras = new() { Dock = DockStyle.Fill };

    public ChartsForm(FinanceiroService svc, int ano, int mes)
    {
        _svc = svc;
        Text = "Gráficos";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(1100, 520);
        Font = new Font("Segoe UI", 9.5f);

        var topo = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 46, Padding = new Padding(8, 8, 0, 0) };
        topo.Controls.Add(new Label { Text = "Mês:", AutoSize = true, Margin = new Padding(0, 6, 4, 0) });
        topo.Controls.Add(dtMes);

        var tabela = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        tabela.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        tabela.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        tabela.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        tabela.Controls.Add(pie, 0, 0);
        tabela.Controls.Add(barras, 1, 0);

        Controls.Add(tabela);
        Controls.Add(topo);

        dtMes.Value = new DateTime(ano, mes, 1);
        dtMes.ValueChanged += (_, _) => Recarregar();
        Recarregar();
    }

    private void Recarregar()
    {
        int ano = dtMes.Value.Year;
        int mes = dtMes.Value.Month;

        pie.Titulo = $"Despesas por categoria ({dtMes.Value:MM/yyyy})";
        pie.Definir(_svc.DespesasPorCategoria(ano, mes)
                        .Select(d => (d.Categoria, d.Total)));

        barras.Titulo = "Receitas x despesas (últimos 6 meses)";
        barras.Definir(_svc.EvolucaoMensal(ano, mes, 6)
                           .Select(x => new BarraGrupo(x.Mes, x.Receitas, x.Despesas)));
    }
}
