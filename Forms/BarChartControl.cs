using System.Drawing.Drawing2D;

namespace ControleFinanceiro.Forms;

public record BarraGrupo(string Rotulo, decimal Receitas, decimal Despesas);

/// <summary>Gráfico de barras agrupadas (receitas x despesas) desenhado com GDI+.</summary>
public class BarChartControl : Control
{
    private List<BarraGrupo> _dados = new();

    public string Titulo { get; set; } = "";

    private static readonly Color CorReceita = Color.FromArgb(46, 160, 90);
    private static readonly Color CorDespesa = Color.FromArgb(214, 69, 65);

    public BarChartControl()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        BackColor = Color.White;
    }

    public void Definir(IEnumerable<BarraGrupo> dados)
    {
        _dados = dados.ToList();
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        using (var fTitulo = new Font("Segoe UI", 11f, FontStyle.Bold))
            g.DrawString(Titulo, fTitulo, Brushes.Black, 10, 8);

        if (_dados.Count == 0) return;

        var area = new Rectangle(70, 55, Width - 90, Height - 55 - 40);
        if (area.Width < 60 || area.Height < 60) return;

        decimal max = _dados.Max(d => Math.Max(d.Receitas, d.Despesas));
        if (max <= 0) max = 1;

        // Linhas de grade e valores do eixo Y
        using (var pen = new Pen(Color.Gainsboro))
        {
            for (int i = 0; i <= 4; i++)
            {
                int y = area.Bottom - (int)(area.Height * i / 4.0);
                g.DrawLine(pen, area.Left, y, area.Right, y);

                string texto = (max * i / 4).ToString("N0");
                var tam = g.MeasureString(texto, Font);
                g.DrawString(texto, Font, Brushes.Gray, area.Left - tam.Width - 4, y - tam.Height / 2);
            }
        }

        float larguraGrupo = (float)area.Width / _dados.Count;
        float larguraBarra = larguraGrupo * 0.3f;

        using var brushR = new SolidBrush(CorReceita);
        using var brushD = new SolidBrush(CorDespesa);

        for (int i = 0; i < _dados.Count; i++)
        {
            var d = _dados[i];
            float xGrupo = area.Left + i * larguraGrupo;
            float x1 = xGrupo + larguraGrupo * 0.15f;
            float x2 = x1 + larguraBarra + 3;

            float hR = (float)(d.Receitas / max) * area.Height;
            float hD = (float)(d.Despesas / max) * area.Height;

            g.FillRectangle(brushR, x1, area.Bottom - hR, larguraBarra, hR);
            g.FillRectangle(brushD, x2, area.Bottom - hD, larguraBarra, hD);

            var tam = g.MeasureString(d.Rotulo, Font);
            g.DrawString(d.Rotulo, Font, Brushes.Black,
                         xGrupo + (larguraGrupo - tam.Width) / 2, area.Bottom + 6);
        }

        // Legenda
        int lx = Math.Max(area.Left, Width - 220);
        g.FillRectangle(brushR, lx, 32, 12, 12);
        g.DrawString("Receitas", Font, Brushes.Black, lx + 16, 29);
        g.FillRectangle(brushD, lx + 90, 32, 12, 12);
        g.DrawString("Despesas", Font, Brushes.Black, lx + 106, 29);
    }
}
