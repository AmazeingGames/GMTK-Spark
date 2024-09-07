using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class CheatsManager : MonoBehaviour
{
    [SerializeField] List<CheatCode> cheats = new();
    public enum CheatCommands { None, AutoSnap }

    readonly StringBuilder playerInput = new();
    string command;

    public static EventHandler<CheatEventArgs> CheatEventHandler;
    public class CheatEventArgs : EventArgs
    {
        public readonly GameManager.GameAction gameAction = GameManager.GameAction.None;
        public readonly CheatCommands cheatCommand = CheatCommands.None;

        public CheatEventArgs(GameManager.GameAction gameAction, CheatCommands gheatCommand)
        {
            this.gameAction = gameAction;
            this.cheatCommand = gheatCommand;
        }
    }

    // Update is called once per frame
    void Update()
    {
#if DEBUG
        // Checks if a player types in a code, which corresponds to a cheat or game command
        playerInput.Append(Input.inputString);
        if (Input.GetKeyDown(KeyCode.Return))
        {
            playerInput.Remove(playerInput.Length - 1, 1);
            command = playerInput.ToString();   
            playerInput.Clear();

            foreach (var cheat in cheats)
            {
                if (cheat.Code.Equals(command, StringComparison.OrdinalIgnoreCase))
                    OnCheat(cheat.GameActionToPerform, cheat.CheatCommand);
            }
        }
#endif
    }

    /// <summary>
    ///     Notifies systems we performed a cheat.
    /// </summary>
    /// <param name="gameAction"></param>
    /// <param name="cheatCommand"></param>
    void OnCheat(GameManager.GameAction gameAction = GameManager.GameAction.None, CheatCommands cheatCommand = CheatCommands.None)
        => CheatEventHandler?.Invoke(this, new(gameAction, cheatCommand));

    [Serializable]
    public class CheatCode
    {
        [field: SerializeField] public string Code { get; private set; }

        [field: SerializeField] public GameManager.GameAction GameActionToPerform { get; private set; } = GameManager.GameAction.None;
        [field: SerializeField] public CheatsManager.CheatCommands CheatCommand { get; private set; } = CheatCommands.None;
    }
}
