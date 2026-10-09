using Microsoft.EntityFrameworkCore;
using ControleFinanceiro.Models;

namespace ControleFinanceiro.Data;

public class AppDbContext : DbContext
{
    public DbSet<Conta> Contas => Set<Conta>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Lancamento> Lancamentos => Set<Lancamento>();

    public static string CaminhoBanco
    {
        get
        {
            var pasta = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ControleFinanceiro");
            Directory.CreateDirectory(pasta);
            return Path.Combine(pasta, "financeiro.db");
        }
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite($"Data Source={CaminhoBanco}");

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Lancamento>()
            .HasOne(l => l.Categoria).WithMany()
            .HasForeignKey(l => l.CategoriaId)
            .OnDelete(DeleteBehavior.Restrict);

        mb.Entity<Lancamento>()
            .HasOne(l => l.Conta).WithMany()
            .HasForeignKey(l => l.ContaId)
            .OnDelete(DeleteBehavior.Restrict);

        mb.Entity<Conta>().HasData(
            new Conta { Id = 1, Nome = "Carteira", SaldoInicial = 0m });

        mb.Entity<Categoria>().HasData(
            new Categoria { Id = 1, Nome = "Salário",          Tipo = TipoLancamento.Receita },
            new Categoria { Id = 2, Nome = "Outras receitas",  Tipo = TipoLancamento.Receita },
            new Categoria { Id = 3, Nome = "Alimentação",      Tipo = TipoLancamento.Despesa },
            new Categoria { Id = 4, Nome = "Moradia",          Tipo = TipoLancamento.Despesa },
            new Categoria { Id = 5, Nome = "Transporte",       Tipo = TipoLancamento.Despesa },
            new Categoria { Id = 6, Nome = "Saúde",            Tipo = TipoLancamento.Despesa },
            new Categoria { Id = 7, Nome = "Lazer",            Tipo = TipoLancamento.Despesa },
            new Categoria { Id = 8, Nome = "Outras despesas",  Tipo = TipoLancamento.Despesa });
    }
}
