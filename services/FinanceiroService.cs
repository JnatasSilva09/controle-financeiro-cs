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
        var nomeNormalizado = nome.Trim();
        using var db = new AppDbContext();
        if (db.Contas.AsEnumerable().Any(c =>
                string.Equals(c.Nome.Trim(), nomeNormalizado, StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException("Já existe uma conta com esse nome. Escolha outro nome para facilitar a identificação.");

        db.Contas.Add(new Conta { Nome = nomeNormalizado, SaldoInicial = saldoInicial });
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
        var nomeNormalizado = nome.Trim();
        using var db = new AppDbContext();
        if (db.Categorias.AsEnumerable().Any(c => c.Tipo == tipo &&
                string.Equals(c.Nome.Trim(), nomeNormalizado, StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException($"Já existe uma categoria de {tipo.ToString().ToLowerInvariant()} com esse nome.");

        db.Categorias.Add(new Categoria { Nome = nomeNormalizado, Tipo = tipo });
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

    private static void ValidarCampos(Lancamento l)
    {
        if (string.IsNullOrWhiteSpace(l.Descricao))
            throw new ArgumentException("Dê um nome para este lançamento, por exemplo: Conta de luz ou Salário.");
        if (l.Descricao.Trim().Length > 120)
            throw new ArgumentException("A descrição deve ter no máximo 120 caracteres.");
        if (l.Valor <= 0)
            throw new ArgumentException("Informe um valor maior que R$ 0,00.");
        if (l.CategoriaId <= 0)
            throw new ArgumentException("Selecione uma categoria para organizar este lançamento.");
        if (l.ContaId <= 0)
            throw new ArgumentException("Selecione a conta relacionada a este lançamento.");
        if (!Enum.IsDefined(typeof(TipoLancamento), l.Tipo))
            throw new ArgumentException("Selecione se o lançamento é uma receita ou uma despesa.");
    }

    private static void ValidarRelacionamentos(AppDbContext db, Lancamento l)
    {
        var contaExiste = db.Contas.Any(c => c.Id == l.ContaId);
        if (!contaExiste)
            throw new ArgumentException("A conta selecionada não foi encontrada. Atualize a tela e tente novamente.");

        var categoria = db.Categorias.FirstOrDefault(c => c.Id == l.CategoriaId);
        if (categoria == null)
            throw new ArgumentException("A categoria selecionada não foi encontrada. Atualize a tela e tente novamente.");

        if (categoria.Tipo != l.Tipo)
            throw new ArgumentException($"A categoria '{categoria.Nome}' é de {categoria.Tipo.ToString().ToLowerInvariant()}, mas o lançamento está marcado como {l.Tipo.ToString().ToLowerInvariant()}. Selecione uma categoria compatível.");
    }

    public void Salvar(Lancamento l)
    {
        ValidarCampos(l);

        using var db = new AppDbContext();
        ValidarRelacionamentos(db, l);
        if (l.Id == 0) db.Lancamentos.Add(l);
        else
        {
            var existente = db.Lancamentos.Find(l.Id);
            if (existente == null)
                throw new ArgumentException("Este lançamento não foi encontrado. Atualize a lista antes de salvar.");

            existente.Descricao = l.Descricao.Trim();
            existente.Valor = l.Valor;
            existente.Data = l.Data;
            existente.Vencimento = l.Vencimento;
            existente.Pago = l.Pago;
            existente.Tipo = l.Tipo;
            existente.CategoriaId = l.CategoriaId;
            existente.ContaId = l.ContaId;
        }
        if (l.Id == 0) l.Descricao = l.Descricao.Trim();
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
        ValidarCampos(modelo);
        if (modo != ModoRepeticao.Parcelado && modo != ModoRepeticao.Recorrente)
            throw new ArgumentException("Escolha se deseja parcelar ou repetir este lançamento mensalmente.");
        if (quantidade < 2 || quantidade > 120)
            throw new ArgumentException("Escolha uma quantidade entre 2 e 120 ocorrências.");

        using var db = new AppDbContext();
        ValidarRelacionamentos(db, modelo);
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
                    Situacao = l.Pago
                        ? "Pago"
                        : l.Vencimento.HasValue && l.Vencimento.Value.Date < DateTime.Today
                            ? "Vencido"
                            : "Pendente",
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
