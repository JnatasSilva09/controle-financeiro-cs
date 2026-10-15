using System.Globalization;
using ControleFinanceiro.Data;
using ControleFinanceiro.Forms;
using ControleFinanceiro.Services;

namespace ControleFinanceiro;

static class Program
{
    [STAThread]
    static void Main()
    {
        var cultura = new CultureInfo("pt-BR");
        CultureInfo.DefaultThreadCurrentCulture = cultura;
        CultureInfo.DefaultThreadCurrentUICulture = cultura;
        CultureInfo.CurrentCulture = cultura;
        CultureInfo.CurrentUICulture = cultura;

        ApplicationConfiguration.Initialize();

        // Cria o banco (primeira vez) ou atualiza a estrutura sem perder dados
        DbInitializer.Inicializar();

        // Backup diário automático (guarda os 7 mais recentes).
        // Falha aqui não deve impedir o app de abrir.
        try { new FinanceiroService().BackupAutomatico(); } catch { }

        Application.Run(new MainForm());
    }
}
