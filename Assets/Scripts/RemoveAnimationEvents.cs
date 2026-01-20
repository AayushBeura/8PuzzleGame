using UnityEngine;

public class RemoveAnimationEvents : MonoBehaviour
{
    void Awake()
    {
        Animator animator = GetComponent<Animator>();
        if (animator == null) return;

        // Go through all clips in the Animator
        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
        {
            clip.events = new AnimationEvent[0]; // remove all events
        }
    }
}
