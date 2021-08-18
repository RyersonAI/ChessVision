using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System ;

public class Manager : MonoBehaviour
{

    // Enable the Gizmos in both the scene and game view
    
    private const float tileSize = 1.0f;
    private const float tileOffset = 0.5f;

    //private int selectionX = -1;
    //private int selectionY = -1;

    public List<GameObject> chesspiecePrefabs;
    public GameObject boardPrefab;
    public static List<GameObject> activePieces ;
    public static List<string> activePiecePositions;
    public GameObject board; 


    // Spawn the board and the pieces at the start of the Game


    // Start is called before the first frame update
    void Start()
    {

        activePieces = new List<GameObject>() ;
        activePiecePositions = new List<string>() ; 

        SpawnBoard();       
        runGames();
        Application.Quit();

    }

    private void Update()
    {
        DrawChessBoard();
        // UpdateSelection();
    }

    private void runGames()
    {

        // Loads generated games from csv file and uses them to generate training data for the vision system 

        TextAsset gameData = (TextAsset)Resources.Load("Game_Examples");
        string gameDataText = gameData.text;
        string[] games = gameDataText.Split('\n');

        for (int i = 0; i < games.Length ; i++)
        { 
            ResetBoard();              

            string[] currentGame = games[i].Split(',');

            bool whiteKingMoved = false;
            bool blackKingMoved = false;

            for (int j = 0; j < currentGame.Length ; j++) 
            {

                try {
                string sq1 = currentGame[j].Substring(0, 2);
                string sq2 = currentGame[j].Substring(2, 2);

                if (currentGame[j].Length == 5) // 5 characters, means it is promotion
                {
                    bool isWhite = (j % 2 == 1);
                    MovePiece(sq1, sq2, currentGame[j][4], isWhite);
                }
                else if (!whiteKingMoved && sq1 == "e1") // White castling
                {
                    if (sq2 == "g1")
                        MovePiece("h1", "f1");
                    else
                        MovePiece("a1", "d1");

                    MovePiece(sq1, sq2);
                    whiteKingMoved = true;
                }
                else if (!blackKingMoved && sq1 == "e8") // Black castling
                {
                    if (sq2 == "g8")
                        MovePiece("h8", "f8");
                    else
                        MovePiece("a8", "d8");

                    MovePiece(sq1, sq2);
                    blackKingMoved = true;
                }
                else
                    MovePiece(sq1, sq2);




                // Generate modelInput 
                string screenshotPath = "GeneratedVisionData/" + i + "_" + j + ".png";
                if (!File.Exists(screenshotPath))
                {
                    DataGeneratorScript.generateModelInput(screenshotPath);
                }
             
                // Generate modelOutput 
                string csvPath = "GeneratedVisionData/" + i + "_" + j + ".csv";
                
                if (!File.Exists(csvPath)) { 
                DataGeneratorScript.generateModelOutput(csvPath, activePieces);
                }

            } 
            catch (ArgumentOutOfRangeException e)
            {
                Debug.Log(e.ToString());
                break;
            }
            }

        }
    }

    public void ResetBoard()
    {
        Debug.Log("Resetting board");
        
        // Destroy all active pieces if any 

        if (activePieces.Count > 0)
        {

            foreach (GameObject piece in activePieces)
            { 

                Debug.Log("Destroying " + piece.name); 
                Destroy(piece);

            }

        }

        activePieces.Clear();
        activePiecePositions.Clear();
        SpawnAllChesspieces();

    }

    // Function to move the in-game pieces
    public void MovePiece(string sq1, string sq2, char promotionPiece = 'z', bool isWhite = true)
    {
        int sq2Index = activePiecePositions.IndexOf(sq2);

        if (sq2Index != -1) //If there is a piece in sq2, we need to delete it
        {
            GameObject sq2Piece = activePieces[sq2Index];
            Destroy(sq2Piece);

            activePieces.RemoveAt(sq2Index);
            activePiecePositions.RemoveAt(sq2Index);
        }

        int sq1Index = activePiecePositions.IndexOf(sq1);
        GameObject sq1Piece = activePieces[sq1Index]; //Find which chess piece is on sq1

        if (promotionPiece == 'z')
        {
            UnityEngine.Vector3 newPosition = GetTileCenter(sq2);
            sq1Piece.transform.position = newPosition; //Move the sq1 chess piece to sq2

            activePiecePositions[sq1Index] = sq2; //Update chesspiece position list, with new position at sq2
        }
        else 
        {
            Destroy(sq1Piece); //Don't need the pawn gameobject anymore, SO DELETE IT
            activePieces.RemoveAt(sq1Index);
            activePiecePositions.RemoveAt(sq1Index); //Spawning chesspiece below will add values to list, so we must delete old values

            if (isWhite) 
            {
                if (promotionPiece == 'q')
                    SpawnChesspiece(1, sq2, "white queen");
                else if (promotionPiece == 'r')
                    SpawnChesspiece(2, sq2, "white rook");
                else if (promotionPiece == 'b')
                    SpawnChesspiece(3, sq2, "white bishop");
                else if (promotionPiece == 'n')
                    SpawnChesspiece(4, sq2, "white knight");
            }
            else
            {
                if (promotionPiece == 'q')
                    SpawnChesspiece(7, sq2, "black queen");
                else if (promotionPiece == 'r')
                    SpawnChesspiece(8, sq2, "black rook");
                else if (promotionPiece == 'b')
                    SpawnChesspiece(9, sq2, "black bishop");
                else if (promotionPiece == 'n')
                    SpawnChesspiece(10, sq2, "black knight");
            }
        }
    }

    

    private void SpawnChesspiece(int index, string square, string piece_class)
    {
        UnityEngine.Vector3 position = GetTileCenter(square);

        GameObject piece = Instantiate(chesspiecePrefabs[index],position,chesspiecePrefabs[index].transform.rotation) as GameObject;
        piece.transform.SetParent(transform);
        piece.name = piece_class ; 

        activePieces.Add(piece);
        activePiecePositions.Add(square);
    }

    public void SpawnBoard()
    {
        board = Instantiate(boardPrefab,boardPrefab.transform.position,boardPrefab.transform.rotation) as GameObject;
    }

    private void SpawnAllChesspieces()
    {
        //White pieces

        SpawnChesspiece(0,"e1", "white king"); //King
        SpawnChesspiece(1, "d1", "white queen"); //Queen

        SpawnChesspiece(2, "a1", "white rook"); //Rook1
        SpawnChesspiece(2, "h1", "white rook"); //Rook2

        SpawnChesspiece(3, "c1", "white bishop"); //Bishop1
        SpawnChesspiece(3, "f1", "white bishop"); //Bishop2

        SpawnChesspiece(4, "b1", "white knight"); //Knight1                                 
        SpawnChesspiece(4, "g1", "white knight"); //Knight2

        //Pawns

        SpawnChesspiece(5, "a2", "white pawn");
        SpawnChesspiece(5, "b2", "white pawn");
        SpawnChesspiece(5, "c2", "white pawn");
        SpawnChesspiece(5, "d2", "white pawn");
        SpawnChesspiece(5, "e2", "white pawn");
        SpawnChesspiece(5, "f2", "white pawn");
        SpawnChesspiece(5, "g2", "white pawn");
        SpawnChesspiece(5, "h2", "white pawn");

        //Black pieces

        SpawnChesspiece(6, "d8", "black king"); //King
        SpawnChesspiece(7, "e8", "black queen"); //Queen

        SpawnChesspiece(8, "a8", "black rook"); //Rook1                           
        SpawnChesspiece(8, "h8", "black rook"); //Rook2

        SpawnChesspiece(9, "c8", "black bishop"); //Bishop1
        SpawnChesspiece(9, "f8", "black bishop"); //Bishop2

        SpawnChesspiece(10, "g8", "black knight"); //Knight2
        SpawnChesspiece(10, "b8", "black knight"); //Knight1     
        
        //Pawns

        SpawnChesspiece(11, "a7", "black pawn");
        SpawnChesspiece(11, "b7", "black pawn");
        SpawnChesspiece(11, "c7", "black pawn");
        SpawnChesspiece(11, "d7", "black pawn");
        SpawnChesspiece(11, "e7", "black pawn");
        SpawnChesspiece(11, "f7", "black pawn");
        SpawnChesspiece(11, "g7", "black pawn");
        SpawnChesspiece(11, "h7", "black pawn");
    }

    private Vector3 GetTileCenter(string input)
    {
        int x = char.ToUpper(input[0]) - 65;
        int z = int.Parse(input[1].ToString()) - 1;

        Vector3 origin = Vector3.zero;
        origin.x += (tileSize * x) + tileOffset;
        origin.z += (tileSize * z) + tileOffset;

        return origin;  
    }

    /*This function draws the layout of the chessboard I have adjusted the full board and the pieces 
    to this layout it is the main function that will help you for the movement
    Enable Gismos in the game scene to view this and disable the chess board.Its like a frame work
    */
    private void DrawChessBoard()
    {
        Vector3 widthLine = Vector3.right * 8;
        Vector3 heightLine = Vector3.forward * 8;

        for(int i = 0; i<= 8 ; i++)
        {
            Vector3 start = Vector3.forward * i;
            UnityEngine.Debug.DrawLine(start, start + widthLine);

            for(int j = 0; j<= 8 ; j++)
            {
                Vector3 start1 = Vector3.right * j;
                UnityEngine.Debug.DrawLine(start1, start1 + heightLine);
            }
        }
    }
}
