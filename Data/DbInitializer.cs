using Microsoft.Data.Sqlite;

namespace ControleFinanceiro.Data;

/// <summary>
/// Cria o banco na primeira execução e aplica atualizações de estrutura nas
/// execuções seguintes, SEM apagar dados. Usa o PRAGMA user_version do SQLite
/// para saber em que versão o banco está.
///
/// Para mudar a estrutura no futuro:
///   1) altere o modelo (Models.cs);
///   2) adicione um item em Scripts com a próxima versão e o SQL da mudança,
///      por exemplo: (2, "ALTER TABLE Lancamentos ADD COLUMN GrupoId TEXT NULL;")
///   3) aumente VersaoAtual para o mesmo número.
/// Bancos novos já nascem completos pelo EF Core; bancos antigos recebem os scripts.
/// </summary>
public static class DbInitializer
{
    public const int VersaoAtual = 1;

    private static readonly (int Versao, string Sql)[] Scripts =
        Array.Empty<(int Versao, string Sql)>();

    public static void Inicializar()
    {
        bool jaExiste = Escalar(
            "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='Lancamentos';") > 0;

        if (!jaExiste)
        {
            using (var db = new AppDbContext())
            {
                db.Database.EnsureCreated();
            }
            Executar($"PRAGMA user_version = {VersaoAtual};");
            return;
        }

        long versao = Escalar("PRAGMA user_version;");
        if (versao == 0) versao = 1;   // banco criado antes do versionamento = versão 1

        foreach (var (v, sql) in Scripts.OrderBy(s => s.Versao))
        {
            if (v <= versao) continue;
            Executar(sql + $"\nPRAGMA user_version = {v};");
            versao = v;
        }

        Executar($"PRAGMA user_version = {versao};");
    }

    private static SqliteConnection Abrir()
    {
        var conn = new SqliteConnection($"Data Source={AppDbContext.CaminhoBanco}");
        conn.Open();
        return conn;
    }

    private static long Escalar(string sql)
    {
        using var conn = Abrir();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        return Convert.ToInt64(cmd.ExecuteScalar());
    }

    private static void Executar(string sql)
    {
        using var conn = Abrir();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }
}
