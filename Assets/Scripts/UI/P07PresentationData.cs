using UnityEngine;
using TMPro;
[CreateAssetMenu(menuName="Presentation/P07 Interface Data")]
public sealed class P07PresentationData:ScriptableObject {
 public TMP_FontAsset font;public Sprite startBackground,endingBackground;
 [TextArea] public string credits="";
 public string gameTitle="旧雨";
}
