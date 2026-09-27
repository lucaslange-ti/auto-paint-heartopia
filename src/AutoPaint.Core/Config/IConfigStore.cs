namespace AutoPaint.Core.Config;

/// <summary>
/// Carrega/salva o <see cref="AppConfig"/>. Implementação: JSON em
/// %AppData%\AutoPaintHeartopia\config.json, escrita atômica (.tmp + File.Replace)
/// e debounce. Aplica migração de schema no load.
/// </summary>
public interface IConfigStore
{
    AppConfig Current { get; }

    AppConfig Load();

    /// <summary>Substitui a config em memória (ex.: vinda da UI). Não grava sozinho.</summary>
    void Apply(AppConfig config);

    /// <summary>Agenda um save (debounced). Chamável a cada mudança de UI.</summary>
    void Save();

    /// <summary>Grava imediatamente (ex.: no fechamento do app).</summary>
    void Flush();

    /// <summary>Apaga o arquivo e volta aos defaults ("resetar tudo").</summary>
    void Reset();
}
