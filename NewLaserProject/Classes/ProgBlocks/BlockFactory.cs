﻿namespace NewLaserProject.Classes.ProgBlocks
{
    internal class BlockFactory
    {
        public static IProgBlock GetProgBlock(object progBlock) => progBlock switch
        {
            PierceBlock pierceBlock => pierceBlock,
            LoopBlock loopBlock => loopBlock,
            DelayBlock delayBlock => delayBlock,
            AddZBlock addZBlock => addZBlock,
            TapperBlock tapperBlock => tapperBlock ,
            RepairZBlock repairZBlock => repairZBlock,
        };
        public static IProgBlock BlockTypeSelector(object progBlock) => progBlock switch
        {
            PierceBlock => new PierceBlock(),
            LoopBlock => new LoopBlock(),
            DelayBlock => new DelayBlock(),
            AddZBlock => new AddZBlock(),
            TapperBlock => new TapperBlock(),
            RepairZBlock => new RepairZBlock()
        };
    }

}
