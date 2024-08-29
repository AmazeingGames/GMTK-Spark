using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

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
    [SerializeField] GameManager.GameState newGameState;
    [SerializeField] int levelToLoad = -1;

    public enum UIEventTypes { None, UI, GameState }
    public enum UIInteractionTypes { Enter, Click, Up, Exit }

    public static EventHandler<UIInteractEventArgs> UIInteractEventHandler;

    public class UIInteractEventArgs : EventArgs
    {
        public readonly UIEventTypes buttonEvent;
        public readonly UIInteractionTypes buttonInteraction;
        public readonly PointerEventData pointerEventData;

        public readonly MenuManager.MenuTypes menuToOpen = MenuManager.MenuTypes.None;

        public readonly GameManager.GameState newGameState = GameManager.GameState.None;
        public readonly int levelToLoad = -1;

        public UIInteractEventArgs(UIButton button, UIEventTypes uiEventType, PointerEventData pointerEventData, UIInteractionTypes uiInteractionType)
        {
            this.buttonEvent = uiEventType;
            this.pointerEventData = pointerEventData;
            this.buttonInteraction = uiInteractionType;

            switch (uiEventType)
            {
                case UIEventTypes.UI:
                    menuToOpen = button.menuToOpen;
                break;
                
                case UIEventTypes.GameState:
                    newGameState = button.newGameState;
                    levelToLoad = button.levelToLoad;
                break;
            }
        }
    }

    public void OnPointerClick(PointerEventData pointerEventData)
        => OnUIInteract(pointerEventData, UIInteractionTypes.Click);

    public void OnPointerEnter(PointerEventData pointerEventData)
        => OnUIInteract(pointerEventData, UIInteractionTypes.Enter);

    public void OnPointerExit(PointerEventData pointerEventData)
        => OnUIInteract(pointerEventData, UIInteractionTypes.Exit);

    public void OnPointerUp(PointerEventData pointerEventData)
        => OnUIInteract(pointerEventData, UIInteractionTypes.Up);

    public virtual void OnUIInteract(PointerEventData pointerEventData, UIInteractionTypes buttonInteract)
        => UIInteractEventHandler?.Invoke(this, new(this, buttonEvent, pointerEventData, buttonInteract));
}
