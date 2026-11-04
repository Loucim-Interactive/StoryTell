using UnityEngine;
namespace StoryTell.MainIntegration
{
 [RequireComponent(typeof(Animator)),DefaultExecutionOrder(450)]
 public sealed class NpcFootPlacement:MonoBehaviour
 {
  [SerializeField]private Transform actor;
  [SerializeField]private float soleOffset=.1f;
  [SerializeField]private SkinnedMeshRenderer soleMesh;
  [SerializeField]private Terrain ground;
  [SerializeField,Min(0)]private float clearance=.015f;
  private Animator animator;
  private Transform left,right;
  private float leftBottom,rightBottom,settle;
  private int warmup;
  private float nextCalibration;
  private bool calibrated;
  public bool Calibrated=>calibrated;
  public float LeftSoleOffset=>leftBottom;
  public float RightSoleOffset=>rightBottom;
  private void Awake(){animator=GetComponent<Animator>();}
  private float Height(Vector3 p)=>ground?ground.SampleHeight(p)+ground.transform.position.y:actor.position.y;
  private void LateUpdate()
  {
   if(!actor||!soleMesh||!animator.isInitialized||!animator.isHuman)return;
   if(!left||!right){left=animator.GetBoneTransform(HumanBodyBones.LeftFoot);right=animator.GetBoneTransform(HumanBodyBones.RightFoot);}
   if(!left||!right)return;
   if(!calibrated&&++warmup>=2&&Time.time>=nextCalibration){nextCalibration=Time.time+.25f;Calibrate();}
   if(!calibrated||Mathf.Abs(settle)<.0005f)return;
   float step=settle>0?settle:Mathf.Max(settle,-.8f*Time.deltaTime);actor.position+=Vector3.up*step;settle-=step;
  }
  private void Calibrate()
  {
   // Baking with scale avoids applying the imported 100x FBX scale twice.
   var mesh=new Mesh();soleMesh.BakeMesh(mesh,true);var vertices=mesh.vertices;
   float minLeft=float.PositiveInfinity,minRight=float.PositiveInfinity,correction=float.NegativeInfinity;
   foreach(var v in vertices)
   {
    Vector3 p=soleMesh.transform.position+soleMesh.transform.rotation*v;
    float dl=(new Vector2(p.x-left.position.x,p.z-left.position.z)).sqrMagnitude;
    float dr=(new Vector2(p.x-right.position.x,p.z-right.position.z)).sqrMagnitude;
    if(dl<dr)minLeft=Mathf.Min(minLeft,p.y);else minRight=Mathf.Min(minRight,p.y);
    correction=Mathf.Max(correction,Height(p)+clearance-p.y);
   }
   Destroy(mesh);
   if((float.IsInfinity(minLeft)&&float.IsInfinity(minRight))||Mathf.Abs(correction)>1)return;
   // Some imported boot meshes cover one side only; the matching boot shares its sole thickness.
   float known=!float.IsInfinity(minLeft)?left.position.y-minLeft:right.position.y-minRight;
   leftBottom=Mathf.Clamp(float.IsInfinity(minLeft)?known:left.position.y-minLeft,.01f,.5f);
   rightBottom=Mathf.Clamp(float.IsInfinity(minRight)?known:right.position.y-minRight,.01f,.5f);
   settle=correction;calibrated=true;
  }
  private void OnAnimatorIK(int layer)
  {
   if(!calibrated||!ground)return;
   Place(AvatarIKGoal.LeftFoot,left,leftBottom);Place(AvatarIKGoal.RightFoot,right,rightBottom);
  }
  private void Place(AvatarIKGoal goal,Transform foot,float bottom)
  {
   Vector3 p=foot.position;p.y=Height(p)+bottom+clearance;
   Vector3 local=ground.transform.InverseTransformPoint(p);Vector3 n=ground.terrainData.GetInterpolatedNormal(local.x/ground.terrainData.size.x,local.z/ground.terrainData.size.z);
   animator.SetIKPositionWeight(goal,1);animator.SetIKRotationWeight(goal,.35f);animator.SetIKPosition(goal,p);animator.SetIKRotation(goal,Quaternion.FromToRotation(Vector3.up,n)*foot.rotation);
  }
 }
}
