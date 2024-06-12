#if TOOLS
using Godot;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;


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

        private List<Texture2D> ico_list = new List<Texture2D>();   //

        //维护json文件
        SaveLoadData saveLoadData = null;
        //场景树
        public SceneLable labledata;

        public int MAXTIME = 120;//最大保存间隔 120帧
        public int now_time = 0;//当前间隔
        public override void _EnterTree()
        {
            if (OS.HasFeature("debug"))
            {
                MainPanelInstance = GD.Load<PackedScene>("res://addons/SceneView/scene/SceneView.tscn").Instantiate<Control>();
                //tree = MainPanelInstance.GetNode<Tree>("Tree");
                //TreePanel = MainPanelInstance.GetNode<Control>("Tree");
                //tree = TreePanel.GetNode<Tree>("SceneTree");
                popupMenu = MainPanelInstance.GetNode<PopupMenu>("PopupMenu");
                saveLoadData = new SaveLoadData();
                labledata = saveLoadData.GetSceneLabelList();
                ico_list.Add(GD.Load<Texture2D>("res://addons/SceneView/img/主场景.png"));
                ico_list.Add(GD.Load<Texture2D>("res://addons/SceneView/img/标签.png"));
                ico_list.Add(GD.Load<Texture2D>("res://addons/SceneView/img/场景.png"));
                tree = new SceneTree(popupMenu, labledata, ico_list);
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

        public override void _PhysicsProcess(double delta)
        {
            now_time += 1;
            if (now_time > MAXTIME)
            {
                if (saveLoadData != null)
                {
                    saveLoadData.SaveData();//保存数据
                }
                now_time = 0;
            }
            base._PhysicsProcess(delta);
        }

        public override void _ExitTree()
        {
            if (OS.HasFeature("debug") && MainPanelInstance != null)
            {
                RemoveControlFromDocks(MainPanelInstance);
                MainPanelInstance.Free();
            }
            if (saveLoadData != null)
            {
                saveLoadData.SaveData();
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