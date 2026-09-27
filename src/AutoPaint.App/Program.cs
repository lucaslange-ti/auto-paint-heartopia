using System;
using Velopack;

namespace AutoPaint.App;

/// <summary>
/// Ponto de entrada real do processo. Precisa rodar <see cref="VelopackApp"/>
/// ANTES de qualquer coisa do WPF: é ele que intercepta os "hooks" de
/// instalação/atualização/desinstalação (o Setup.exe e o updater chamam o
/// próprio .exe com argumentos especiais) e encerra sem abrir a janela.
///
/// Em build portátil ou <c>dotnet run</c> o <c>Run()</c> não faz nada.
/// </summary>
public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        VelopackApp.Build().Run();

        var app = new App();
        app.InitializeComponent();
        app.Run();
    }
}
