namespace AxiomRPG.Simulation.Systems;

public interface ISystem
{
    int Priority { get; }
    void Initialize();
    void Update(float deltaTime);
}
