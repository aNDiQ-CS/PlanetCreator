using Planets;
using UnityEngine;

/// <summary>
/// Хранилище параметров создаваемой планеты.
/// Висит на сцене, все мини-игры записывают сюда результаты.
/// </summary>
public class PlanetBuildData : MonoBehaviour
{
    public static PlanetBuildData Instance { get; private set; }

    [Header("Текущие параметры планеты")]
    public Size size = Size.Small;
    public Mass mass = Mass.Light;
    public SatellitesOrRings satellites = SatellitesOrRings.None;
    public Remoteness remoteness = Remoteness.Near;
    public Migration migration = Migration.No;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void ResetAll()
    {
        size = Size.Small;
        mass = Mass.Light;
        satellites = SatellitesOrRings.None;
        remoteness = Remoteness.Near;
        migration = Migration.No;
    }
}