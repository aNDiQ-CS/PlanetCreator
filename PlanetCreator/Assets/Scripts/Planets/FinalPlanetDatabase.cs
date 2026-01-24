using UnityEngine;

namespace Planets
{
    [CreateAssetMenu(menuName = "Planet Database")]
    public class FinalPlanetDatabase : ScriptableObject
    {
        [SerializeField] private FinalPlanet[] m_planets;
    }
}
