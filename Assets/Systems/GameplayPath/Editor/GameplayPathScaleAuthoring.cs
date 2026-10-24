#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using FirstPersonControllerSystem.Scripts.ControllerSys;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using Unity.AI.Navigation;
using UnityEngine.AI;
namespace StoryTell.GameplayPath.Editor
{
    public static class GameplayPathScaleAuthoring
    {
        [Serializable] private sealed class Entry { public string name; public Vector3 position, size, center, scale; }
        [Serializable] private sealed class Inspection { public string scene; public float playerHeight; public Vector3 cameraPosition; public Entry[] objects; }
        public static void Inspect()
        {
            var scene=EditorSceneManager.OpenScene(GameplayPathInstaller.ScenePath);
            var player=All<PlayerControllerScript>(scene).Single();
            var root=All<GameplayPathController>(scene).Single().transform;
            var entries=root.GetComponentsInChildren<Transform>(true).Where(t=>t==root||t.name.StartsWith("Cartel_")||t.name.StartsWith("Carta_")||t.name.StartsWith("Lectura_")||t.name.Contains("Table")||t.name.Contains("Mesa_")||t.name.Contains("Recoger")||t.name.Contains("Equipo")||t.name.Contains("Comida")||t.name.Contains("Olla")||t.name.Contains("Caja_")||t.name.Contains("Ruben_")||t.name.Contains("Gomez_")||t.name.Contains("Sargento_")).Select(t=>{var b=VisualBounds(t);return new Entry{name=t.name,position=t.position,scale=t.lossyScale,size=b.size,center=b.center};}).ToArray();
            File.WriteAllText("Library/GameplayPath-scale-before.json",JsonUtility.ToJson(new Inspection{scene=scene.path,playerHeight=player.GetComponent<CharacterController>().height,cameraPosition=player.GetComponentInChildren<Camera>().transform.position,objects=entries},true));
        }
        [MenuItem("StoryTell/Gameplay Path/Adjust furniture and prop scale")]
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode first.");
            var scene=SceneManager.GetActiveScene();
            if(scene.path!=GameplayPathInstaller.ScenePath)
            {
                if(scene.IsValid()&&scene.isDirty)throw new InvalidOperationException("Save the current scene first.");
                scene=EditorSceneManager.OpenScene(GameplayPathInstaller.ScenePath);
            }
            var root=All<GameplayPathController>(scene).Single().transform;
            Directory.CreateDirectory("Library/GameplayPathBackups");
            string backup="Library/GameplayPathBackups/Level1-before-scale-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+".unity";
            if(!EditorSceneManager.SaveScene(scene,backup,true))throw new IOException("Cannot back up Level1.");
            Shots(root,"before");
            Undo.IncrementCurrentGroup();int undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Adjust Level1 furniture and prop scale");
            Transform Get(string name)=>root.GetComponentsInChildren<Transform>(true).Single(t=>t.name==name);
            var equipment=Get("Mesa_Suministros");Table(equipment,new Vector3(77.7f,0,277.4f));
            float top=VisualBounds(equipment).max.y;
            Vector3 On(float x,float z)=>new Vector3(equipment.position.x+x,top+0.008f,equipment.position.z+z);
            var helmet=Get("Casco_Recoger");Size(helmet,0.33f);Seat(helmet,On(-0.53f,0.20f));Refit(helmet,true);
            var paper=Get("Mapa_Recoger");PrimitiveSize(paper,new Vector3(0.28f,0.006f,0.21f));Seat(paper,On(-0.16f,0.19f));Refit(paper,true);
            var radio=Get("Radio_Recoger");Size(radio,0.32f);Seat(radio,On(0.44f,0.18f));Refit(radio,true);
            var ammo=Get("Municion_Equipo");Size(ammo,0.30f);Seat(ammo,On(0.58f,-0.18f));Refit(ammo,true);
            var fal=Get("FAL_Recoger_Equipo");Undo.RecordObject(fal,"Lay the rifle on the supply table");fal.rotation=Quaternion.Euler(90,0,0);Size(fal,1.0f);Seat(fal,On(-0.20f,-0.20f));Refit(fal,true);
            var kitchen=Get("Kitchen_Table");Table(kitchen,new Vector3(89,0,292));
            top=VisualBounds(kitchen).max.y;
            var cup=Get("Jarro_Comida");PrimitiveSize(cup,new Vector3(0.11f,0.12f,0.11f));Seat(cup,new Vector3(kitchen.position.x-0.3f,top+0.006f,kitchen.position.z-0.22f));Refit(cup,true);
            var pot=Get("Olla_Campana");PrimitiveSize(pot,new Vector3(0.56f,0.40f,0.56f));Seat(pot,Ground(new Vector3(91.1f,0,293.3f))+Vector3.up*0.15f);Refit(pot,false);
            var lid=Get("Tapa_Olla");PrimitiveSize(lid,new Vector3(0.60f,0.035f,0.60f));Seat(lid,new Vector3(pot.position.x,VisualBounds(pot).max.y,pot.position.z));Refit(lid,false);
            var stove=pot.parent.Find("Soporte_Olla");
            if(stove==null)
            {
                var go=GameObject.CreatePrimitive(PrimitiveType.Cylinder);Undo.RegisterCreatedObjectUndo(go,"Add field stove support");go.name="Soporte_Olla";stove=go.transform;stove.SetParent(pot.parent,false);go.GetComponent<Renderer>().sharedMaterial=pot.GetComponent<Renderer>().sharedMaterial;
            }
            PrimitiveSize(stove,new Vector3(0.42f,0.15f,0.42f));Seat(stove,Ground(pot.position));
            // Raise papers above the grass using two real crates rather than floating interaction objects.
            Vector3[] locations={new Vector3(87,0,296.2f),new Vector3(92.5f,0,296),new Vector3(94.5f,0,291.3f),new Vector3(85.5f,0,289.4f)};
            for(int i=0;i<4;i++)
            {
                var support=Get("Carta_Apoyo_"+i);Seat(support,Ground(locations[i]));Stack(support);
                var fragment=Get("Carta_Madre_Fragmento_"+(i+1));PrimitiveSize(fragment,new Vector3(0.18f,0.006f,0.13f));Undo.RecordObject(fragment,"Place letter fragment naturally");fragment.rotation=Quaternion.Euler(0,new[]{-12,19,8,-23}[i],0);
                var b=VisualBounds(support);Seat(fragment,new Vector3(b.center.x,b.max.y+0.004f,b.center.z));Refit(fragment,true);
                var ink=fragment.Find("Tinta");if(ink!=null)
                {
                    Undo.RecordObject(ink,"Fit handwriting to paper");ink.localPosition=new Vector3(0,0.55f,0);ink.localRotation=Quaternion.Euler(90,0,0);ink.localScale=new Vector3(1/fragment.lossyScale.x,1/fragment.lossyScale.z,1/fragment.lossyScale.y);
                    var text=ink.GetComponent<TMP_Text>();Undo.RecordObject(text,"Fit handwritten fragment");text.rectTransform.sizeDelta=new Vector2(0.16f,0.10f);text.fontSize=0.55f;text.enableAutoSizing=true;text.fontSizeMin=0.3f;text.fontSizeMax=0.55f;
                }
            }
            for(int i=0;i<2;i++)
            {
                var support=Get("Lectura_Apoyo_"+i);Seat(support,Ground(new Vector3(87.1f+i*1.5f,0,290.2f)));Stack(support);
                var letter=Get("Carta_Opcional_"+i);PrimitiveSize(letter,new Vector3(0.28f,0.006f,0.20f));var b=VisualBounds(support);Seat(letter,new Vector3(b.center.x,b.max.y+0.004f,b.center.z));Refit(letter,true);
            }
            var crate=Get("Caja_Herramientas_Mantas_Recoger");Size(crate,0.65f);Seat(crate,Ground(new Vector3(98,0,269)));Refit(crate,true);
            var delivered=Get("Caja_Entregada");Size(delivered,0.65f);Seat(delivered,Ground(new Vector3(115.4f,0,362.1f)));Refit(delivered,false);
            var player=All<PlayerControllerScript>(scene).Single();
            foreach(var sign in root.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Cartel_")).ToArray())Sign(sign,player.transform.position);
            foreach(var t in player.GetComponentsInChildren<Transform>(true))if(t.name=="Jarro_EnManos")PrimitiveSize(t,new Vector3(0.11f,0.12f,0.11f));
            Physics.SyncTransforms();Rebake(scene);
            foreach(var c in root.GetComponentsInChildren<Component>(true))if(c!=null&&PrefabUtility.IsPartOfPrefabInstance(c))PrefabUtility.RecordPrefabInstancePropertyModifications(c);
            Undo.CollapseUndoOperations(undo);EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();
            if(!EditorSceneManager.SaveScene(scene))throw new IOException("Could not save scaled Level1.");
            Shots(root,"after");Validate();
        }
        private static void Table(Transform table,Vector3 center)
        {
            Undo.RecordObject(table,"Set table to waist height");table.localScale*=0.78f/VisualBounds(table).size.y;
            var bounds=VisualBounds(table);if(bounds.size.z>bounds.size.x)table.rotation=Quaternion.Euler(0,90,0);
            Seat(table,Ground(center));Refit(table,false);
        }
        private static void Stack(Transform support)
        {
            if(support.Find("Caja_Superior")==null)
            {
                var source=support.GetChild(0);var copy=UnityEngine.Object.Instantiate(source.gameObject,support,false);Undo.RegisterCreatedObjectUndo(copy,"Stack supply crates");copy.name="Caja_Superior";copy.transform.localPosition=source.localPosition+Vector3.up*VisualBounds(source).size.y;
                foreach(var collider in copy.GetComponentsInChildren<Collider>(true))collider.enabled=false;
            }
            Refit(support,false);
        }
        private static void Sign(Transform sign,Vector3 start)
        {
            Vector3 point=sign.position;Undo.RecordObject(sign,"Orient sign toward arriving player");sign.position=Ground(point);
            Vector3 approach=point.z<310?start:point.z<350?new Vector3(96,0,320):point.z<370?new Vector3(110,0,342):point.z<400?new Vector3(114,0,361):point.z<440?new Vector3(102,0,383):new Vector3(77,0,416);
            Vector3 forward=Vector3.ProjectOnPlane(point-approach,Vector3.up);if(forward.sqrMagnitude>0.01f)sign.rotation=Quaternion.LookRotation(forward);
            var post=sign.Find("Poste");Undo.RecordObject(post,"Scale wooden signpost");post.localScale=new Vector3(0.075f,1.32f,0.075f);post.localPosition=Vector3.up*0.66f;
            var board=sign.Find("Tabla");Undo.RecordObject(board,"Scale signboard");board.localScale=new Vector3(1.35f,0.30f,0.045f);board.localPosition=Vector3.up*1.22f;
            var label=sign.Find("Rotulo").GetComponent<TextMeshPro>();Undo.RecordObject(label,"Fit readable sign label");Undo.RecordObject(label.transform,"Place sign lettering");label.transform.localPosition=new Vector3(0,1.22f,-0.027f);label.transform.localRotation=Quaternion.identity;label.rectTransform.sizeDelta=new Vector2(1.26f,0.26f);label.fontSize=1.8f;label.enableAutoSizing=true;label.fontSizeMin=0.8f;label.fontSizeMax=1.8f;
            if(sign.name.Contains("→")||sign.name.Contains("↑"))
            {
                Vector3 destination=point.z<350?new Vector3(114,0,361):point.z<400?new Vector3(77,0,416):point.z<440?new Vector3(60,0,468):new Vector3(56,0,543);
                float side=Vector3.Dot((destination-point).normalized,sign.right);string arrow=side>0.35f?"→":side< -0.35f?"←":"↑";
                label.text=label.text.Replace("→","").Replace("↑","").Replace("←","").Trim()+" "+arrow;
            }
            var reverse=sign.Find("Rotulo_Reverso");if(reverse==null){var copy=UnityEngine.Object.Instantiate(label.gameObject,sign,false);Undo.RegisterCreatedObjectUndo(copy,"Make sign readable when returning");copy.name="Rotulo_Reverso";reverse=copy.transform;}
            Undo.RecordObject(reverse,"Position reverse sign lettering");reverse.localPosition=new Vector3(0,1.22f,0.027f);reverse.localRotation=Quaternion.Euler(0,180,0);var reverseText=reverse.GetComponent<TextMeshPro>();Undo.RecordObject(reverseText,"Match reverse sign label");reverseText.text=ReverseDirections(label.text);
        }
        private static void Size(Transform target,float maximum)
        {Undo.RecordObject(target,"Resize scene prop");var b=VisualBounds(target);float current=Mathf.Max(b.size.x,Mathf.Max(b.size.y,b.size.z));if(current>0.001f)target.localScale*=maximum/current;}
        private static void PrimitiveSize(Transform target,Vector3 size)
        {Undo.RecordObject(target,"Set human-scale prop dimensions");var b=VisualBounds(target);target.localScale=Vector3.Scale(target.localScale,new Vector3(size.x/b.size.x,size.y/b.size.y,size.z/b.size.z));}
        private static void Seat(Transform target,Vector3 centerBase)
        {Undo.RecordObject(target,"Seat prop on supporting surface");var b=VisualBounds(target);target.position+=centerBase-new Vector3(b.center.x,b.min.y,b.center.z);}
        private static void Refit(Transform target,bool pickup)
        {
            var box=target.GetComponent<BoxCollider>();if(box==null)return;
            Undo.RecordObject(box,"Fit collision to resized model");var filters=target.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.GetComponent<TMP_Text>()==null&&f.sharedMesh!=null).ToArray();var local=new Bounds();bool first=true;
            foreach(var f in filters){var b=f.sharedMesh.bounds;for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2){var p=target.InverseTransformPoint(f.transform.TransformPoint(b.center+Vector3.Scale(b.extents,new Vector3(x,y,z))));if(first){local=new Bounds(p,Vector3.zero);first=false;}else local.Encapsulate(p);}}
            foreach(var r in target.GetComponentsInChildren<SkinnedMeshRenderer>(true)){var b=r.bounds;for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2){var p=target.InverseTransformPoint(b.center+Vector3.Scale(b.extents,new Vector3(x,y,z)));if(first){local=new Bounds(p,Vector3.zero);first=false;}else local.Encapsulate(p);}}
            box.center=local.center;box.size=local.size;
            if(pickup){box.isTrigger=true;Vector3 scale=target.lossyScale;box.size+=new Vector3(0.04f/Mathf.Abs(scale.x),0.035f/Mathf.Abs(scale.y),0.04f/Mathf.Abs(scale.z));}
        }
        private static Vector3 Ground(Vector3 point)
        {var terrain=Terrain.activeTerrains.First(t=>point.x>=t.transform.position.x&&point.z>=t.transform.position.z&&point.x<=t.transform.position.x+t.terrainData.size.x&&point.z<=t.transform.position.z+t.terrainData.size.z);return new Vector3(point.x,terrain.SampleHeight(point)+terrain.transform.position.y+0.015f,point.z);}
        private static void Rebake(Scene scene)
        {
            var surface=All<NavMeshSurface>(scene).Single();var saved=surface.navMeshData;surface.BuildNavMesh();if(surface.navMeshData==null)throw new InvalidOperationException("Navigation bake failed.");EditorUtility.CopySerialized(surface.navMeshData,saved);surface.RemoveData();var so=new SerializedObject(surface);so.FindProperty("m_NavMeshData").objectReferenceValue=saved;so.ApplyModifiedProperties();surface.AddData();EditorUtility.SetDirty(saved);
        }
        public static void Validate()
        {
            var scene=SceneManager.GetActiveScene();var root=All<GameplayPathController>(scene).Single().transform;
            var tables=root.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Mesa_Suministros"||t.name=="Kitchen_Table").ToArray();
            foreach(var table in tables)if(Mathf.Abs(VisualBounds(table).size.y-0.78f)>0.015f)throw new InvalidOperationException("Incorrect table height: "+table.name);
            foreach(var sign in root.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Cartel_")))if(VisualBounds(sign).size.y>1.5f)throw new InvalidOperationException("Sign taller than planned: "+sign.name);
            var supply=tables.Single(t=>t.name=="Mesa_Suministros");float top=VisualBounds(supply).max.y;
            foreach(var name in new[]{"Casco_Recoger","FAL_Recoger_Equipo","Municion_Equipo","Mapa_Recoger","Radio_Recoger"})
            {
                var item=root.GetComponentsInChildren<Transform>(true).Single(t=>t.name==name);var bounds=VisualBounds(item);
                if(Mathf.Abs(bounds.min.y-top-0.008f)>0.02f)throw new InvalidOperationException("Unsupported pickup: "+name);
                var tableBounds=VisualBounds(supply);if(bounds.min.x<tableBounds.min.x-0.02f||bounds.max.x>tableBounds.max.x+0.02f||bounds.min.z<tableBounds.min.z-0.02f||bounds.max.z>tableBounds.max.z+0.02f)throw new InvalidOperationException("Pickup overhangs table: "+name);
                Vector3 eye=supply.position+new Vector3(0,1.65f,-2.2f);Vector3 delta=bounds.center-eye;
                if(!Physics.Raycast(eye,delta.normalized,out var interaction,5,1<<6,QueryTriggerInteraction.Collide)||interaction.collider.GetComponentInParent<GameplayPathInteractable>()!=item.GetComponent<GameplayPathInteractable>())throw new InvalidOperationException("Equipment interaction obstructed: "+name+"; first hit="+(interaction.collider!=null?interaction.collider.name:"none"));
                var solid=Physics.RaycastAll(eye,delta.normalized,interaction.distance,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
                if(solid.Any(h=>h.distance+0.001f<interaction.distance))throw new InvalidOperationException("World geometry blocks equipment interaction: "+name);
            }
            foreach(var t in root.GetComponentsInChildren<Transform>(true))if(t.GetComponents<Component>().Any(c=>c==null))throw new InvalidOperationException("Missing script: "+t.name);
            File.WriteAllText("Library/GameplayPath-scale-result.json","{\"status\":\"passed\",\"tables\":2,\"signs\":8,\"tableHeightMeters\":0.78,\"supportedEquipment\":5,\"unobstructedInteractions\":5}");
        }
        public static void VerifySaved()
        {
            var scene=EditorSceneManager.OpenScene(GameplayPathInstaller.ScenePath);Physics.SyncTransforms();Validate();
            var root=All<GameplayPathController>(scene).Single().transform;Shots(root,"after");
            var sign=root.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Cartel_EQUIPO");Shot("Library/GameplayPath-scale-sign-after.png",sign.position-sign.forward*2.3f+Vector3.up*1.6f,sign.position+Vector3.up*1.15f);
        }
        private static string ReverseDirections(string text)=>text.Replace("→","~").Replace("←","→").Replace("~","←").Replace("↑","↓");
        public static void FinishPresentation()
        {
            var scene=EditorSceneManager.OpenScene(GameplayPathInstaller.ScenePath);var root=All<GameplayPathController>(scene).Single().transform;
            foreach(var sign in root.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Cartel_")))
            {
                var front=sign.Find("Rotulo").GetComponent<TextMeshPro>();var back=sign.Find("Rotulo_Reverso").GetComponent<TextMeshPro>();
                if(back.text!=ReverseDirections(front.text)){Undo.RecordObject(back,"Correct return-direction arrow");back.text=ReverseDirections(front.text);EditorSceneManager.MarkSceneDirty(scene);}
            }
            if(scene.isDirty)EditorSceneManager.SaveScene(scene);VerifySaved();
        }
        public static void GroundLooseCrate()
        {
            var scene=EditorSceneManager.OpenScene(GameplayPathInstaller.ScenePath);
            var crate=scene.GetRootGameObjects().FirstOrDefault(g=>g.name=="MediumBox");
            if(crate!=null)
            {
                var bounds=VisualBounds(crate.transform);var floor=Ground(bounds.center);
                if(bounds.min.y-floor.y>1)
                {
                    Seat(crate.transform,Ground(new Vector3(80.0f,0,279.1f)));
                    foreach(var component in crate.GetComponentsInChildren<Component>(true))if(component!=null&&PrefabUtility.IsPartOfPrefabInstance(component))PrefabUtility.RecordPrefabInstancePropertyModifications(component);
                    Rebake(scene);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                    File.WriteAllText("Library/GameplayPath-scale-loose-crate.json","{\"grounded\":true}");
                }
            }
            VerifySaved();
        }
        private static void Shots(Transform root,string suffix)
        {
            var equipment=root.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Mesa_Suministros");var kitchen=root.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Kitchen_Table");
            Shot("Library/GameplayPath-scale-equipment-"+suffix+".png",equipment.position+new Vector3(-0.6f,1.65f,-2.8f),equipment.position+Vector3.up*0.85f);
            Shot("Library/GameplayPath-scale-kitchen-"+suffix+".png",kitchen.position+new Vector3(-2.2f,1.65f,-3.1f),kitchen.position+Vector3.up*0.8f);
        }
        private static void Shot(string path,Vector3 eye,Vector3 target)
        {
            var host=new GameObject("ScaleInspectionCamera");var camera=host.AddComponent<Camera>();host.transform.position=eye;host.transform.LookAt(target);camera.fieldOfView=65;camera.nearClipPlane=0.05f;camera.farClipPlane=700;camera.cullingMask=~(1<<5|1<<9|1<<10);var rt=new RenderTexture(1440,900,24);var pixels=new Texture2D(1440,900,TextureFormat.RGB24,false);var previous=RenderTexture.active;
            try{rt.Create();camera.targetTexture=rt;RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=rt});RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,1440,900),0,0);pixels.Apply();File.WriteAllBytes(path,pixels.EncodeToPNG());}
            finally{RenderTexture.active=previous;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(host);rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(pixels);}
        }
        internal static Bounds VisualBounds(Transform target)
        {
            var rs=target.GetComponentsInChildren<Renderer>(true).Where(r=>r.GetComponent<TMP_Text>()==null).ToArray();
            if(rs.Length==0)return new Bounds(target.position,Vector3.zero);
            var bounds=rs[0].bounds;foreach(var r in rs.Skip(1))bounds.Encapsulate(r.bounds);return bounds;
        }
        internal static T[] All<T>(Scene scene)where T:Component=>scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();
    }
}
#endif
