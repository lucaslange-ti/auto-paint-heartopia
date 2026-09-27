using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using AutoPaint.Core.Config;
using Serilog;
using Velopack;
using Velopack.Sources;

namespace AutoPaint.App.Updates;

/// <summary>
/// Atualização automática via GitHub Releases (Velopack).
///
/// Só age quando o app foi instalado pelo <c>*-Setup.exe</c> gerado pelo
/// <c>vpk</c> (<see cref="UpdateManager.IsInstalled"/>). Em build portátil
/// single-file ou <c>dotnet run</c> vira no-op — nada de rede, nada de erro.
///
/// Respeita <see cref="AppConfig.CheckForUpdates"/> (opt-out na tela de
/// Configurações). Toda falha é engolida e logada: update quebrado nunca
/// pode impedir o app de abrir.
/// </summary>
public sealed class UpdateService
{
    private const string RepoUrl = "https://github.com/lucaslange-ti/auto-paint-heartopia";

    // Repositório é PÚBLICO → não precisa de token aqui (e nunca coloque um:
    // ele iria embutido no .exe e qualquer um extrairia). O limite de 60 req/h
    // por IP para chamadas anônimas do GitHub sobra de folga.
    private const string? AccessToken = null;

    // true = também recebe pré-lançamentos (tag marcada como "pre-release").
    private const bool IncludePrereleases = false;

    private readonly IConfigStore _config;

    public UpdateService(IConfigStore config) => _config = config;

    /// <summary>
    /// Fluxo completo: checa → baixa → pergunta → aplica e reinicia.
    /// Chamado em "fire and forget" no startup.
    /// </summary>
    public async Task CheckAndApplyAsync(CancellationToken ct = default)
    {
        try
        {
            // Dá tempo da janela abrir e do config.json carregar antes de tocar a rede.
            await Task.Delay(TimeSpan.FromSeconds(5), ct).ConfigureAwait(false);

            if (!_config.Current.CheckForUpdates)
            {
                Log.Information("Update: desativado nas configurações");
                return;
            }

            if (RepoUrl.Contains("SEU_USUARIO", StringComparison.Ordinal))
            {
                Log.Warning("Update: RepoUrl não configurado em UpdateService.cs — pulando");
                return;
            }

            var source = new GithubSource(RepoUrl, AccessToken, IncludePrereleases);
            var mgr = new UpdateManager(source);

            if (!mgr.IsInstalled)
            {
                Log.Information("Update: app não instalado via Velopack (build portátil/dev) — pulando");
                return;
            }

            var updateInfo = await mgr.CheckForUpdatesAsync().ConfigureAwait(false);
            if (updateInfo is null)
            {
                Log.Information("Update: já está na versão mais recente ({Version})", mgr.CurrentVersion);
                return;
            }

            var newVersion = updateInfo.TargetFullRelease.Version.ToString();
            Log.Information("Update: baixando versão {Version}", newVersion);
            await mgr.DownloadUpdatesAsync(updateInfo, cancelToken: ct).ConfigureAwait(false);

            if (await UserWantsToRestartAsync(newVersion).ConfigureAwait(false))
            {
                Log.Information("Update: aplicando {Version} e reiniciando", newVersion);
                mgr.ApplyUpdatesAndRestart(updateInfo);
            }
            else
            {
                // O update já está baixado; o Velopack o aplica sozinho no
                // próximo fechamento do app.
                Log.Information("Update: usuário adiou; será aplicado ao fechar");
            }
        }
        catch (OperationCanceledException)
        {
            // App fechando: silêncio.
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Update: falha ao verificar/baixar atualização");
        }
    }

    private static Task<bool> UserWantsToRestartAsync(string version)
    {
        var tcs = new TaskCompletionSource<bool>();

        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var result = MessageBox.Show(
                $"A versão {version} do Auto Paint Heartopia está disponível e já foi baixada.\n\n" +
                "Deseja reiniciar agora para atualizar?",
                "Atualização disponível",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);

            tcs.SetResult(result == MessageBoxResult.Yes);
        });

        return tcs.Task;
    }
}
