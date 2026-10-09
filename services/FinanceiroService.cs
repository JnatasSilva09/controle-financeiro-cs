using ControleFinanceiro.Data;
using ControleFinanceiro.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ControleFinanceiro.Services;

public class LancamentoItem
{
    public int Id { get; set; }
    public DateTime Data { get; set; }
    public string Descricao { get; set; } = "";
    public string Categoria { get; set; } = "";
    public string Conta { get; set; } = "";
    public string Tipo { get; set; } = "";
    public decimal Valor { get; set; }
    public string Situacao { get; set; } = "";
    public DateTime? Vencimento { get; set; }
}

public class DespesaCategoria
{
    public string Categoria { get; set; } = "";
    public decimal Total { get; set; }
}

public record ResumoMes(decimal Receitas, decimal Despesas, decimal Pendente);

public record EvolucaoItem(string Mes, decimal Receitas, decimal Despesas);

public enum ModoRepeticao
{
    Unica = 0,
    Parcelado = 1,
    Recorrente = 2
}

// Obs.: o SQLite não soma "decimal" direto no banco, então as somas
// são feitas em memória (para uso pessoal o volume de dados é pequeno).
public class FinanceiroService
{
    // ---------- Contas ----------
    public List<Conta> ListarContas()
    {
        using var db = new AppDbContext();
        return db.Contas.OrderBy(c => c.Nome).ToList();
    }

    public void AdicionarConta(string nome, decimal saldoInicial)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("Informe o nome da conta.");
        using var db = new AppDbContext();
        db.Contas.Add(new Conta { Nome = nome.Trim(), SaldoInicial = saldoInicial });
        db.SaveChanges();
    }

    /// <returns>mensagem de erro, ou null se excluiu.</returns>
    public string? ExcluirConta(int id)
    {
        using var db = new AppDbContext();
        if (db.Contas.Count() <= 1) return "Mantenha pelo menos uma conta.";
        if (db.Lancamentos.Any(l => l.ContaId == id))
            return "Esta conta possui lançamentos e não pode ser excluída.";
        var conta = db.Contas.Find(id);
        if (conta != null)
        {
            db.Contas.Remove(conta);
            db.SaveChanges();
        }
        return null;
    }

    // ---------- Categorias ----------
    public List<Categoria> ListarCategorias(TipoLancamento? tipo = null)
    {
        using var db = new AppDbContext();
        var q = db.Categorias.AsQueryable();
        if (tipo.HasValue) q = q.Where(c => c.Tipo == tipo.Value);
        return q.OrderBy(c => c.Nome).ToList();
    }

    public void AdicionarCategoria(string nome, TipoLancamento tipo)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("Informe o nome da categoria.");
        using var db = new AppDbContext();
        db.Categorias.Add(new Categoria { Nome = nome.Trim(), Tipo = tipo });
        db.SaveChanges();
    }

    public string? ExcluirCategoria(int id)
    {
        using var db = new AppDbContext();
        if (db.Lancamentos.Any(l => l.CategoriaId == id))
            return "Esta categoria possui lançamentos e não pode ser excluída.";
        var cat = db.Categorias.Find(id);
        if (cat != null)
        {
            db.Categorias.Remove(cat);
            db.SaveChanges();
        }
        return null;
    }

    // ---------- Lançamentos ----------
    public Lancamento? Obter(int id)
    {
        using var db = new AppDbContext();
        return db.Lancamentos.Find(id);
    }

    private static void Validar(Lancamento l)
    {
        if (string.IsNullOrWhiteSpace(l.Descricao))
            throw new ArgumentException("Informe a descrição.");
        if (l.Valor <= 0)
            throw new ArgumentException("O valor deve ser maior que zero.");
        if (l.CategoriaId <= 0)
            throw new ArgumentException("Escolha uma categoria.");
        if (l.ContaId <= 0)
            throw new ArgumentException("Escolha uma conta.");
    }

    public void Salvar(Lancamento l)
    {
        Validar(l);

        using var db = new AppDbContext();
        if (l.Id == 0) db.Lancamentos.Add(l);
        else db.Lancamentos.Update(l);
        db.SaveChanges();
    }

    /// <summary>
    /// Gera vários lançamentos mensais a partir de um modelo.
    /// Parcelado: "Descrição (1/12)", "(2/12)"...  Recorrente: mesma descrição todo mês.
    /// O valor informado é o de CADA parcela/mês. Só o primeiro respeita o campo
    /// "Pago"; os demais nascem como pendentes.
    /// </summary>
    public void SalvarRepetido(Lancamento modelo, ModoRepeticao modo, int quantidade)
    {
        Validar(modelo);
        if (quantidade < 2 || quantidade > 120)
            throw new ArgumentException("A quantidade deve estar entre 2 e 120.");

        using var db = new AppDbContext();
        for (int i = 0; i < quantidade; i++)
        {
            db.Lancamentos.Add(new Lancamento
            {
                Descricao = modo == ModoRepeticao.Parcelado
                    ? $"{modelo.Descricao.Trim()} ({i + 1}/{quantidade})"
                    : modelo.Descricao.Trim(),
                Valor = modelo.Valor,
                Data = modelo.Data.AddMonths(i),
                Vencimento = modelo.Vencimento?.AddMonths(i),
                Pago = i == 0 && modelo.Pago,
                Tipo = modelo.Tipo,
                CategoriaId = modelo.CategoriaId,
                ContaId = modelo.ContaId
            });
        }
        db.SaveChanges();
    }

    /// <summary>Receitas e despesas (todos os lançamentos) dos últimos N meses, até o mês informado.</summary>
    public List<EvolucaoItem> EvolucaoMensal(int ano, int mes, int meses = 6)
    {
        using var db = new AppDbContext();
        var fim = new DateTime(ano, mes, 1).AddMonths(1);
        var inicio = fim.AddMonths(-meses);

        var dados = db.Lancamentos
            .Where(l => l.Data >= inicio && l.Data < fim)
            .Select(l => new { l.Data, l.Tipo, l.Valor })
            .ToList();

        var resultado = new List<EvolucaoItem>();
        for (int i = 0; i < meses; i++)
        {
            var ini = inicio.AddMonths(i);
            var f = ini.AddMonths(1);
            var doMes = dados.Where(x => x.Data >= ini && x.Data < f).ToList();

            resultado.Add(new EvolucaoItem(
                ini.ToString("MM/yy"),
                doMes.Where(x => x.Tipo == TipoLancamento.Receita).Sum(x => x.Valor),
                doMes.Where(x => x.Tipo == TipoLancamento.Despesa).Sum(x => x.Valor)));
        }
        return resultado;
    }

    // ---------- Backup ----------
    public void Backup(string caminhoDestino)
    {
        using var origem = new SqliteConnection($"Data Source={AppDbContext.CaminhoBanco}");
        using var destino = new SqliteConnection($"Data Source={caminhoDestino}");
        origem.Open();
        destino.Open();
        origem.BackupDatabase(destino);
    }

    public void Excluir(int id)
    {
        using var db = new AppDbContext();
        var l = db.Lancamentos.Find(id);
        if (l == null) return;
        db.Lancamentos.Remove(l);
        db.SaveChanges();
    }

    public void AlternarPago(int id)
    {
        using var db = new AppDbContext();
        var l = db.Lancamentos.Find(id);
        if (l == null) return;
        l.Pago = !l.Pago;
        db.SaveChanges();
    }

    public List<LancamentoItem> Listar(int ano, int mes, int? contaId, int? categoriaId)
    {
        using var db = new AppDbContext();
        var inicio = new DateTime(ano, mes, 1);
        var fim = inicio.AddMonths(1);

        var q = db.Lancamentos
            .Include(l => l.Categoria)
            .Include(l => l.Conta)
            .Where(l => l.Data >= inicio && l.Data < fim);

        if (contaId.HasValue) q = q.Where(l => l.ContaId == contaId.Value);
        if (categoriaId.HasValue) q = q.Where(l => l.CategoriaId == categoriaId.Value);

        return q.OrderByDescending(l => l.Data)
                .ThenByDescending(l => l.Id)
                .ToList()
                .Select(l => new LancamentoItem
                {
                    Id = l.Id,
                    Data = l.Data,
                    Descricao = l.Descricao,
                    Categoria = l.Categoria.Nome,
                    Conta = l.Conta.Nome,
                    Tipo = l.Tipo.ToString(),
                    Valor = l.Valor,
                    Situacao = l.Pago ? "Pago" : "Pendente",
                    Vencimento = l.Vencimento
                })
                .ToList();
    }

    // ---------- Resumos ----------
    public decimal SaldoTotal()
    {
        using var db = new AppDbContext();
        var inicial = db.Contas.Select(c => c.SaldoInicial).ToList().Sum();
        var pagos = db.Lancamentos.Where(l => l.Pago)
                      .Select(l => new { l.Tipo, l.Valor }).ToList();

        var receitas = pagos.Where(x => x.Tipo == TipoLancamento.Receita).Sum(x => x.Valor);
        var despesas = pagos.Where(x => x.Tipo == TipoLancamento.Despesa).Sum(x => x.Valor);
        return inicial + receitas - despesas;
    }

    public ResumoMes Resumo(int ano, int mes)
    {
        using var db = new AppDbContext();
        var inicio = new DateTime(ano, mes, 1);
        var fim = inicio.AddMonths(1);

        var doMes = db.Lancamentos
            .Where(l => l.Data >= inicio && l.Data < fim)
            .Select(l => new { l.Tipo, l.Valor, l.Pago })
            .ToList();

        var receitas = doMes.Where(x => x.Tipo == TipoLancamento.Receita && x.Pago).Sum(x => x.Valor);
        var despesas = doMes.Where(x => x.Tipo == TipoLancamento.Despesa && x.Pago).Sum(x => x.Valor);
        var pendente = doMes.Where(x => x.Tipo == TipoLancamento.Despesa && !x.Pago).Sum(x => x.Valor);
        return new ResumoMes(receitas, despesas, pendente);
    }

    public List<DespesaCategoria> DespesasPorCategoria(int ano, int mes)
    {
        using var db = new AppDbContext();
        var inicio = new DateTime(ano, mes, 1);
        var fim = inicio.AddMonths(1);

        return db.Lancamentos
            .Include(l => l.Categoria)
            .Where(l => l.Tipo == TipoLancamento.Despesa && l.Data >= inicio && l.Data < fim)
            .ToList()
            .GroupBy(l => l.Categoria.Nome)
            .Select(g => new DespesaCategoria { Categoria = g.Key, Total = g.Sum(x => x.Valor) })
            .OrderByDescending(x => x.Total)
            .ToList();
    }
}
