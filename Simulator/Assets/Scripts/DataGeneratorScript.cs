using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DataGeneratorScript : MonoBehaviour
{
    private static DataGeneratorScript instance;

    private Camera camera;
    public float padding = 10f;
    private void Awake()
    {
        instance = this;

        camera = gameObject.GetComponent<Camera>();
        camera.targetTexture = RenderTexture.GetTemporary(416, 416, 0);
    }

    public static void generateModelInput(string screenshotFilename)
    {
        // Generates and saves to disk the model input which in this case is just an image of the board
        instance.TakeScreenshot(screenshotFilename);
    }

    public static void generateModelOutput(string csvPath, List<GameObject> activePieces)
    {
        // Generates and saves to disk a csv that contains all the data for the bounding boxes of every active piece 
        string delimiter = ",";
        string columnText = "region_shape_attributes" + delimiter + "region_attributes\n";

        System.IO.File.WriteAllText(csvPath, columnText);

        foreach (GameObject piece in activePieces)
        {
            string pieceCSV = instance.generatePieceCSV(piece);
            System.IO.File.AppendAllText(csvPath, pieceCSV); 

        }

    }
    private void TakeScreenshot(string screenshotPath) 
    {
        RenderTexture activeRenderTexture = RenderTexture.active;
        RenderTexture.active = camera.targetTexture;

        camera.Render();

        Texture2D image = new Texture2D(camera.targetTexture.width, camera.targetTexture.height);
        Rect rect = new Rect(0, 0, camera.targetTexture.width, camera.targetTexture.height);
        image.ReadPixels(rect, 0, 0);
      
        image.Apply();

        RenderTexture.active = activeRenderTexture;

        byte[] byteArray = image.EncodeToPNG();

        Destroy(image);
        
        System.IO.File.WriteAllBytes(screenshotPath, byteArray);
    }

    private string generatePieceCSV(GameObject piece)
    {

        List<float> pieceData = generatePieceData(piece);

        string colOne = "{\"\"name\"\":\"\"rect\"\",\"\"x\"\":" + (int) pieceData[0] + ",\"\"y\"\":" + (int) pieceData[1] + ",\"\"width\"\":" + (int) pieceData[2] + ",\"\"height\"\":" + (int) pieceData[3] + "}";
        string colTwo = "{\"\"Class\"\":\"\"" + piece.name + "\"\"}";
        string pieceCSV = "\"" + colOne + "\"" + "," + "\"" + colTwo + "\"\n";

        return pieceCSV;

    }


    private List<float> generatePieceData(GameObject piece)
    {

        List<float> pieceData = new List<float>(); 
        Bounds bounds = piece.GetComponentInChildren<Renderer>().bounds;

        // Map all 8 viewable corners into SCREEN SPACE BOUNDS
        Vector3[] SSCorners = new Vector3[8];
        SSCorners[0] = camera.WorldToScreenPoint(new Vector3(bounds.center.x + bounds.extents.x, bounds.center.y + bounds.extents.y, bounds.center.z + bounds.extents.z));
        SSCorners[1] = camera.WorldToScreenPoint(new Vector3(bounds.center.x + bounds.extents.x, bounds.center.y + bounds.extents.y, bounds.center.z - bounds.extents.z));
        SSCorners[2] = camera.WorldToScreenPoint(new Vector3(bounds.center.x + bounds.extents.x, bounds.center.y - bounds.extents.y, bounds.center.z + bounds.extents.z));
        SSCorners[3] = camera.WorldToScreenPoint(new Vector3(bounds.center.x + bounds.extents.x, bounds.center.y - bounds.extents.y, bounds.center.z - bounds.extents.z));
        SSCorners[4] = camera.WorldToScreenPoint(new Vector3(bounds.center.x - bounds.extents.x, bounds.center.y + bounds.extents.y, bounds.center.z + bounds.extents.z));
        SSCorners[5] = camera.WorldToScreenPoint(new Vector3(bounds.center.x - bounds.extents.x, bounds.center.y + bounds.extents.y, bounds.center.z - bounds.extents.z));
        SSCorners[6] = camera.WorldToScreenPoint(new Vector3(bounds.center.x - bounds.extents.x, bounds.center.y - bounds.extents.y, bounds.center.z + bounds.extents.z));
        SSCorners[7] = camera.WorldToScreenPoint(new Vector3(bounds.center.x - bounds.extents.x, bounds.center.y - bounds.extents.y, bounds.center.z - bounds.extents.z));

        // Save 4 min/max X and Y for screen space bounds (there is no Z axis in 2D)
        float min_x = SSCorners[0].x;
        float min_y = SSCorners[0].y;
        float max_x = SSCorners[0].x;
        float max_y = SSCorners[0].y;

        for (int i = 1; i < 8; i++)
        {
            if (SSCorners[i].x < min_x) min_x = SSCorners[i].x;
            if (SSCorners[i].y < min_y) min_y = SSCorners[i].y;
            if (SSCorners[i].x > max_x) max_x = SSCorners[i].x;
            if (SSCorners[i].y > max_y) max_y = SSCorners[i].y;
        }

        // Move and Size the Bounding Box to outline the object, Disable the renderer for this or 
        // dont look at UI elements in the image capture

        //RectTransform rt = GetComponent<RectTransform>();
        //rt.position = new Vector2(min_x - padding, min_y - padding);
        //rt.sizeDelta = new Vector2(max_x - min_x + (padding * 2), max_y - min_y + (padding * 2));
      
        pieceData.Add(min_x - padding);
        pieceData.Add(min_y - padding);
        pieceData.Add(max_x - min_x + (padding * 2));
        pieceData.Add(max_y - min_y + (padding * 2));

        return pieceData;
    }





}
