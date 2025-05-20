using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DynamicData;
using NodeNetwork.Toolkit.ValueNode;
using NodeNetwork.ViewModels;

namespace NewLaserProject.ViewModels
{
    public class NodesEditorVM
    {
        public NetworkViewModel Network { get; set; }
        public NodesEditorVM()
        {
            Network = new NetworkViewModel();

            //Create the node for the first node, set its name and add it to the network.
            var node1 = new NodeViewModel();
            node1.Name = "Node 1";
            Network.Nodes.Add(node1);

            //Create the viewmodel for the input on the first node, set its name and add it to the node.
            var node1Input = new ValueNodeInputViewModel<string>();
            node1Input.Name = "Node 1 input";
            node1.Inputs.Add(node1Input);

            //Create the second node viewmodel, set its name, add it to the network and add an output in a similar fashion.
            var node2 = new NodeViewModel();
            node2.Name = "Node 2";
            Network.Nodes.Add(node2);

            var node2Output = new NodeOutputViewModel();
            node2Output.Name = "Node 2 output";
            node2.Outputs.Add(node2Output);
        }
    }
}
