#if TOOLS
using Godot;


namespace SceneCore_Space
{
    [Tool]
    public partial class SceneCore : EditorPlugin
    {
        //主界面
        private Control MainPanelInstance;//主节目
        private Control TreePanel;//主节目
        private SceneTree tree;//树
        private PopupMenu popupMenu;//菜单
        //场景树
        public SceneLable labledata;
        public override void _EnterTree()
        {
            if (OS.HasFeature("debug"))
            {
                MainPanelInstance = GD.Load<PackedScene>("res://addons/SceneView/scene/SceneView.tscn").Instantiate<Control>();
                popupMenu = MainPanelInstance.GetNode<PopupMenu>("PopupMenu");
                tree = new SceneTree(popupMenu);//初始化树
                Button button = new Button();
                button.Text = "刷新场景";
                button.Size = new Vector2(80, 31);
                button.Position = new Vector2(250, 1);
                button.ButtonDown += OnButtonRefreshView;
                MainPanelInstance.AddChild(tree);
                MainPanelInstance.AddChild(button);
                AddControlToDock(DockSlot.LeftUl, MainPanelInstance);
            }
        }

        public override void _ExitTree()
        {
            if (OS.HasFeature("debug"))
            {
                if (MainPanelInstance != null)
                {
                    RemoveControlFromDocks(MainPanelInstance);
                    MainPanelInstance.Free();
                }
            }
        }

        /// <summary>
        ///按钮点击窗口刷新按钮
        /// </summary>
        private void OnButtonRefreshView()
        {
            tree.IniView(true);
        }


    }

}
#endif