using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static MovePaper.PaperActionEventArgs;

public class UIButton : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, IPointerUpHandler
{
    [Header("Button Type")]
    [SerializeField] UIEventTypes buttonEvent;

    // Turn this into a class value, which inherits from the same type
    // Change the class based on the button type, and then serialize the class values
    // The script would have to run in the editor for this to work properly
    [Header("UI Button Type")]
    [SerializeField] MenuManager.MenuTypes menuToOpen;

    [Header("Game State Button Type")]
    [SerializeField] GameManager.GameAction newGameAction;
    [SerializeField] int levelToLoad = -1;

    [Header("Components")]
    [SerializeField] TextMeshProUGUI text;
    [SerializeField] Image underline;

    public enum UIEventTypes { None, UI, GameAction }
    public enum UIInteractionTypes { Enter, Click, Up, Exit }

    public static EventHandler<UIInteractEventArgs> UIInteractEventHandler;

    Coroutine lerpButtonCoroutine = null;
    Coroutine lerpUnderlineCoroutine = null;

    public class UIInteractEventArgs : EventArgs
    {
        public readonly UIEventTypes buttonEvent;
        public readonly UIInteractionTypes buttonInteraction;
        public readonly PointerEventData pointerEventData;

        public readonly MenuManager.MenuTypes menuToOpen = MenuManager.MenuTypes.None;
        public readonly GameManager.GameAction actionToPerform = GameManager.GameAction.None;
        public readonly int levelToLoad = -1;

        public UIInteractEventArgs(UIButton button, UIEventTypes uiEventType, PointerEventData pointerEventData, UIInteractionTypes uiInteractionType)
        {
            this.buttonEvent = uiEventType;
            this.pointerEventData = pointerEventData;
            this.buttonInteraction = uiInteractionType;

            if (uiInteractionType == UIInteractionTypes.Enter || uiInteractionType == UIInteractionTypes.Exit)
                return;

            switch (uiEventType)
            {
                case UIEventTypes.UI:
                    menuToOpen = button.menuToOpen;

                    if (menuToOpen == MenuManager.MenuTypes.Pause)
                        throw new InvalidOperationException("Puasing the game should be done by updating the game state, not through changing UI menus.");
                    else if (menuToOpen == MenuManager.MenuTypes.Empty)
                        throw new InvalidOperationException("Closing all menus should be done by updating the game to the proper game state, not through changing UI menus.");
                    else if (menuToOpen == MenuManager.MenuTypes.None)
                        throw new InvalidOperationException("A menu type of none will cause nothing to happen.");
                break;
                
                case UIEventTypes.GameAction:
                    actionToPerform = button.newGameAction;
                    levelToLoad = button.levelToLoad;

                    if (actionToPerform == GameManager.GameAction.None)
                        throw new InvalidOperationException("A game state of none will cause nothing to happen.");
                break;
            }
        }
    }

    void OnEnable()
    {
        if (MenuManager.Instance != null)
            StartCoroutine(LerpButton(false));
    }

    void OnDisable()
    {
        if (MenuManager.Instance == null)
            return;

        text.transform.localScale = new (MenuManager.Instance.RegularScale, MenuManager.Instance.RegularScale);

        text.alpha = MenuManager.Instance.RegularOpacity;

        underline.fillAmount = 0;
    }

    IEnumerator Start()
    {
        while (MenuManager.Instance == null)
            yield return null;

        text.alpha = MenuManager.Instance.RegularOpacity;
        text.gameObject.SetActive(true);

        var regularScale = MenuManager.Instance.RegularScale;

        var rect = transform as RectTransform;
        text.transform.localScale = new Vector3(regularScale, regularScale, text.transform.localScale.z);
        underline.rectTransform.sizeDelta = new Vector2(rect.sizeDelta.x, underline.rectTransform.sizeDelta.y);
        underline.fillAmount = 0;

        StartCoroutine(SetUnderlineLength());

        // Debug.Log($"Underline: size set to {underline.rectTransform.sizeDelta.x}");
    }

    IEnumerator SetUnderlineLength()
    {
        underline.rectTransform.sizeDelta = new Vector2(0, underline.rectTransform.sizeDelta.y);

        while (true)
        {
            var rect = transform as RectTransform;
            underline.rectTransform.sizeDelta = new Vector2(rect.sizeDelta.x, underline.rectTransform.sizeDelta.y);
            yield return new WaitForSeconds(.1f);
        }
    }

    /// <summary>
    ///     Moves the last held paper to the correct position over time.
    /// </summary>
    IEnumerator LerpButton(bool isSelected)
    {
        float time = 0;
        
        float startingScale = text.transform.localScale.x;
        float targetScale = isSelected ? MenuManager.Instance.HoverScale : MenuManager.Instance.RegularScale;

        float startingOpacity = text.alpha;
        float targetOpacity = isSelected ? MenuManager.Instance.HoverOpacity : MenuManager.Instance.RegularOpacity;

        while (time < 1)
        {
            var lerpCurve = MenuManager.Instance.ButtonLerpCurve;
            
            float newScale = Mathf.Lerp(startingScale, targetScale, lerpCurve.Evaluate(time));
            text.transform.localScale = new Vector3 (newScale, newScale, text.transform.localScale.z);

            float newOpacity = Mathf.Lerp(startingOpacity, targetOpacity, lerpCurve.Evaluate(time));
            text.alpha = newOpacity;

            time += Time.deltaTime * MenuManager.Instance.ButtonLerpSpeed;
            yield return null;
        }
    }

    IEnumerator LerpUnderline(bool isSelected)
    {
        float time = 0;

        float startingFill = underline.fillAmount;
        float targetFill = isSelected ? 1 : 0;

        while (time < 1)
        {
            var lerpCurve = MenuManager.Instance.UnderlineLerpCurve;

            float newFillAmount = Mathf.Lerp(startingFill, targetFill, lerpCurve.Evaluate(time));
            underline.fillAmount = newFillAmount;

            time += Time.deltaTime * MenuManager.Instance.UnderlineLerpSpeed;
            yield return null;
        }
    }


    public void OnPointerClick(PointerEventData pointerEventData)
    {
        OnUIInteract(pointerEventData, UIInteractionTypes.Click);

        OnButtonInteract(false);
    }
    public void OnPointerEnter(PointerEventData pointerEventData)
    {
        OnUIInteract(pointerEventData, UIInteractionTypes.Enter);

        OnButtonInteract(true);
    }
    public void OnPointerExit(PointerEventData pointerEventData)
    {
        OnUIInteract(pointerEventData, UIInteractionTypes.Exit);

        OnButtonInteract(false);
    }

    void OnButtonInteract(bool isSelected)
    {
        if (!gameObject.activeInHierarchy)
            return;

        if (lerpButtonCoroutine != null)
            StopCoroutine(lerpButtonCoroutine);

        lerpButtonCoroutine = StartCoroutine(LerpButton(isSelected));

        if (lerpUnderlineCoroutine != null)
            StopCoroutine(lerpUnderlineCoroutine);

        lerpUnderlineCoroutine = StartCoroutine(LerpUnderline(isSelected));
    }

    public void OnPointerUp(PointerEventData pointerEventData)
        => OnUIInteract(pointerEventData, UIInteractionTypes.Up);

    public virtual void OnUIInteract(PointerEventData pointerEventData, UIInteractionTypes buttonInteract)
        => UIInteractEventHandler?.Invoke(this, new(this, buttonEvent, pointerEventData, buttonInteract));
}
