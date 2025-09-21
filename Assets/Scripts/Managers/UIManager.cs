using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;  // For hover detection
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Resource UI")]
    public TMP_Text TimeText;
    public TMP_Text FoodText;
    public TMP_Text MoneyText;
    public TMP_Text PowerText;

    [Header("Card UI")]
    public TMP_Text CardTitleText;
    public TMP_Text CardDescriptionText;
    public Transform ChoicesContainer; // parent for buttons
    public Button ChoiceButtonPrefab;   // prefab for clickable choices
    public Image CardArtworkImage;
    
    private ICardManager _cardManager;
    private IResourceRepository _resourceRepo;
    private ICapitalRepository _capitalRepo;
    private UniTaskCompletionSource<CardChoice> _choiceTcs;

    public ICapitalRepository CapitalRepo { get; private set; }
    public IResourceRepository ResourceRepo { get; private set; }

    void Start()
    {
        CardArtworkImage.gameObject.SetActive(false);
    }
    public void Initialize(
        ITurnService turnService,
        IResourceRepository resourceRepo,
        ICardManager cardManager,
        ICapitalRepository capitalRepo)
    {
        _resourceRepo = resourceRepo;
        _capitalRepo = capitalRepo;
        _cardManager = cardManager;

        CapitalRepo = capitalRepo;
        ResourceRepo = resourceRepo;

        turnService.OnTurnStarted += turn => TimeText.text = $"Turn: {turn}";
        turnService.OnTurnEnded += _ =>
        {
            FoodText.text = $"Food: {resourceRepo.GetByType(ResourceType.Food)?.Amount ?? 0}";
            MoneyText.text = $"Money: {resourceRepo.GetByType(ResourceType.Money)?.Amount ?? 0}";
            PowerText.text = $"Power: {resourceRepo.GetByType(ResourceType.Power)?.Amount ?? 0}";
        };
        turnService.OnTurnDelay += _ =>
        {
            FoodText.text = $"Food: {resourceRepo.GetByType(ResourceType.Food)?.Amount ?? 0}";
            MoneyText.text = $"Money: {resourceRepo.GetByType(ResourceType.Money)?.Amount ?? 0}";
            PowerText.text = $"Power: {resourceRepo.GetByType(ResourceType.Power)?.Amount ?? 0}";
        };

        _cardManager.OnCardDrawn += OnCardDrawnHandler;

    }

    private void OnCardDrawnHandler(CardDrawResult result)
    {
        if (result == null || result.Choices.Count == 0)
            return;

        DisplayCard(result.Card, result.Choices);
    }

    public void DisplayCard(CardData card, List<CardChoice> choices)
    {
        // Başlık ve açıklama
        CardTitleText.text = card.Title;
        CardDescriptionText.text = card.Description;

        // Artwork ayarı
        if (CardArtworkImage != null)
        {
            if (card.Artwork != null)
            {
                CardArtworkImage.sprite = card.Artwork;
                CardArtworkImage.gameObject.SetActive(true);
            }
            else
            {
                CardArtworkImage.gameObject.SetActive(false);
            }
        }

        // Eski butonları temizle
        foreach (Transform child in ChoicesContainer)
            Destroy(child.gameObject);

        // Yeni butonları oluştur
        foreach (var choice in choices)
        {
            var btn = Instantiate(ChoiceButtonPrefab, ChoicesContainer);
            btn.GetComponentInChildren<TMP_Text>().text = choice.Label;

            bool isAvailable = choice.IsAvailable(CapitalRepo, ResourceRepo);
            btn.interactable = isAvailable;

            // Renk değişimi
            var buttonImage = btn.GetComponent<Image>();
            buttonImage.color = isAvailable ? Color.white : Color.gray;

            // Tıklama
            btn.onClick.AddListener(() =>
            {
                if (isAvailable)
                    _choiceTcs?.TrySetResult(choice);
            });

            // Hover efekti sadece aktifse
            if (isAvailable)
                AddHoverEffect(btn);
        }
    }

    private void AddHoverEffect(Button btn)
    {
        var trigger = btn.gameObject.AddComponent<EventTrigger>();
        trigger.triggers = new List<EventTrigger.Entry>();

        // Hover enter
        var entryEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        entryEnter.callback.AddListener((_) => OnButtonHoverEnter(btn));
        trigger.triggers.Add(entryEnter);

        // Hover exit
        var entryExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        entryExit.callback.AddListener((_) => OnButtonHoverExit(btn));
        trigger.triggers.Add(entryExit);
    }


    // Change button color when hovered
    private void OnButtonHoverEnter(Button button)
    {
        var buttonImage = button.GetComponent<Image>();
        buttonImage.color = new Color(0.7f, 0.7f, 0.7f); // Light gray when hovered
    }

    // Reset button color when hover exits
    private void OnButtonHoverExit(Button button)
    {
        var buttonImage = button.GetComponent<Image>();
        buttonImage.color = Color.white; // Reset to default color when hover exits
    }

    // Handle button click (for visual feedback)
    private void OnButtonClick(Button button)
    {
        // Optional: Play a sound or animation when the button is clicked
        Debug.Log("Button clicked: " + button.GetComponentInChildren<Text>().text);
    }

    // Await player's click
    public UniTask<CardChoice> WaitForChoiceAsync()
    {
        _choiceTcs = new UniTaskCompletionSource<CardChoice>();
        return _choiceTcs.Task;
    }

    // Clear card UI
    public void ClearCardUI()
    {
        CardTitleText.text = "";
        CardDescriptionText.text = "";

        if (CardArtworkImage != null)
            CardArtworkImage.gameObject.SetActive(false);

        foreach (Transform child in ChoicesContainer)
            Destroy(child.gameObject);
    }
}
