using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Managers
{
    public class CapitalUIManager : MonoBehaviour
    {
        [Header("Bars")]
        [SerializeField] private Slider governmentBar;
        [SerializeField] private Slider populationBar;
        [SerializeField] private Slider militaryBar;

        [Header("Texts (Optional)")]
        [SerializeField] private TMP_Text governmentText;
        [SerializeField] private TMP_Text populationText;
        [SerializeField] private TMP_Text militaryText;

        private ICapitalRepository _capitalRepo;

        public void Initialize(ICapitalRepository repo)
        {
            _capitalRepo = repo;

            // Başlangıç değerlerini yükle
            UpdateBars();
        }

        private void Update()
        {
            // Her frame güncelle (performans derdin yoksa böyle basit olur)
            UpdateBars();
        }

        private void UpdateBars()
        {
            var gov = _capitalRepo.GetByName("Government");
            var pop = _capitalRepo.GetByName("Population");
            var mil = _capitalRepo.GetByName("Military");

            if (governmentBar && gov != null)
                governmentBar.value = gov.Health / 100f;

            if (populationBar && pop != null)
                populationBar.value = pop.Health / 100f;

            if (militaryBar && mil != null)
                militaryBar.value = mil.Health / 100f;

            if (governmentText) governmentText.text = $"Gov: {gov.Health:0}";
            if (populationText) populationText.text = $"Pop: {pop.Health:0}";
            if (militaryText) militaryText.text = $"Mil: {mil.Health:0}";
        }
    }
}