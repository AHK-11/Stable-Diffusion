using System.Collections;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class StableTexture : EditorWindow
{
    //Start is called before the first frame update
    [MenuItem("Addons/StableTexture")]
    public static void ShowWindow()
    {
        var window = GetWindow(typeof(StableTexture));
        window.titleContent = new GUIContent("StableTexture");
    }

    private string prompt = "empty";
    private int iteration = 1;
    private int steps = 25;
    private float cfgscale = 7.5f;
    private int height = 512;
    private int width = 512;

    // Update is called once per frame
    void Update()
    {
        
    }
    }