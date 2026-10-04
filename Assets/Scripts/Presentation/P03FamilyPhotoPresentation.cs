using UnityEngine;
/// <summary>Displays the obscured photograph until the existing birthday memory has been learned.</summary>
public sealed class P03FamilyPhotoPresentation:MonoBehaviour
{
 public SpriteRenderer target;
 StoryStateManager story;
 void Start(){story=PersistentRuntime.Instance?PersistentRuntime.Instance.GetComponent<StoryStateManager>():FindAnyObjectByType<StoryStateManager>();if(story)story.Changed+=Apply;Apply();}
 void Apply(){if(target)target.sprite=DemoArtCatalog.Find(story&&story.Has(StoryFlag.Scene03BirthdayKnown)?"FILE_FAMILY_PHOTO_CLEAN":"FILE_FAMILY_PHOTO_STAINED");}
 void OnDestroy(){if(story)story.Changed-=Apply;}
}
