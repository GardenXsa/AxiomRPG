namespace AxiomRPG.Rendering.Screens;

public interface IScreen
{
    void Initialize();
    void Render(RenderBuffer buffer);
    bool HandleInput(ConsoleKeyInfo key);
}
