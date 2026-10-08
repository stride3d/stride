using System;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace Stride.Animation
{
    public class MorphTargetEvaluator : AnimationEvaluator
    {
        public override void Evaluate(float deltaTime, AnimationClip clip, Entity target)
        {
            // Lógica para ler tracks de 'MorphWeight' do clip de animação
            // e aplicar os valores nos componentes de Mesh do target.
            var meshComponent = target.GetComponent<ModelComponent>();
            if (meshComponent == null) return;

            // Atualiza os pesos que serão enviados para a GPU
            foreach (var track in clip.MorphTracks)
            {
                float weight = track.Evaluate(clip.Time);
                meshComponent.SetMorphWeight(track.TargetName, weight);
            }
        }
    }
}
