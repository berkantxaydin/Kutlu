using UnityEngine;
using UnityEngine.UI;

namespace Extras
{
    public class CapitalUIManager : MonoBehaviour
    {
        [Header("Bars")]
        [SerializeField] private Slider governmentBar;
        [SerializeField] private Slider populationBar;
        [SerializeField] private Slider militaryBar;

        private CapitalRepository _repository;
        private CapitalBase _government;
        private CapitalBase _population;
        private CapitalBase _military;

        private void Awake()
        {
            // Create repository and get capitals
            _repository = new CapitalRepository();
            _government = _repository.GetByName("Government");
            _population = _repository.GetByName("Population");
            _military = _repository.GetByName("Military");

            // Initialize sliders (0 - 100 range for health)
            governmentBar.minValue = 0;
            governmentBar.maxValue = 100;
            populationBar.minValue = 0;
            populationBar.maxValue = 100;
            militaryBar.minValue = 0;
            militaryBar.maxValue = 100;
        }

        private void Update()
        {
            // Update slider values according to health
            governmentBar.value = _government.Health;
            populationBar.value = _population.Health;
            militaryBar.value = _military.Health;
        }
    }
}
