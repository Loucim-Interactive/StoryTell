using UnityEngine;
namespace StoryTell.MainIntegration
{
    [RequireComponent(typeof(Animator))]
    public sealed class NpcFootPlacement:MonoBehaviour
    {
        [SerializeField] private Transform actor;
        [SerializeField] private float soleOffset=.1f;
        [SerializeField] private SkinnedMeshRenderer soleMesh;
        private Animator animator;
        private readonly RaycastHit[] hits=new RaycastHit[16];
        private void Awake(){animator=GetComponent<Animator>();}
        private void OnAnimatorIK(int layer)
        {
            if(!animator||!animator.isHuman)return;Place(AvatarIKGoal.LeftFoot,HumanBodyBones.LeftFoot);Place(AvatarIKGoal.RightFoot,HumanBodyBones.RightFoot);
        }
        private void Place(AvatarIKGoal goal,HumanBodyBones bone)
        {
            var foot=animator.GetBoneTransform(bone);if(!foot)return;
            int count=Physics.RaycastNonAlloc(foot.position+Vector3.up*.65f,Vector3.down,hits,1.6f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);float distance=float.PositiveInfinity;int nearest=-1;
            for(int i=0;i<count;i++)if(!hits[i].collider.transform.IsChildOf(actor)&&hits[i].normal.y>.5f&&hits[i].distance<distance){distance=hits[i].distance;nearest=i;}
            if(nearest<0){animator.SetIKPositionWeight(goal,0);animator.SetIKRotationWeight(goal,0);return;}
            var hit=hits[nearest];float bottom=goal==AvatarIKGoal.LeftFoot?animator.leftFeetBottomHeight:animator.rightFeetBottomHeight;if(bottom<=0)bottom=soleOffset;
            animator.SetIKPositionWeight(goal,1);animator.SetIKRotationWeight(goal,.35f);animator.SetIKPosition(goal,hit.point+Vector3.up*bottom);animator.SetIKRotation(goal,Quaternion.FromToRotation(Vector3.up,hit.normal)*foot.rotation);
        }
    }
}
