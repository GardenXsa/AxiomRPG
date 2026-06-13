using AxiomRPG.ECS;
using AxiomRPG.Components;

namespace AxiomRPG.Simulation.Systems;

public class WeatherSystem : ISystem
{
    public int Priority => 5;

    private readonly EntityManager _entityManager;
    private readonly GameTime _gameTime;
    private float _weatherTimer;
    private const float WeatherUpdateInterval = 60f; // seconds
    private readonly Random _random = new();

    public WeatherSystem(EntityManager entityManager, GameTime gameTime)
    {
        _entityManager = entityManager;
        _gameTime = gameTime;
    }

    public void Initialize() { }

    public void Update(float deltaTime)
    {
        _weatherTimer += deltaTime;
        if (_weatherTimer < WeatherUpdateInterval) return;
        _weatherTimer = 0f;

        // Update weather for all regions with weather components
        var weatherEntities = _entityManager.QueryEntitiesWith<WeatherComponent>();
        foreach (var entity in weatherEntities)
        {
            var weather = _entityManager.GetComponent<WeatherComponent>(entity.Id);
            if (weather == null) continue;

            // Cycle weather based on time of day and random chance
            if (_random.Next(100) < 10) // 10% chance to change weather
            {
                var newWeather = _gameTime.IsDaytime
                    ? new[] { "clear", "clear", "clear", "cloudy", "rain" }
                    : new[] { "clear", "clear", "cloudy", "fog", "rain" };
                weather = weather with { WeatherType = newWeather[_random.Next(newWeather.Length)] };
                _entityManager.AddComponent(entity.Id, weather);
            }
        }
    }
}
