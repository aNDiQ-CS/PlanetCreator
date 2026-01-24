using UnityEngine;

namespace Planets
{
    [CreateAssetMenu(menuName = "Planet")]
    public class FinalPlanet : ScriptableObject
    {
        [SerializeField] private ChemicalType[] m_chemical = new ChemicalType[3];
        [SerializeField] private Remoteness m_remoteness;
        [SerializeField] private Mass m_mass;
        [SerializeField] private Size m_size;
        [SerializeField] private SatellitesOrRings m_additional;
        [SerializeField] private Migration m_migration;
    }
}
