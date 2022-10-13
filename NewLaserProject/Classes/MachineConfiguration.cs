using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft;

namespace NewLaserProject.Classes
{
    internal class MachineConfiguration
    {
        private const string PCI1240U = "PCI1240U";
        private const string PCI1245E = "PCI1245E";
        private const string MOCKBOARD = "MOCKBOARD";

        private const string UF = "UF";
        private const string IR = "IR";
        private const string LASERMOCK = "LASERMOCK";

        public string MotionBoardNote { get => $"Choose from following boards: {PCI1240U}, {PCI1245E}, {MOCKBOARD}"; }
        public string MotionBoard { get; set; }
        public string MarkDevTypeNote { get => $"Choose from following types: {UF}, {IR}, {LASERMOCK}"; }
        public string MarkDevType { get; set; }
        public bool IsPCI1240U { get => MotionBoard == PCI1240U; }
        public bool IsPCI1245E { get => MotionBoard == PCI1245E; }
        public bool IsMOCKBOARD { get => MotionBoard == MOCKBOARD; }
        public bool IsUF { get => MarkDevType == UF; }
        public bool IsIR { get => MarkDevType == IR; }
        public bool IsLaserMock { get => MarkDevType == LASERMOCK; }

    }
}
