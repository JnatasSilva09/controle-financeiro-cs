namespace ControleFinanceiro.Models;

public enum TipoLancamento
{
    Receita = 1,
    Despesa = 2
}

public class Conta
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
    public decimal SaldoInicial { get; set; }

    public override string ToString() => Nome;
}

public class Categoria
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
    public TipoLancamento Tipo { get; set; }

    public string NomeComTipo => $"{Nome} ({Tipo})";

    public override string ToString() => Nome;
}

public class Lancamento
{
    public int Id { get; set; }
    public string Descricao { get; set; } = "";
    public decimal Valor { get; set; }              // sempre positivo
    public DateTime Data { get; set; }
    public DateTime? Vencimento { get; set; }
    public bool Pago { get; set; } = true;
    public TipoLancamento Tipo { get; set; }

    public int CategoriaId { get; set; }
    public Categoria Categoria { get; set; } = null!;

    public int ContaId { get; set; }
    public Conta Conta { get; set; } = null!;
}
