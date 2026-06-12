namespace AxiomRPG.Core.Types;

public readonly record struct AsciiTile(
    char Character,
    string ForegroundColor,
    string BackgroundColor = "#000000",
    bool IsBlocking = false,
    bool IsTransparent = true
);
