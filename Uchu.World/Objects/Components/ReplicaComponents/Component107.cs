using RakDotNet.IO;

namespace Uchu.World
{
    public class Component107 : ReplicaComponent
    {
        public override ComponentId Id => ComponentId.Component107;

        public override void Construct(BitWriter writer)
        {
            Serialize(writer);
        }

        public override void Serialize(BitWriter writer)
        {
            writer.WriteBit(false);
        }
    }
}