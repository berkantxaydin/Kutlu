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

    private ICardManager _cardManager;

    private UniTaskCompletionSource<CardChoice> _choiceTcs;

    public void Initialize(
        ITurnService turnService,
        IResourceRepository resourceRepo,
        ICardManager cardManager)
    {
        _cardManager = cardManager;

        turnService.OnTurnStarted += turn => TimeText.text = $"Turn: {turn}";
        turnService.OnTurnEnded += _ =>
        {
            FoodText.text = $"Food: {resourceRepo.GetByType(ResourceType.Food)?.Amount ?? 0}";
            MoneyText.text = $"Money: {resourceRepo.GetByType(ResourceType.Money)?.Amount ?? 0}";
            PowerText.text = $"Power: {resourceRepo.GetByType(ResourceType.Power)?.Amount ?? 0}";
        };

        _cardManager.OnCardDrawn += DisplayCard;
    }

    public void DisplayCard(CardData card, List<CardChoice> choices)
    {
        // Show card text
        CardTitleText.text = card.Title;
        CardDescriptionText.text = card.Description;

        // Clear old buttons
        foreach (Transform child in ChoicesContainer)
            Destroy(child.gameObject);

        // Create buttons with visual feedback
        foreach (var choice in choices)
        {
            var btn = Instantiate(ChoiceButtonPrefab, ChoicesContainer);
            btn.GetComponentInChildren<TMP_Text>().text = choice.Label;
            btn.interactable = choice.IsAvailable(null, null); // UI only; already filtered in CardManager

            btn.onClick.AddListener(() =>
            {
                _choiceTcs?.TrySetResult(choice);
            });

            // Check if the choice is available
            bool isChoiceAvailable = choice.IsAvailable(null, null);
            btn.interactable = isChoiceAvailable;

            // Visual feedback: Disable unavailable choices
            if (!isChoiceAvailable)
            {
                var buttonImage = btn.GetComponent<Image>();
                buttonImage.color = Color.gray; // Gray out the button if unavailable
            }
            else
            {
                var buttonImage = btn.GetComponent<Image>();
                buttonImage.color = Color.white; // Reset to default color when available
            }

            // Add listener for button click
            btn.onClick.AddListener(() =>
            {
                if (isChoiceAvailable)  // Make sure the choice is available before selecting
                {
                    _choiceTcs?.TrySetResult(choice);
                }
            });

            // Optional: Add hover effects
            var buttonEvent = btn.GetComponent<Button>();
            AddHoverEffect(buttonEvent, btn);
        }
    }

    // Hover effect on buttons
    private void AddHoverEffect(Button button, Button btn)
    {
        var buttonEvent = btn.GetComponent<Button>();

        // Hover events
        buttonEvent.onClick.AddListener(() => OnButtonClick(btn));
        var pointerEnter = buttonEvent.gameObject.AddComponent<EventTrigger>();
        pointerEnter.triggers = new List<EventTrigger.Entry>();

        // On hover enter
        EventTrigger.Entry entryEnter = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerEnter
        };
        entryEnter.callback.AddListener((data) => OnButtonHoverEnter(btn));
        pointerEnter.triggers.Add(entryEnter);

        // On hover exit
        EventTrigger.Entry entryExit = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerExit
        };
        entryExit.callback.AddListener((data) => OnButtonHoverExit(btn));
        pointerEnter.triggers.Add(entryExit);
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
        foreach (Transform child in ChoicesContainer)
            Destroy(child.gameObject);
    }
}
