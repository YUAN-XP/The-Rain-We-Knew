using UnityEngine;

// Rebuilds only when authored data changes; photographs toggle the cached roots.
public static class MazeLayoutGeometry
{
    public static void Rebuild(Scene03MazeController maze)
    {
        var source = maze.visuals[0].GetComponentInChildren<SpriteRenderer>(true);
        Sprite sprite = source.sprite; Material material = source.sharedMaterial;
        for (int layer = 0; layer < 2; layer++)
        {
            var volume=maze.visuals[layer].GetComponent<MazeWallPresentation>();
            if(volume){
                Clear(maze.collisions[layer].transform);int n=0;
                foreach(Rect rect in maze.layouts[layer].WallRects(maze.cellSize,maze.wallThickness)){
                    var wall=Node("WallEdge_"+(n++).ToString("D3"),maze.collisions[layer].transform,rect.center);
                    wall.gameObject.AddComponent<BoxCollider2D>().size=rect.size;
                }
                volume.layout=maze.layouts[layer];volume.cellSize=maze.cellSize;volume.wallThickness=maze.wallThickness;volume.Rebuild();continue;
            }
            Clear(maze.visuals[layer].transform); Clear(maze.collisions[layer].transform);
            int index = 0;
            foreach (Rect rect in maze.layouts[layer].WallRects(maze.cellSize, maze.wallThickness))
            {
                string name = "WallEdge_" + (index++).ToString("D3");
                var collision = Node(name, maze.collisions[layer].transform, rect.center);
                collision.gameObject.AddComponent<BoxCollider2D>().size = rect.size;
                Shape(name + "_Shadow", maze.visuals[layer].transform, rect.center + new Vector2(.1f,-.12f), rect.size + Vector2.one * .1f, new Color(.26f,.29f,.22f,.24f), -3, sprite, material);
                Shape(name, maze.visuals[layer].transform, rect.center, rect.size + Vector2.one * .08f, layer == 0 ? new Color(.53f,.49f,.39f) : new Color(.49f,.53f,.42f), 0, sprite, material);
                Shape(name + "_Cap", maze.visuals[layer].transform, rect.center + new Vector2(-.025f,.04f), rect.size * .65f, layer == 0 ? new Color(.68f,.64f,.51f) : new Color(.62f,.66f,.53f), 1, sprite, material);
            }
        }
    }
    static Transform Node(string name, Transform parent, Vector2 position)
    { var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=position;return go.transform; }
    static void Shape(string name,Transform parent,Vector2 position,Vector2 size,Color color,int order,Sprite sprite,Material material)
    { var t=Node(name,parent,position);t.localScale=new Vector3(size.x,size.y,1);var r=t.gameObject.AddComponent<SpriteRenderer>();r.sprite=sprite;r.sharedMaterial=material;r.color=color;r.sortingOrder=order; }
    static void Clear(Transform parent)
    {
        for(int i=parent.childCount-1;i>=0;i--)
        {
            var child=parent.GetChild(i).gameObject;
            child.SetActive(false);
            if(Application.isPlaying)Object.Destroy(child);else Object.DestroyImmediate(child);
        }
    }
}
