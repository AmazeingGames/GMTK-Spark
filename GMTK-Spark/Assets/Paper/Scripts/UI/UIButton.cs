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
    [SerializeField] GameManager.GameAction newGameAction;
    [SerializeField] int levelToLoad = -1;

    public enum UIEventTypes { None, UI, GameAction }
    public enum UIInteractionTypes { Enter, Click, Up, Exit }

    public static EventHandler<UIInteractEventArgs> UIInteractEventHandler;

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
