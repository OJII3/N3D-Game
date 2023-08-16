using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Project
{
    public class InsertAnimation : MonoBehaviour
    {
        private PlayableGraph _graph;
        private AnimationPlayableOutput _animOutput;
        private AnimationClipPlayable _animClipPlayable;
        private AnimationClip _animClip;

        [SerializeField] private Animator animator;
        [SerializeField] private AnimationCurve curve = null;

        private void Awake()
        {
            _graph = PlayableGraph.Create();
            _animOutput = AnimationPlayableOutput.Create(_graph, "Animation", animator);
            _animOutput.SetWeight(0);
        }

        private void OnDestroy()
        {
            _graph.Destroy();
        }

        public IEnumerable<WaitForSeconds> Play(float time, AnimationClip animClip)
        {
            if (_animClipPlayable.IsValid()) _animClipPlayable.Destroy();
            _animClipPlayable = AnimationClipPlayable.Create(_graph, animClip);
            _animOutput.SetSourcePlayable(_animClipPlayable);

            _graph.Play();

            for (var endTime = Time.timeSinceLevelLoad + time; endTime > Time.timeSinceLevelLoad;)
            {
                var diff = 1 - (endTime - Time.timeSinceLevelLoad) / time;
                _animOutput.SetWeight(curve.Evaluate(diff));
                yield return null;
            }

            _animOutput.SetWeight(1);

            animator.playableGraph.Stop();
            yield return new WaitForSeconds(animClip.length - time * 2);
            animator.playableGraph.Play();

            for (var endTime = Time.timeSinceLevelLoad + time; endTime > Time.timeSinceLevelLoad;)
            {
                var diff = 1 - (endTime - Time.timeSinceLevelLoad) / time;
                _animOutput.SetWeight(curve.Evaluate(diff));
                yield return null;
            }

            _animOutput.SetWeight(0);

            _animClipPlayable.Destroy();
            _graph.Stop();
        }
    }
}