#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using StoryTell.MainIntegration;
using Systems.Missions.FindObjects;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

public sealed class QuietPresentationTests
{
    static T One<T>()where T:Object=>Object.FindAnyObjectByType<T>();
    [UnityTest]public IEnumerator EntryIsClearAndSaveStatusStaysSmallAtBottomRight()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");yield return new EnterPlayMode();yield return null;yield return null;
        Assert.That(One<PauseMenu>().IsOpen,Is.False);Assert.That(One<UiSessionCoordinator>().Mode,Is.EqualTo(UiMode.None));
        Assert.That(Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None).Any(t=>t.text.Contains("PRUEBA DE COMBATE")),Is.False);
        var toast=One<SaveStatusToast>();toast.Show(true);yield return null;yield return null;Assert.That(toast.IsVisible,Is.True);Assert.That(toast.Message,Is.EqualTo("Checkpoint guardado"));var data=new SerializedObject(toast);var panel=(GameObject)data.FindProperty("panel").objectReferenceValue;var rect=(RectTransform)panel.transform;Assert.That(rect.anchorMin,Is.EqualTo(new Vector2(1,0)));Assert.That(rect.rect.height,Is.LessThanOrEqualTo(40));Assert.That(panel.GetComponentInChildren<TMP_Text>().fontSize,Is.LessThanOrEqualTo(22));
        yield return MainIntegrationTests.Capture("Library/QuietPresentation-save.png",1280,720);Assert.That(One<PauseMenu>().Open(),Is.True);yield return null;Assert.That(toast.IsVisible,Is.False);One<PauseMenu>().Close();yield return new ExitPlayMode();
    }
    [UnityTest]public IEnumerator DesignerLetterCutsReassembleAndPreserveOriginalArt()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");yield return new EnterPlayMode();yield return null;yield return null;
        One<LetterManager>().RestoreProgress(true,15,false,false);yield return null;yield return null;Assert.That(One<MainPresentation>().BeginLetterAssembly(),Is.True);yield return null;
        var pieces=Object.FindObjectsByType<LetterPuzzlePiece>(FindObjectsInactive.Include,FindObjectsSortMode.None);Assert.That(pieces.Length,Is.EqualTo(4));
        foreach(var piece in pieces)
        {
            Assert.That(piece.HasArtwork,Is.True);Assert.That(AssetDatabase.GetAssetPath(piece.mainTexture),Does.Contain("Assets/Art/Maximo/Interactables/Cartas/CartaLanus/"));Assert.That(piece.GetComponentsInChildren<TMP_Text>().Length,Is.Zero);
            var data=new SerializedObject(piece);var mesh=(Mesh)data.FindProperty("artworkMesh").objectReferenceValue;var mask=(Texture2D)data.FindProperty("artworkHitMask").objectReferenceValue;Assert.That(mask.isReadable,Is.True);Assert.That(mask.GetPixels().Any(c=>c.r<.1f),Is.True);Assert.That(mask.GetPixels().Any(c=>c.r>.9f),Is.True);
            var source=new Texture2D(2,2);source.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(piece.mainTexture)));var colors=source.GetPixels32();float minU=mesh.uv.Min(p=>p.x),maxU=mesh.uv.Max(p=>p.x),minV=mesh.uv.Min(p=>p.y),maxV=mesh.uv.Max(p=>p.y);
            for(int y=Mathf.CeilToInt(.4955f*source.height);y<.9685f*source.height;y+=4)for(int x=Mathf.CeilToInt(.1625f*source.width);x<.8375f*source.width;x+=4)if(colors[y*source.width+x].a>25){Assert.That(x/(float)source.width,Is.InRange(minU,maxU));Assert.That(y/(float)source.height,Is.InRange(minV,maxV));}Object.Destroy(source);
        }
        yield return MainIntegrationTests.Capture("Library/QuietPresentation-letter-pieces.png",1280,720);
        foreach(var piece in pieces){piece.transform.position=piece.owner.Slot(piece.slotIndex).position;piece.rectTransform.localRotation=Quaternion.identity;Assert.That(piece.owner.TrySnap(piece),Is.True);}
        Assert.That(One<LetterAssemblyPuzzle>().Complete,Is.True);Assert.That(One<LetterManager>().LetterAssembled,Is.True);yield return MainIntegrationTests.Capture("Library/QuietPresentation-letter-complete.png",1280,720);yield return MainIntegrationTests.Capture("Library/QuietPresentation-letter-5x4.png",1280,1024);One<LetterAssemblyPuzzle>().Close();yield return new ExitPlayMode();
    }
    [UnityTearDown]public IEnumerator Cleanup(){Time.timeScale=1;if(Application.isPlaying)yield return new ExitPlayMode();}
}
#endif
