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
        public readonly GameManager.GameState gameStateCommand = GameManager.GameState.None;
        public readonly CheatCommands cheatCommand = CheatCommands.None;

        public CheatEventArgs(GameManager.GameState gameStateCommand, CheatCommands gheatCommand)
        {
            this.gameStateCommand = gameStateCommand;
            this.cheatCommand = gheatCommand;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
#if DEBUG
        playerInput.Append(Input.inputString);

        if (Input.GetKeyDown(KeyCode.Return))
        {
            playerInput.Remove(playerInput.Length - 1, 1);
            command = playerInput.ToString();   
            playerInput.Clear();

            foreach (var cheat in cheats)
            {
                if (cheat.Code.Equals(command, StringComparison.OrdinalIgnoreCase))
                    OnCheat(cheat.GameStateCommand, cheat.CheatCommand);
            }
        }
#endif
    }

    void OnCheat(GameManager.GameState gameStateCommand = GameManager.GameState.None, CheatCommands cheatCommand = CheatCommands.None)
        => CheatEventHandler?.Invoke(this, new(gameStateCommand, cheatCommand));

    [Serializable]
    public class CheatCode
    {
        [field: SerializeField] public string Code { get; private set; }

        [field: SerializeField] public GameManager.GameState GameStateCommand { get; private set; } = GameManager.GameState.None;
        [field: SerializeField] public CheatsManager.CheatCommands CheatCommand { get; private set; } = CheatCommands.None;
    }
}
