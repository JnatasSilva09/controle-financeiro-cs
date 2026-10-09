using ClosedXML.Excel;

namespace ControleFinanceiro.Services;

public static class ExcelExporter
{
    private const string FormatoMoeda = "\"R$\" #,##0.00";
    private const string FormatoData = "dd/mm/yyyy";

    public static void Exportar(string caminho, int ano, int mes,
        List<LancamentoItem> itens, List<DespesaCategoria> categorias, ResumoMes resumo)
    {
        using var wb = new XLWorkbook();

        // ----- Aba 1: lançamentos (respeita os filtros da tela) -----
        var ws = wb.Worksheets.Add("Lançamentos");
        string[] cabecalho =
        {
            "Data", "Descrição", "Categoria", "Conta", "Tipo",
            "Valor (+receita / -despesa)", "Situação", "Vencimento"
        };
        for (int c = 0; c < cabecalho.Length; c++)
            ws.Cell(1, c + 1).Value = cabecalho[c];

        var faixa = ws.Range(1, 1, 1, cabecalho.Length);
        faixa.Style.Font.Bold = true;
        faixa.Style.Fill.BackgroundColor = XLColor.LightGray;

        int linha = 2;
        foreach (var i in itens.OrderBy(x => x.Data).ThenBy(x => x.Id))
        {
            ws.Cell(linha, 1).Value = i.Data;
            ws.Cell(linha, 1).Style.DateFormat.Format = FormatoData;
            ws.Cell(linha, 2).Value = i.Descricao;
            ws.Cell(linha, 3).Value = i.Categoria;
            ws.Cell(linha, 4).Value = i.Conta;
            ws.Cell(linha, 5).Value = i.Tipo;
            ws.Cell(linha, 6).Value = i.Tipo == "Despesa" ? -i.Valor : i.Valor;
            ws.Cell(linha, 6).Style.NumberFormat.Format = FormatoMoeda;
            ws.Cell(linha, 7).Value = i.Situacao;
            if (i.Vencimento.HasValue)
            {
                ws.Cell(linha, 8).Value = i.Vencimento.Value;
                ws.Cell(linha, 8).Style.DateFormat.Format = FormatoData;
            }
            linha++;
        }

        if (itens.Count > 0)
        {
            ws.Cell(linha, 5).Value = "Total";
            ws.Cell(linha, 6).FormulaA1 = $"SUM(F2:F{linha - 1})";
            ws.Cell(linha, 6).Style.NumberFormat.Format = FormatoMoeda;
            ws.Range(linha, 5, linha, 6).Style.Font.Bold = true;
        }
        ws.SheetView.FreezeRows(1);
        ws.Columns().AdjustToContents();

        // ----- Aba 2: resumo do mês (sem filtros) -----
        var rs = wb.Worksheets.Add("Resumo");
        rs.Cell(1, 1).Value = $"Resumo de {new DateTime(ano, mes, 1):MMMM/yyyy}";
        rs.Cell(1, 1).Style.Font.Bold = true;

        rs.Cell(2, 1).Value = "Receitas recebidas";
        rs.Cell(2, 2).Value = resumo.Receitas;
        rs.Cell(3, 1).Value = "Despesas pagas";
        rs.Cell(3, 2).Value = resumo.Despesas;
        rs.Cell(4, 1).Value = "Resultado do mês (receitas - despesas)";
        rs.Cell(4, 2).Value = resumo.Receitas - resumo.Despesas;
        rs.Cell(5, 1).Value = "A pagar (pendentes)";
        rs.Cell(5, 2).Value = resumo.Pendente;
        rs.Range(2, 2, 5, 2).Style.NumberFormat.Format = FormatoMoeda;

        rs.Cell(7, 1).Value = "Despesas por categoria";
        rs.Cell(7, 2).Value = "Total";
        rs.Range(7, 1, 7, 2).Style.Font.Bold = true;
        rs.Range(7, 1, 7, 2).Style.Fill.BackgroundColor = XLColor.LightGray;

        int l2 = 8;
        foreach (var c in categorias)
        {
            rs.Cell(l2, 1).Value = c.Categoria;
            rs.Cell(l2, 2).Value = c.Total;
            rs.Cell(l2, 2).Style.NumberFormat.Format = FormatoMoeda;
            l2++;
        }
        rs.Columns().AdjustToContents();

        wb.SaveAs(caminho);
    }
}
