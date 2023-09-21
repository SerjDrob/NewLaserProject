using System.Linq;
using NewLaserProject.Classes.ProgBlocks.ProgBlocksFeatures;

namespace NewLaserProject.Classes.ProgBlocks
{
    internal class LoopBlock : IProgBlock
    {
        public bool CanAcceptChildren { get; set; } = true;
        public ChildrenObservableCollection<IProgBlock> Children { get; protected set; }
        public LoopBlock()
        {
            Children = new(this);
        }
        public void AddChild(IProgBlock child)
        {
            Children.Add(child);
        }
        public int LoopCount { get; set; }       
       
    }

}
