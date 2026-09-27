namespace AutoPaint.Core.Imaging;

/// <summary>Como encaixar a imagem de entrada na proporção da grade.</summary>
public enum FitMode
{
    /// <summary>Imagem inteira cabe, com sobra (letterbox) preenchida de branco.</summary>
    Contain,
    /// <summary>Preenche a grade cortando o excedente.</summary>
    Fill
}

/// <summary>Filtro de reamostragem ao redimensionar para CxR.</summary>
public enum ResampleMode
{
    NearestNeighbor,
    /// <summary>Padrão — alta qualidade, igual ao projeto de referência (LANCZOS).</summary>
    Lanczos
}
