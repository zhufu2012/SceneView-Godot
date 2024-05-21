using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SceneCore_Space
{

    public partial class SceneTree : Tree
    {
		
        public override void _Ready()
        {
            base._Ready();
        }
		
		//检查是否可拖动
        public override bool _CanDropData(Vector2 atPosition, Variant data)
        {
            GD.Print(atPosition);
            GD.Print(data);
            return base._CanDropData(atPosition, data);
        }
    }
}
