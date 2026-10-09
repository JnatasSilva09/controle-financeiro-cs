using ControleFinanceiro.Models;
using ControleFinanceiro.Services;

namespace ControleFinanceiro.Forms;

public class CategoriasForm : Form
{
    private readonly FinanceiroService _svc;
    private readonly ListBox lista = new() { Dock = DockStyle.Fill, DisplayMember = "NomeComTipo" };
    private readonly TextBox txtNome = new() { Width = 180, PlaceholderText = "Nome da categoria" };
    private readonly ComboBox cmbTipo = new() { Width = 100, DropDownStyle = ComboBoxStyle.DropDownList };

    public CategoriasForm(FinanceiroService svc)
    {
        _svc = svc;
        Text = "Categorias";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(440, 400);
        Font = new Font("Segoe UI", 9.5f);

        cmbTipo.Items.AddRange(new object[] { "Despesa", "Receita" });
        cmbTipo.SelectedIndex = 0;

        var btnAdd = new Button { Text = "Adicionar", AutoSize = true };
        var btnDel = new Button { Text = "Excluir selecionada", AutoSize = true };
        btnAdd.Click += (_, _) => Adicionar();
        btnDel.Click += (_, _) => Excluir();

        var topo = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 48, Padding = new Padding(8) };
        topo.Controls.AddRange(new Control[] { txtNome, cmbTipo, btnAdd });

        var rodape = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 46, Padding = new Padding(8) };
        rodape.Controls.Add(btnDel);

        Controls.AddRange(new Control[] { lista, rodape, topo });
        Recarregar();
    }

    private void Recarregar() => lista.DataSource = _svc.ListarCategorias();

    private void Adicionar()
    {
        var tipo = cmbTipo.SelectedIndex == 1 ? TipoLancamento.Receita : TipoLancamento.Despesa;
        try
        {
            _svc.AdicionarCategoria(txtNome.Text, tipo);
            txtNome.Clear();
            Recarregar();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Atenção");
        }
    }

    private void Excluir()
    {
        if (lista.SelectedItem is not Categoria c) return;
        var erro = _svc.ExcluirCategoria(c.Id);
        if (erro != null) MessageBox.Show(erro, "Atenção");
        Recarregar();
    }
}
