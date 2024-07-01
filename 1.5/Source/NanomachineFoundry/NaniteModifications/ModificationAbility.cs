namespace NanomachineFoundry.NaniteModifications
{
    public abstract class ModificationAbility
    {
        public ModificationWorkerAbility Worker;

        public ModificationAbility(ModificationWorkerAbility worker)
        {
            Worker = worker;
        }

        public abstract void Use();
    }
}