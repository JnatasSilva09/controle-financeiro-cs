using System.Globalization;
using ControleFinanceiro.Models;
using ControleFinanceiro.Services;

namespace ControleFinanceiro.Forms;

public class ContasForm : Form
{
    private readonly FinanceiroService _svc;
    private readonly ListBox lista = new() { Dock = DockStyle.Fill, DisplayMember = "Nome" };
    private readonly TextBox txtNome = new() { Width = 180, PlaceholderText = "Nome da conta" };
    private readonly TextBox txtSaldo = new() { Width = 110, PlaceholderText = "Saldo inicial" };

    public ContasForm(FinanceiroService svc)
    {
        _svc = svc;
        Text = "Contas";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(440, 340);
        Font = new Font("Segoe UI", 9.5f);

        var btnAdd = new Button { Text = "Adicionar", AutoSize = true };
        var btnDel = new Button { Text = "Excluir selecionada", AutoSize = true };
        btnAdd.Click += (_, _) => Adicionar();
        btnDel.Click += (_, _) => Excluir();

        var topo = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 48, Padding = new Padding(8) };
        topo.Controls.AddRange(new Control[] { txtNome, txtSaldo, btnAdd });

        var rodape = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 46, Padding = new Padding(8) };
        rodape.Controls.Add(btnDel);

        Controls.AddRange(new Control[] { lista, rodape, topo });
        Recarregar();
    }

    private void Recarregar() => lista.DataSource = _svc.ListarContas();

    private void Adicionar()
    {
        decimal saldo = 0;
        var texto = txtSaldo.Text.Replace("R$", "").Trim();
        if (texto.Length > 0 &&
            !decimal.TryParse(texto, NumberStyles.Number, CultureInfo.CurrentCulture, out saldo))
        {
            MessageBox.Show("Saldo inválido. Exemplo: 1500,00", "Atenção");
            return;
        }

        try
        {
            _svc.AdicionarConta(txtNome.Text, saldo);
            txtNome.Clear();
            txtSaldo.Clear();
            Recarregar();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Atenção");
        }
    }

    private void Excluir()
    {
        if (lista.SelectedItem is not Conta c) return;
        var erro = _svc.ExcluirConta(c.Id);
        if (erro != null) MessageBox.Show(erro, "Atenção");
        Recarregar();
    }
}
