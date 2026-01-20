using UnityEngine;
using System.Collections;

public class PuzzleGrid : MonoBehaviour
{
    public GameObject tilePrefab;       // Prefab for numbered tile
    public float tileSpacing = 1.1f;    // Distance between tiles
    private GameObject[,] tiles = new GameObject[3, 3];
    private Vector2 emptyPos;           // empty tile position
    private PuzzleGameManager puzzleManager;

    void Start()
    {
        puzzleManager = FindObjectOfType<PuzzleGameManager>();
        CreateGrid();
    }

    void CreateGrid()
    {
        int number = 1;
        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                if (number <= 8)
                {
                    Vector3 pos = new Vector3(col * tileSpacing, -row * tileSpacing, 0);
                    GameObject tile = Instantiate(tilePrefab, pos, Quaternion.identity, transform);
                    tile.name = "Tile" + number;

                    // Optional: set number text if prefab has TextMesh
                    var txt = tile.GetComponentInChildren<TextMesh>();
                    if (txt != null) txt.text = number.ToString();

                    tiles[row, col] = tile;
                    number++;
                }
                else
                {
                    tiles[row, col] = null;
                    emptyPos = new Vector2(row, col);
                }
            }
        }
    }
}
