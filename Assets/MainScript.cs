

using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class MainScript : MonoBehaviour {


  const int
    TYPE_TERRAIN  = 0,
    TYPE_BUILDING = 1,
    TYPE_WALKER   = 2,
    TILE_WIDE = 60,
    TILE_HIGH = 30;


  class Sprite {

    public int type = TYPE_BUILDING;
    public Texture2D[] images;
    public float animProgress;

    public int size;
    public Vector2[] path;
    public float walkProgress;
    public int facing;

    public float x, y;
    public int sx, sy, depth;


    public static Texture2D[] loadImages(string path, params string[] IDs) {
      Texture2D[] images = new Texture2D[IDs.Length];
      for (int i = IDs.Length; i-- > 0;) {
        images[i] = (Texture2D) Resources.Load(path+IDs[i]);
      }
      return images;
    }

    public static Sprite with(int size, string path, params string[] IDs) {
      Sprite s = new Sprite();
      s.size   = size;
      s.images = loadImages(path, IDs);
      return s;
    }

    public static Sprite with(
      int size, string path, int minID, int maxID, string suffix,
      params int[] excepted
    ) {
      List <string> IDs = new List <string> ();
      bool skip = false;
      for (int i = minID; i < maxID; i++) {
        foreach (int e in excepted) if (e == i) { skip = true; break; }
        if (skip) break;
        IDs.Add(i+suffix);
      }
      return with(size, path, IDs.ToArray());
    }
  }


  class Stage {

    List <Sprite> terrain = new List <Sprite> ();
    List <Sprite> sprites = new List <Sprite> ();
    public int viewX = 0, viewY = 0;
    public bool report = false;


    public Sprite addTerrain(Sprite s, int x, int y) {
      s.x = x;
      s.y = y;
      terrain.Add(s);
      return s;
    }

    public Sprite addSprite(Sprite s, int x, int y) {
      s.x = x;
      s.y = y;
      sprites.Add(s);
      return s;
    }


    public void updateWalkers() {
      foreach (Sprite s in sprites) if (s.type == TYPE_WALKER) {

        float pathLen = 0, segProg = -1;
        Vector2 l = s.path[0], n = l;

        for (int i = 1; i < s.path.Length; i++) {
          float dist = Vector2.Distance(s.path[i], s.path[i - 1]);
          pathLen += dist;
          if (pathLen > s.walkProgress && segProg == -1) {
            l = s.path[i - 1];
            n = s.path[i    ];
            if (dist == 0) segProg = 0;
            else segProg = 1 - ((pathLen - s.walkProgress) / dist);
          }
        }
        if (pathLen < 1) pathLen = 1;

        float x = l.x, y = l.y;
        x += (float) ((n.x - l.x) * segProg);
        y += (float) ((n.y - l.y) * segProg);
        s.x = x;
        s.y = y;

        float angle = (float) Mathf.Atan2(n.x - l.x, n.y - l.y);
        angle = (float) angle * Mathf.Rad2Deg * 8f / 360;
        if (angle < 0) angle += 8;
        s.facing = (int) angle;

        s.walkProgress += 0.03f;
        while (s.walkProgress >= pathLen) s.walkProgress -= pathLen;

        s.animProgress += 0.05f;
        while(s.animProgress >= 1) s.animProgress--;
      }
    }


    Vector2 translatePoint(float sx, float sy, int wide, int high) {
      float x = 0, y = 0;

      //  X is going down and right, Y is going up and right:
      sx -= viewX;
      sy -= viewY;
      x += sx * TILE_WIDE / 2f;
      x += sy * TILE_WIDE / 2f;
      y += sy * TILE_HIGH / 2f;
      y -= sx * TILE_HIGH / 2f;
      x += wide / 2f;
      y += high / 2f;

      y = high - (1f + y);

      return new Vector2((int) x, (int) y);
    }


    public void renderToGUI(Rect trans) {
      int x = (int) trans.x, y = (int) trans.y;
      int wide = (int) trans.width, high = (int) trans.height;
      //Vector2 store = new Vector2();

      foreach (Sprite s in terrain) {
        Texture2D img = s.images[0];
        if (! img) continue;
        Vector2 store = translatePoint(s.x, s.y, wide, high);
        store.x -= 0;
        store.y -= 0 + (int) TILE_HIGH / 2;

        Rect size = new Rect(store.x, store.y, img.width, img.height);
        GUI.DrawTexture(size, img, ScaleMode.ScaleToFit, true, 0);
      }

      if (report) print("\nRendering....");

      foreach (Sprite s in sprites) {
        Texture2D img = s.images[0];
        if (! img) continue;
        Vector2 store = translatePoint(s.x, s.y, wide, high);
        int depth = (int) store.y;

        if (report) {
          print("  World position: "+s.x+"|"+s.y);
          print("  Image size:     "+img.width+"|"+img.height);
          print("  Translated to:  "+store);
        }

        int baseW = img.width;
        store.y -= img.height;
        store.y += TILE_HIGH * 0.5f * baseW / TILE_WIDE;

        if (s.type == TYPE_WALKER) {
          store.x += (TILE_WIDE - baseW) / 2;
          store.y += TILE_HIGH * 0.2f * baseW / TILE_WIDE;
        }

        s.sx    = (int) store.x - 0;
        s.sy    = (int) store.y - 0;
        s.depth = depth;
      }

      sprites.Sort((a, b) => a.depth.CompareTo(b.depth));

      foreach (Sprite s in sprites) {
        int numFrames = s.images.Length / 8;
        int index = 8 * (int) (s.animProgress * numFrames);
        index += s.facing;
        Texture2D img = s.images[index];
        if (! img) continue;

        Rect size = new Rect(s.sx, s.sy, img.width, img.height);
        GUI.DrawTexture(size, img, ScaleMode.ScaleToFit, true, 0);

        if (report) print("  Screen position:  "+size);
      }
    }
  }



  Stage stage;


  void Start() {
    stage = new Stage();

    Sprite ground = Sprite.with(
      1, "Land1a_", 61, 119, ""
    );
    Sprite trees = Sprite.with(
      1, "Land1a_", 9, 16, "", 12, 13
    );
    Sprite roads = Sprite.with(
      1, "Land2a_", 43, 62, ""
    );
    int[,] roadIDs = new int[,] {
      { 0, 0, 0, 0, 0, 0, 0, 0, 0 },
      { 0, 5, 1, 1, 1, 6, 0, 0, 0 },
      { 0, 2, 0, 0, 0, 2, 0, 0, 0 },
      { 0, 2, 0, 0, 0, 2, 0, 0, 0 },
      { 0, 2, 0, 0, 0, 2, 0, 0, 0 },
      { 0, 8, 1, 1, 1, 7, 0, 0, 0 },
      { 0, 0, 0, 0, 0, 0, 0, 0, 0 },
      { 0, 0, 0, 0, 0, 0, 0, 0, 0 },
      { 0, 0, 0, 0, 0, 0, 0, 0, 0 },
    };

    for (int x = 9, index; x-- > 0;) for (int y = 9; y-- > 0;) {

      int roadID = roadIDs[x, y];
      if (roadID > 0) {
        Sprite r = new Sprite();
        r.size = 1;
        r.type = TYPE_TERRAIN;
        r.images = new Texture2D[1];
        r.images[0] = roads.images[roadID - 1];
        stage.addTerrain(r, x, y);
        continue;
      }

      Sprite g = new Sprite();
      g.size = 1;
      g.type = TYPE_TERRAIN;
      index = Random.Range(0, ground.images.Length);
      g.images = new Texture2D[1];
      g.images[0] = ground.images[index];
      stage.addTerrain(g, x, y);

      if (x > 0 && x < 8 && y > 0 && y < 8) continue;
      if (Random.Range(0f, 1f) > 0.5f) continue;

      Sprite t = new Sprite();
      t.size = 1;
      t.type = TYPE_TERRAIN;
      index = Random.Range(0, trees.images.Length);
      t.images = new Texture2D[1];
      t.images[0] = trees.images[index];
      stage.addSprite(t, x, y);
    }

    stage.addSprite(Sprite.with(
      2, "Housng1a_", "32"
    ), 1, 6);
    stage.addSprite(Sprite.with(
      2, "Housng1a_", "33"
    ), 3, 6);
    stage.addSprite(Sprite.with(
      2, "Housng1a_", "36"
    ), 6, 1);
    stage.addSprite(Sprite.with(
      3, "Security_", "45"
    ), 2, 2);

    Sprite walker = stage.addSprite(Sprite.with(
      1, "Citizen01_", 0, 96, ""
    ), 1, 1);
    walker.type = TYPE_WALKER;
    walker.path = new Vector2[] {
      new Vector2(1, 1),
      new Vector2(1, 5),
      new Vector2(5, 5),
      new Vector2(5, 1),
      new Vector2(1, 1)
    };

    stage.viewX = 4;
    stage.viewY = 4;
    stage.report = true;
  }


  void OnGUI() {
    RectTransform trans = gameObject.GetComponent <RectTransform> ();
    //print("Transform rect is: "+trans.rect);
    stage.updateWalkers();
    stage.renderToGUI(trans.rect);
    stage.report = false;
  }

}




//
