using System;
using System.Numerics;
using RakDotNet.IO;
using Uchu.Core;

namespace Uchu.World
{
    public class PhantomPhysicsComponent : ReplicaComponent
    {
        public override ComponentId Id => ComponentId.PhantomPhysicsComponent;

        public bool HasPosition { get; set; } = true;

        public bool IsEffectActive { get; set; } = false;

        public uint EffectType { get; set; }

        public float EffectAmount { get; set; }

        public Vector3? EffectDirection { get; set; } = null;
        
        public override void Construct(BitWriter writer)
        {
            Serialize(writer);
        }

        public override void Serialize(BitWriter writer)
        {
            writer.WriteBit(HasPosition);

            if (HasPosition)
            {
                writer.Write(Transform.Position);
                writer.Write(Transform.Rotation);
            }

            writer.WriteBit(true);
            writer.WriteBit(IsEffectActive);

            if (!IsEffectActive) return;

            writer.Write(EffectType);
            writer.Write(EffectAmount);
            writer.WriteBit(false);

            var hasDirection = EffectDirection != null;

            writer.WriteBit(hasDirection);

            if (!hasDirection) return;

            writer.Write(EffectDirection.Value * EffectAmount);
        }
    }
}