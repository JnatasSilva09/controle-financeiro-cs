using System.Drawing.Drawing2D;

namespace ControleFinanceiro.Forms;

/// <summary>Gráfico de pizza desenhado com GDI+ (sem dependências externas).</summary>
public class PieChartControl : Control
{
    private List<(string Nome, decimal Valor)> _dados = new();

    public string Titulo { get; set; } = "";

    private static readonly Color[] Cores =
    {
        Color.FromArgb(52, 152, 219), Color.FromArgb(231, 76, 60),  Color.FromArgb(46, 204, 113),
        Color.FromArgb(241, 196, 15), Color.FromArgb(155, 89, 182), Color.FromArgb(230, 126, 34),
        Color.FromArgb(26, 188, 156), Color.FromArgb(149, 165, 166), Color.FromArgb(233, 30, 99),
        Color.FromArgb(121, 85, 72)
    };

    public PieChartControl()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        BackColor = Color.White;
    }

    public void Definir(IEnumerable<(string Nome, decimal Valor)> dados)
    {
        _dados = dados.Where(d => d.Valor > 0).ToList();
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        using (var fTitulo = new Font("Segoe UI", 11f, FontStyle.Bold))
            g.DrawString(Titulo, fTitulo, Brushes.Black, 10, 8);

        if (_dados.Count == 0)
        {
            g.DrawString("Sem despesas neste mês.", Font, Brushes.Gray, 10, 45);
            return;
        }

        decimal total = _dados.Sum(d => d.Valor);
        int topo = 45;
        int tamanho = Math.Min((int)(Width * 0.45), Height - topo - 15);
        if (tamanho < 60) return;

        var rect = new Rectangle(10, topo, tamanho, tamanho);
        float inicio = -90f;
        for (int i = 0; i < _dados.Count; i++)
        {
            float varredura = (float)(_dados[i].Valor / total * 360m);
            using var brush = new SolidBrush(Cores[i % Cores.Length]);
            g.FillPie(brush, rect, inicio, varredura);
            inicio += varredura;
        }

        // Legenda
        int x = rect.Right + 20;
        int y = topo;
        for (int i = 0; i < _dados.Count; i++)
        {
            using (var brush = new SolidBrush(Cores[i % Cores.Length]))
                g.FillRectangle(brush, x, y + 3, 12, 12);

            decimal pct = _dados[i].Valor / total;
            g.DrawString($"{_dados[i].Nome}: {_dados[i].Valor:C2} ({pct:P0})",
                         Font, Brushes.Black, x + 18, y);
            y += 24;
        }
    }
}
