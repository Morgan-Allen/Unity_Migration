

using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class MainScript : MonoBehaviour {
  
  Texture2D disp;
  
  
  void Start () {
    disp = (Texture2D) Resources.Load("Govt_9");
    print("Texture loaded: "+(disp != null));
  }
  
  
  void Update () {
    //print("Updating.");
  }
  
  
  void OnGUI() {
    if (! disp) return;
    Rect size = new Rect(10, 10, disp.width, disp.height);
    GUI.DrawTexture(size, disp, ScaleMode.ScaleToFit, true, 0);
  }
}









