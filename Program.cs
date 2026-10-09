using System.Globalization;
using ControleFinanceiro.Data;
using ControleFinanceiro.Forms;

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

        // Cria o banco (e os dados iniciais) na primeira execução
        using (var db = new AppDbContext())
        {
            db.Database.EnsureCreated();
        }

        Application.Run(new MainForm());
    }
}
