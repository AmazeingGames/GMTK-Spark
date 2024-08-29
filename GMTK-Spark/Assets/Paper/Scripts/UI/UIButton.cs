using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIButton : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, IPointerUpHandler
{
    [Header("Button Type")]
    [SerializeField] ButtonEventType buttonEvent;

    // Turn this into a class value, which inherits from the same type
    // Change the class based on the button type, and then serialize the class values
    // The script would have to run in the editor for this to work properly
    [Header("UI Button Type")]
    [SerializeField] MenuManager.MenuTypes menuToOpen;

    [Header("Game State Button Type")]
    [SerializeField] GameManager.GameState newGameState;
    [SerializeField] int levelToLoad = -1;

    public enum ButtonEventType { None, UI, GameState }
    public enum ButtonInteractType { Enter, Click, Up, Exit }

    public static EventHandler<UIInteractEventArgs> UIInteractEventHandler;

    public class UIInteractEventArgs : EventArgs
    {
        public readonly ButtonEventType buttonEvent;
        public readonly ButtonInteractType buttonInteraction;
        public readonly PointerEventData pointerEventData;

        public readonly MenuManager.MenuTypes menuToOpen = MenuManager.MenuTypes.None;

        public readonly GameManager.GameState newGameState = GameManager.GameState.None;
        public readonly int levelToLoad = -1;

        public UIInteractEventArgs(UIButton button, ButtonEventType buttonEvent, PointerEventData pointerEventData, ButtonInteractType buttonInteraction)
        {
            this.buttonEvent = buttonEvent;
            this.pointerEventData = pointerEventData;
            this.buttonInteraction = buttonInteraction;

            switch (buttonEvent)
            {
                case ButtonEventType.UI:
                    menuToOpen = button.menuToOpen;
                break;
                
                case ButtonEventType.GameState:
                    newGameState = button.newGameState;
                    levelToLoad = button.levelToLoad;
                break;
            }
        }
    }

    public void OnPointerClick(PointerEventData pointerEventData)
        => OnUIInteract(pointerEventData, ButtonInteractType.Click);

    public void OnPointerEnter(PointerEventData pointerEventData)
        => OnUIInteract(pointerEventData, ButtonInteractType.Enter);

    public void OnPointerExit(PointerEventData pointerEventData)
        => OnUIInteract(pointerEventData, ButtonInteractType.Exit);

    public void OnPointerUp(PointerEventData pointerEventData)
        => OnUIInteract(pointerEventData, ButtonInteractType.Up);

    public virtual void OnUIInteract(PointerEventData pointerEventData, ButtonInteractType buttonInteract)
        => UIInteractEventHandler?.Invoke(this, new(this, buttonEvent, pointerEventData, buttonInteract));
}
