using UnityEngine;

public class PuzzleGameManager : MonoBehaviour
{
    [Header("HUD & Stats")]
    public float elapsedTime = 0f;   // seconds since start
    public int moves = 0;            // number of tile moves
    public bool gameStarted = false; // becomes true after Start button

    void Update()
    {
        if (gameStarted)
        {
            elapsedTime += Time.deltaTime;
        }
    }

    public void TileMoved()
    {
        moves++;
    }
}
